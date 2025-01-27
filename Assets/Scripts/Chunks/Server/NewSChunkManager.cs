using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib.Utils;
using Networking.Shared;
using UnityEngine;

namespace Networking.Server {
    public class NewSChunkManager : BaseSingleton<NewSChunkManager> {
        private static readonly Vector2Int[] offsets = new Vector2Int[] {
            new(-1,  1), new( 0,  1), new( 1,  1),
            new(-1,  0), new( 0,  0), new( 1,  0),
            new(-1, -1), new( 0, -1), new( 1, -1)
        };

        private static readonly Vector2Int[] nonCenterOffsets = new Vector2Int[] {
            new(-1,  1), new( 0,  1), new( 1,  1),
            new(-1,  0),              new( 1,  0),
            new(-1, -1), new( 0, -1), new( 1, -1)
        };

        private const float chunkSize = 32;
        private readonly TimeSpan chunkExpirationTime = new(0, 0, 10);
        ///<summary> Chunk coordinates and their expiration dates </summary>
        private Dictionary<Vector2Int, DateTime> expiringChunks = new();
        private Dictionary<Vector2Int, NewSChunk> loadedChunks = new();

        private HashSet<NewSChunk> chunksWithUpdates = new();

        public static Vector2Int ProjectToGrid(Vector3 position) {
            Vector2Int ret = new(Mathf.RoundToInt(position.x / chunkSize), Mathf.RoundToInt(position.z / chunkSize));
            return ret;
        }

        protected override void Awake() {
            SEntity.SetAsPlayer += SetEntityAsPlayer;
            SEntity.UnsetAsPlayer += UnsetEntityAsPlayer;
            NewSChunk.StartExpiring += StartExpiringChunk;
            NewSChunk.StopExpiring += StopExpiringChunk;
            NewSChunk.HasAnUpdate += RegisterChunkWithUpdates;
            base.Awake();
        }

        protected override void OnDestroy() {
            SEntity.SetAsPlayer -= SetEntityAsPlayer;
            SEntity.UnsetAsPlayer -= UnsetEntityAsPlayer;
            NewSChunk.StartExpiring -= StartExpiringChunk;
            NewSChunk.StopExpiring -= StopExpiringChunk;
            NewSChunk.HasAnUpdate -= RegisterChunkWithUpdates;
            base.OnDestroy();
        }


        /// <summary> Gets all chunks surrounding a given chunk coordinate. </summary>
        /// <param name="loadIfNotExists"> Should neighboring chunks that don't exist be generated for the entity? </param>
        public static HashSet<NewSChunk> GetChunksInView(Vector2Int centerCoords, bool getCenter = true, bool loadIfNotExists = false) {
            HashSet<NewSChunk> ret = new(getCenter ? offsets.Length : nonCenterOffsets.Length);
            
            foreach(Vector2Int offset in getCenter ? offsets : nonCenterOffsets) {
                Vector2Int offsetCoords = centerCoords + offset;

                if(!Instance.loadedChunks.TryGetValue(offsetCoords, out NewSChunk chunk)) {
                    if(loadIfNotExists) {
                        chunk = new(offsetCoords);
                        Instance.loadedChunks[offsetCoords] = chunk;
                    } else {
                        continue;
                    }
                }

                ret.Add(chunk);
            }

            return ret;
        }


        ///<summary> Adds an entity to the chunk system </summary>
        public static bool AddEntityToSystem(SEntity entity, Vector2Int toCoords) {
            bool canAdd;
            NewSChunk chunk;

            // If adding a player, since everything will need to be loaded around it, entity can always be added
            if(entity.IsPlayer) {
                Debug.Log("Running on a player entity!");
                GetChunksInView(toCoords, true, true);  // Load all nearby chunks if not done so already
                entity.Chunk = Instance.loadedChunks[toCoords];
                SetEntityAsPlayer(entity, entity.Player);
                // Load all nearby chunks
                chunk = Instance.loadedChunks[toCoords];
                canAdd = true;
            // If adding a normal entity, whether it can be added or not depends on if the chunk it's being added to exists
            } else {
                Debug.Log("Is not a player!");
                canAdd = Instance.loadedChunks.TryGetValue(toCoords, out chunk);
            }
            
            if(canAdd) {
                chunk.AddEntity(entity);
                foreach(NewSChunk chunkInView in GetChunksInView(entity.Chunk.Coords, true, false)) {
                    chunkInView.AddEntityIntoRenderDistance(entity);
                }
            }

            return canAdd;
        }

        ///<summary> Removes an entity from the chunk system </summary>
        public static void RemoveEntityFromSystem(SEntity entity) {
            if(entity.IsPlayer)
                UnsetEntityAsPlayer(entity, entity.Player);

            entity.Chunk.RemoveEntity(entity);
            foreach(NewSChunk chunkInView in GetChunksInView(entity.Chunk.Coords, true, false)) {
                chunkInView.RemoveEntityFromRenderDistance(entity);
            }
        }


        ///<summary> Increases the amount of player viewers in surrounding chunks by 1 </para>
        private static void SetEntityAsPlayer(SEntity entity, SPlayer player) {
            foreach(NewSChunk chunkInView in GetChunksInView(entity.Chunk.Coords, true, true)) {
                chunkInView.AddPlayer(player);
            }
        }

        ///<summary> Decreases the amount of player viewers in surrounding chunks by 1 </summary>
        private static void UnsetEntityAsPlayer(SEntity entity, SPlayer player) {
            foreach(NewSChunk chunkInView in GetChunksInView(entity.Chunk.Coords, true, false)) {
                chunkInView.RemovePlayer(player);
            }
        }


        ///<summary> Moves an entity from its current chunk to a new chunk </summary>
        public static void MoveEntity(SEntity entity, Vector2Int toCoords) {
            if(entity.Chunk.Coords == toCoords) {
                Debug.LogError("Tried to move entity to the chunk it was already in!");
                return;
            }
            
            // If this is a player:
            // - Register and unregister from new and old chunks respectively
            // - Tell them what entities to load and unload
            if(entity.IsPlayer) {
                SPlayer player = entity.Player;
                GetChunkDeltas(entity.Chunk.Coords, toCoords, out var chunksEntering, out var chunksLeaving, true);
                
                foreach(NewSChunk chunkLeaving in chunksLeaving) {
                    chunkLeaving.RemovePlayer(player);
                    chunkLeaving.RemoveEntityFromRenderDistance(entity);
                    // Tell client to unload all entities it's leaving
                    foreach(SEntity entityToUnload in chunkLeaving.GetEntities()) {
                        player.ReliablePackets?.AddPacket(SNetManager.Tick,
                            new SEntityKillPkt() {
                                entityId = entityToUnload.Id,
                                reason = WEntityKillReason.Unload
                            }
                        );
                    }
                }

                foreach(NewSChunk chunkEntering in chunksEntering) {
                    chunkEntering.AddPlayer(player);
                    chunkEntering.AddEntityIntoRenderDistance(entity);
                    // Tell client to load all entities it's entering
                    foreach(SEntity entityToLoad in chunkEntering.GetEntities()) {
                        player.ReliablePackets?.AddPacket(SNetManager.Tick,
                            new SEntitySpawnPkt() {
                                entity = entityToLoad.GetSerializedEntity(SNetManager.Tick),
                                reason = WEntitySpawnReason.Load
                            }
                        );
                    }
                }

                NewSChunk destination = Instance.loadedChunks[toCoords];
                entity.Chunk.RemoveEntity(entity);
                entity.Chunk = destination;
                destination.AddEntity(entity);
            } else {
                bool isMovingIntoLoadedChunk = Instance.loadedChunks.TryGetValue(toCoords, out var toChunk);
                GetChunkDeltas(entity.Chunk.Coords, toCoords, out var chunksEntering, out var chunksLeaving, false);

                foreach(NewSChunk chunkLeaving in chunksLeaving) {
                    chunkLeaving.RemoveEntityFromRenderDistance(entity);
                }
                foreach(NewSChunk chunkEntering in chunksEntering) {
                    chunkEntering.AddEntityIntoRenderDistance(entity);
                }

                entity.Chunk.RemoveEntity(entity);
                if(isMovingIntoLoadedChunk) {
                    toChunk.AddEntity(entity);
                } else {
                    entity.StartDeath(WEntityKillReason.Unload);
                }
            }
        }

        ///<summary> Outputs the chunks being left and entered when moving between fromCoords and toCoords </summary>
        private static void GetChunkDeltas
        (Vector2Int fromCoords, Vector2Int toCoords, out HashSet<NewSChunk> enteringChunks, out HashSet<NewSChunk> leavingChunks,
        bool generateUnloadedChunks = false)
        {
            HashSet<NewSChunk> fromChunksInView = GetChunksInView(fromCoords);
            HashSet<NewSChunk> toChunksInView = GetChunksInView(toCoords, true, generateUnloadedChunks);

            leavingChunks = new(fromChunksInView);
            leavingChunks.ExceptWith(toChunksInView);
            enteringChunks = new(toChunksInView);
            enteringChunks.ExceptWith(fromChunksInView);
        }


        private void StartExpiringChunk(NewSChunk chunk) {
            expiringChunks[chunk.Coords] = DateTime.Now + chunkExpirationTime;
        }

        private void StopExpiringChunk(NewSChunk chunk) {
            expiringChunks.Remove(chunk.Coords);
        }


        public static void CleanupAfterSnapshot() {
            foreach(NewSChunk chunk in Instance.chunksWithUpdates) {
                chunk.ResetUpdates();
            }
            Instance.chunksWithUpdates.Clear();
            
            // Unload chunks that need to be unloaded
            // Needs to copy list each time this is ran -- this sucks, consider finding a better way to do this
            foreach(var(coords, timeToUnload) in Instance.expiringChunks.ToList()) {
                if(timeToUnload > DateTime.Now) {
                    Instance.loadedChunks[coords].Unload();
                    Instance.expiringChunks.Remove(coords);
                }
            }
        }


        private void RegisterChunkWithUpdates(NewSChunk chunk) {
            chunksWithUpdates.Add(chunk);
        }


        public static bool TryGetPlayerUpdates(SPlayer player, bool getReliable, out NetDataWriter chunkUpdates) {
            NewSChunk playerChunk = player.Entity.Chunk;
            HashSet<NewSChunk> chunksInView = GetChunksInView(playerChunk.Coords, true, true);
            return getReliable ? 
                playerChunk.TryGet3x3ReliableUpdates(chunksInView, out chunkUpdates) :
                playerChunk.TryGet3x3UnreliableUpdates(chunksInView, out chunkUpdates);
        }
    }
}