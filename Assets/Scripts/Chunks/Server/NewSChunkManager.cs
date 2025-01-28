using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using LiteNetLib.Utils;
using Networking.Shared;
using UnityEngine;

namespace Networking.Server {
    public class NewSChunkManager : BaseSingleton<NewSChunkManager> {
        private static readonly Vector2Int[] offsetsWithCenter = new Vector2Int[] {
            new(-1,  1), new( 0,  1), new( 1,  1),
            new(-1,  0), new( 0,  0), new( 1,  0),
            new(-1, -1), new( 0, -1), new( 1, -1),
        };
        private static readonly Vector2Int[] offsetsWithoutCenter = new Vector2Int[] {
            new(-1,  1), new( 0,  1), new( 1,  1),
            new(-1,  0),              new( 1,  0),
            new(-1, -1), new( 0, -1), new( 1, -1),
        };

        private const float chunkSize = 32;
        private Dictionary<Vector2Int, NewSChunk> loadedChunks = new();
        
        ///<summary> Chunks with updates need to be reset every snapshot </summary>
        private List<NewSChunk> chunksWithUpdatesCache = new();
        private readonly TimeSpan chunkExpirationTime = new(0, 0, 10);
        ///<summary> Chunk coordinates and their expiration dates </summary>
        private Dictionary<Vector2Int, DateTime> expiringChunksCache = new();

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
            SEntityManager.EntityDeleted += RemoveEntityFromSystem;
            base.Awake();
        }

        protected override void OnDestroy() {
            Debug.Log("Destroying new chunk manager!");
            SEntity.SetAsPlayer -= SetEntityAsPlayer;
            SEntity.UnsetAsPlayer -= UnsetEntityAsPlayer;
            NewSChunk.StartExpiring -= StartExpiringChunk;
            NewSChunk.StopExpiring -= StopExpiringChunk;
            NewSChunk.HasAnUpdate -= RegisterChunkWithUpdates;
            SEntityManager.EntityDeleted -= RemoveEntityFromSystem;
            base.OnDestroy();
        }

        private NewSChunk GetOrGenerateChunk(Vector2Int coords) {
            if(!loadedChunks.TryGetValue(coords, out NewSChunk chunk)) {
                chunk = new(coords);
                loadedChunks[coords] = chunk;
            }

            return chunk;
        }

        /// <summary> Gets all chunks surrounding a given chunk coordinat. </summary>
        /// <param name="generateIfNotExists"> Should neighboring chunks that don't exist be generated? </param>
        private HashSet<NewSChunk> GetChunksInView(Vector2Int centerCoords, bool getCenter = true, bool generateIfNotExists = false) {
            Vector2Int[] offsets = getCenter ? offsetsWithCenter : offsetsWithoutCenter;
            HashSet<NewSChunk> ret = new();
            
            foreach(Vector2Int offset in offsets) {
                Vector2Int offsetCoords = centerCoords + offset;
                
                NewSChunk chunkInView;
                
                if(generateIfNotExists)
                    chunkInView = GetOrGenerateChunk(offsetCoords);
                else if(!loadedChunks.TryGetValue(offsetCoords, out chunkInView))
                    continue;

                ret.Add(chunkInView);
            }

            return ret;
        }


        ///<summary> Adds an entity to the chunk system </summary>
        public bool AddEntityAndOrPlayerToSystem(SEntity entity, Vector2Int toCoords, SPlayer optionalPlayer = null) {
            bool canAdd;
            NewSChunk chunk;

            // If adding a player, since everything will need to be loaded around it, entity can always be added
            if(optionalPlayer != null) {
                canAdd = true;

                chunk = GetOrGenerateChunk(toCoords);

                entity.Chunk = chunk;
                entity.ChangePlayer(optionalPlayer);
            }
            // If adding a normal entity, whether it can be added or not depends on if the chunk it's being added to exists
            else {
                canAdd = loadedChunks.TryGetValue(toCoords, out chunk);
                entity.Chunk = chunk;
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
        public void RemoveEntityFromSystem(SEntity entity) {
            if(entity.IsPlayer)
                UnsetEntityAsPlayer(entity, entity.Player);

            entity.Chunk.RemoveEntity(entity);
            foreach(NewSChunk chunkInView in GetChunksInView(entity.Chunk.Coords, true, false)) {
                chunkInView.RemoveEntityFromRenderDistance(entity);
            }
        }


        private void ChangeEntityPlayerStatus(SEntity entity, SPlayer player, bool setAsPlayer) {
            HashSet<NewSChunk> chunksInView = GetChunksInView(entity.Chunk.Coords, true, setAsPlayer);

            foreach(NewSChunk chunkInView in chunksInView) {
                if(setAsPlayer) {
                    chunkInView.AddPlayer(player);
                } else {
                    chunkInView.RemovePlayer(player);
                }
            }
        }

        private void SetEntityAsPlayer(SEntity entity, SPlayer player) =>
            ChangeEntityPlayerStatus(entity, player, true);

        private void UnsetEntityAsPlayer(SEntity entity, SPlayer player) =>
            ChangeEntityPlayerStatus(entity, player, false);


        ///<summary> Moves an entity from its current chunk to a new chunk </summary>
        public void MoveEntity(SEntity entity, Vector2Int toCoords) {
            if(entity.Chunk.Coords == toCoords) {
                Debug.LogError("Tried to move entity to the chunk it was already in!");
                return;
            }
            
            if(entity.IsPlayer) {
                // - Register and unregister from new and old chunks respectively
                // - Tell them what entities to load and unload
                SPlayer player = entity.Player;
                GetChunkDeltas(entity.Chunk.Coords, toCoords, out var chunksEntering, out var chunksLeaving, true);
                
                // Updates chunks and the player of changes resulting from moving the player
                void UpdatePlayerPresence(HashSet<NewSChunk> chunkDeltas, Action<NewSChunk, SPlayer> addOrRemovePlayer, 
                Action<NewSChunk, SEntity> addOrRemoveEntityFromRenderDistance, Func<SEntity, BasePacket> generatePacketToSend) {
                    // For each chunk which is having its visibility changed,
                    foreach(NewSChunk chunkDelta in chunkDeltas) {
                        // Add or remove this player and its entity as being visible,
                        addOrRemovePlayer(chunkDelta, player);
                        addOrRemoveEntityFromRenderDistance(chunkDelta, entity);
                        // And then notify the player of all entities to spawn/despawn
                        foreach(SEntity entityToLoadOrUnload in chunkDelta.GetEntities()) {
                            player.ReliablePackets?.AddPacket(SNetManager.Tick, generatePacketToSend(entityToLoadOrUnload));
                        }
                    }}

                UpdatePlayerPresence(
                    chunksLeaving, 
                    (chunkLeaving, player) => chunkLeaving.RemovePlayer(player), 
                    (chunkLeaving, entity) => chunkLeaving.RemoveEntityFromRenderDistance(entity),
                    (entityToLoad) => new SEntityKillPkt() {
                        entityId = entityToLoad.Id,
                        reason = WEntityKillReason.Unload
                    });

                UpdatePlayerPresence(
                    chunksEntering, 
                    (chunkEntering, player) => chunkEntering.AddPlayer(player), 
                    (chunkEntering, entity) => chunkEntering.AddEntityIntoRenderDistance(entity),
                    (entityToUnload) => new SEntitySpawnPkt() {
                        entity = entityToUnload.GetSerializedEntity(SNetManager.Tick),
                        reason = WEntitySpawnReason.Load
                    });

                NewSChunk destination = loadedChunks[toCoords];
                entity.Chunk.RemoveEntity(entity);
                entity.Chunk = destination;
                destination.AddEntity(entity);
            } else {
                bool isMovingIntoLoadedChunk = loadedChunks.TryGetValue(toCoords, out var toChunk);
                GetChunkDeltas(entity.Chunk.Coords, toCoords, out var chunksEntering, out var chunksLeaving, false);

                foreach(NewSChunk chunkLeaving in chunksLeaving) {
                    chunkLeaving.RemoveEntityFromRenderDistance(entity);
                }
                foreach(NewSChunk chunkEntering in chunksEntering) {
                    chunkEntering.AddEntityIntoRenderDistance(entity);
                }

                entity.Chunk.RemoveEntity(entity);

                // If a non-player entity moves into an unloaded chunk, kill it
                if(isMovingIntoLoadedChunk) {
                    toChunk.AddEntity(entity);
                } else {
                    entity.StartDeath(WEntityKillReason.Unload);
                }
            }
        }

        ///<summary> Outputs the chunks being left and entered when moving between fromCoords and toCoords </summary>
        private void GetChunkDeltas
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
            expiringChunksCache[chunk.Coords] = DateTime.Now + chunkExpirationTime;
        }

        private void StopExpiringChunk(NewSChunk chunk) {
            expiringChunksCache.Remove(chunk.Coords);
        }

        public void CleanupAfterSnapshot() {
            foreach(NewSChunk chunk in chunksWithUpdatesCache) {
                chunk.ResetUpdates();
            }
            chunksWithUpdatesCache.Clear();
            
            // Unload chunks that need to be unloaded
            // Needs to copy list each time this is ran -- this sucks, consider finding a better way to do this
            foreach(var(coords, timeToUnload) in expiringChunksCache.ToList()) {
                if(timeToUnload > DateTime.Now) {
                    loadedChunks[coords].Unload();
                    expiringChunksCache.Remove(coords);
                }
            }
        }

        private void RegisterChunkWithUpdates(NewSChunk chunk) {
            chunksWithUpdatesCache.Add(chunk);
        }

        public bool TryGetPlayerUpdates(SPlayer player, bool getReliable, out NetDataWriter chunkUpdates) {
            NewSChunk playerChunk = player.Entity.Chunk;
            HashSet<NewSChunk> chunksInView = GetChunksInView(playerChunk.Coords, true, true);
            return getReliable ? 
                playerChunk.TryGet3x3ReliableUpdates(chunksInView, out chunkUpdates) :
                playerChunk.TryGet3x3UnreliableUpdates(chunksInView, out chunkUpdates);
        }
    }
}