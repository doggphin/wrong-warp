using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using LiteNetLib.Utils;
using Networking.Shared;
using UnityEngine;

namespace Networking.Server {
    public class SChunkManager : BaseSingleton<SChunkManager> {
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
        private Dictionary<Vector2Int, SChunk> loadedChunks = new();
        
        ///<summary> Chunks with updates need to be reset every snapshot </summary>
        private List<SChunk> chunksWithUpdatesCache = new();
        private readonly TimeSpan chunkExpirationTime = new(0, 0, 0);
        ///<summary> Chunk coordinates and their expiration dates </summary>
        private Dictionary<Vector2Int, DateTime> expiringChunksCache = new();

        public static Vector2Int ProjectToGrid(Vector3 position) {
            Vector2Int ret = new(Mathf.RoundToInt(position.x / chunkSize), Mathf.RoundToInt(position.z / chunkSize));
            return ret;
        }

        protected override void Awake() {
            SEntity.SetAsPlayer += SetEntityAsPlayer;
            SEntity.UnsetAsPlayer += UnsetEntityAsPlayer;
            SChunk.StartExpiring += StartExpiringChunk;
            SChunk.StopExpiring += StopExpiringChunk;
            SChunk.HasAnUpdate += RegisterChunkWithUpdates;
            SEntityManager.EntityDeleted += RemoveEntityFromSystem;
            base.Awake();
        }

        protected override void OnDestroy() {
            SEntity.SetAsPlayer -= SetEntityAsPlayer;
            SEntity.UnsetAsPlayer -= UnsetEntityAsPlayer;
            SChunk.StartExpiring -= StartExpiringChunk;
            SChunk.StopExpiring -= StopExpiringChunk;
            SChunk.HasAnUpdate -= RegisterChunkWithUpdates;
            SEntityManager.EntityDeleted -= RemoveEntityFromSystem;
            base.OnDestroy();
        }

        private SChunk GetOrGenerateChunk(Vector2Int coords) {
            if(!loadedChunks.TryGetValue(coords, out SChunk chunk)) {
                chunk = new(coords);
                loadedChunks[coords] = chunk;
            }

            return chunk;
        }

        /// <summary> Gets all chunks surrounding a given chunk coordinat. </summary>
        /// <param name="generateIfNotExists"> Should neighboring chunks that don't exist be generated? </param>
        private HashSet<SChunk> GetChunksInView(Vector2Int centerCoords, bool getCenter = true, bool generateIfNotExists = false) {
            Vector2Int[] offsets = getCenter ? offsetsWithCenter : offsetsWithoutCenter;
            HashSet<SChunk> ret = new();
            
            foreach(Vector2Int offset in offsets) {
                Vector2Int offsetCoords = centerCoords + offset;
                
                SChunk chunkInView;
                
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
            SChunk chunk;

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
                foreach(SChunk chunkInView in GetChunksInView(entity.Chunk.Coords, true, false)) {
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
            foreach(SChunk chunkInView in GetChunksInView(entity.Chunk.Coords, true, false)) {
                chunkInView.RemoveEntityFromRenderDistance(entity);
            }
        }


        private void ChangeEntityPlayerStatus(SEntity entity, SPlayer player, bool setAsPlayer) {
            HashSet<SChunk> chunksInView = GetChunksInView(entity.Chunk.Coords, true, setAsPlayer);

            foreach(SChunk chunkInView in chunksInView) {
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


        ///<summary> Returns non-player entities moving into unloaded chunks (which must be killed) </summary>
        public IEnumerable<SEntity> UpdateEntityChunkLocations(List<SEntity> entities, int tick) {
            return entities.Where(entity => !MoveEntity(entity, ProjectToGrid(entity.GetPosition(tick)), tick));
        }
            


        ///<summary> Moves an entity from its current chunk to a new chunk </summary>
        ///<returns> Whether the entity is moving into an unloaded chunk and won't be able to load it </returns>
        private bool MoveEntity(SEntity entity, Vector2Int toCoords, int tick) {
            if(entity.Chunk.Coords == toCoords) {
                return true;
            }
            
            bool isPlayer = entity.IsPlayer;
            SPlayer player = entity.Player;
            GetChunkDeltas(entity.Chunk.Coords, toCoords, out var chunksEntering, out var chunksLeaving, isPlayer);
            bool canEnterNewChunk = loadedChunks.TryGetValue(toCoords, out var toChunk) || isPlayer;
            
            void UpdatePresence(HashSet<SChunk> chunkDeltas, 
            Action<SChunk, SPlayer> addOrRemovePlayer, 
            Action<SChunk, SEntity> addOrRemoveEntityFromRenderDistance,
            Func<SEntity, BasePacket> generatePacketToSend) {
                // For each chunk which is having its visibility changed,
                foreach(SChunk chunkDelta in chunkDeltas) {
                    if(isPlayer) {
                        // Add or remove this player and its entity as being visible,
                        addOrRemovePlayer(chunkDelta, player);
                    }
                    
                    addOrRemoveEntityFromRenderDistance(chunkDelta, entity);

                    // And then notify the player of all entities to spawn/despawn
                    if(isPlayer) {
                        foreach(SEntity entityToLoadOrUnload in chunkDelta.GetEntities()) {
                            player.ReliablePackets?.AddPacket(tick, generatePacketToSend(entityToLoadOrUnload));
                        }
                    }   
                }
            }

            UpdatePresence(
                chunksLeaving, 
                (chunkLeaving, player) => chunkLeaving.RemovePlayer(player), 
                (chunkLeaving, entity) => chunkLeaving.RemoveEntityFromRenderDistance(entity),
                (entityToLoad) => new SEntityKillPkt(){
                    entityId = entityToLoad.Id,
                    reason = WEntityKillReason.Unload}
            );
            entity.Chunk.RemoveEntity(entity);

            if(canEnterNewChunk) {
                UpdatePresence(
                    chunksEntering, 
                    (chunkEntering, player) => chunkEntering.AddPlayer(player), 
                    (chunkEntering, entity) => chunkEntering.AddEntityIntoRenderDistance(entity),
                    (entityToUnload) => new SEntitySpawnPkt() {
                        entity = entityToUnload.GetSerializedEntity(tick),
                        reason = WEntitySpawnReason.Load}
                );

                entity.Chunk = toChunk;
                toChunk.AddEntity(entity);
            }
            
            return canEnterNewChunk;
        }

        ///<summary> Outputs the chunks being left and entered when moving between fromCoords and toCoords </summary>
        private void GetChunkDeltas
        (Vector2Int fromCoords, Vector2Int toCoords, out HashSet<SChunk> enteringChunks, out HashSet<SChunk> leavingChunks,
        bool generateUnloadedChunks = false)
        {
            HashSet<SChunk> fromChunksInView = GetChunksInView(fromCoords);
            HashSet<SChunk> toChunksInView = GetChunksInView(toCoords, true, generateUnloadedChunks);

            leavingChunks = new(fromChunksInView);
            leavingChunks.ExceptWith(toChunksInView);
            enteringChunks = new(toChunksInView);
            enteringChunks.ExceptWith(fromChunksInView);
        }

        private void StartExpiringChunk(SChunk chunk) {
            expiringChunksCache[chunk.Coords] = DateTime.Now + chunkExpirationTime;
        }
            

        private void StopExpiringChunk(SChunk chunk) {
            expiringChunksCache.Remove(chunk.Coords);
        }
            
        public void CleanupAfterSnapshot() {
            foreach(SChunk chunk in chunksWithUpdatesCache) {
                chunk.ResetUpdates();
            }

            chunksWithUpdatesCache.Clear();
            
            // Unload chunks that need to be unloaded
            foreach(var(coords, timeToUnload) in expiringChunksCache.ToList()) {
                if(timeToUnload < DateTime.Now) {
                    loadedChunks[coords].Unload();
                    expiringChunksCache.Remove(coords);
                    loadedChunks.Remove(coords);
                }
            }
        }

        private void RegisterChunkWithUpdates(SChunk chunk) =>
            chunksWithUpdatesCache.Add(chunk);

        private bool TryGetPlayerUpdates(SPlayer player, bool getReliable, out NetDataWriter chunkUpdates) {
            SChunk playerChunk = player.Entity.Chunk;
            HashSet<SChunk> chunksInView = GetChunksInView(playerChunk.Coords, true, true);
            return getReliable ? 
                playerChunk.TryGet3x3ReliableUpdates(chunksInView, out chunkUpdates) :
                playerChunk.TryGet3x3UnreliableUpdates(chunksInView, out chunkUpdates);
        }

        public bool TryGetUnreliablePlayerUpdates(SPlayer player, out NetDataWriter chunkUpdates) =>
            TryGetPlayerUpdates(player, false, out chunkUpdates);

        public bool TryGetReliablePlayerUpdates(SPlayer player, out NetDataWriter chunkUpdates) =>
            TryGetPlayerUpdates(player, true, out chunkUpdates);
    }
}