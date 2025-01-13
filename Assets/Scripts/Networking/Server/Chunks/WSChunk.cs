using LiteNetLib.Utils;
using System.Collections.Generic;
using UnityEngine;

using Networking.Shared;

namespace Networking.Server {
    public class WSChunk {
        private HashSet<WSEntity> presentLoaders = new();
        public HashSet<WSEntity> PresentEntities { get; private set; } = new();
        public List<INetSerializable>[] ReliableUpdates { get; private set; } = new List<INetSerializable>[WCommon.TICKS_PER_SNAPSHOT];
        private List<INetSerializable>[] unreliableGeneralUpdates = new List<INetSerializable>[WCommon.TICKS_PER_SNAPSHOT];
        private Dictionary<int, List<INetSerializable>>[] unreliableEntityUpdates = new Dictionary<int, List<INetSerializable>>[WCommon.TICKS_PER_SNAPSHOT];
        private bool isLoaded = false;
        public Vector2Int Coords { get; private set; }

        public void Load(Vector2Int coords) {
            isLoaded = true;
            Coords = coords;

            for (int i = 0; i < WCommon.TICKS_PER_SNAPSHOT; i++) {
                unreliableGeneralUpdates[i] = new();
                unreliableEntityUpdates[i] = new();
                ReliableUpdates[i] = new();
            }
        }


        // Chunks will write their own WSChunkDeltaSnapshotPkts and cache them for each group of ticks.
        // Each player within each chunk is sent the WSChunkDeltaSnapshotPkt of each chunk in a 3x3 radius around them.
        // Work can be reduced by reusing individual chunk WSChunkDeltaSnapshotPkts and sending the same combined 3x3 chunks to players in same chunk
        private WSChunkDeltaSnapshotPkt cachedUnreliableDeltaSnapshot = null;
        private bool isUnreliableDeltaSnapshot3x3PktWritten = false;
        private NetDataWriter unreliableDeltaSnapshot3x3PktWriter = new();
        public NetDataWriter GetPrepared3x3UnreliableDeltaSnapshotPacket() {
            if (isUnreliableDeltaSnapshot3x3PktWritten)
                return unreliableDeltaSnapshot3x3PktWriter;

            WPacketCommunication.StartMultiPacket(unreliableDeltaSnapshot3x3PktWriter, WSNetServer.Tick);
            foreach(var chunk in WSChunkManager.GetNeighboringChunks(Coords, true, false))
                chunk.GetUnreliableDeltaSnapshot().Serialize(unreliableDeltaSnapshot3x3PktWriter);

            isUnreliableDeltaSnapshot3x3PktWritten = true;
            return unreliableDeltaSnapshot3x3PktWriter;
        }
        private WSChunkDeltaSnapshotPkt GetUnreliableDeltaSnapshot() {
            if (cachedUnreliableDeltaSnapshot != null)
                return cachedUnreliableDeltaSnapshot;

            cachedUnreliableDeltaSnapshot = new() { generalUpdates = unreliableGeneralUpdates, entityUpdates = unreliableEntityUpdates };
            return cachedUnreliableDeltaSnapshot;
        }


        private bool isReliableUpdates3x3Written = false;
        private NetDataWriter reliableUpdates3x3PktWriter = new();
        /// <returns> If there are reliable updates, a NetDataWriter with the written packet. Otherwise, returns null. </returns>
        public NetDataWriter GetPrepared3x3ReliableUpdatesPacket() {
            // If 3x3 reliable updates have already been written + cached, use that
            if(isReliableUpdates3x3Written)
                // Not yet tested
                return reliableUpdates3x3PktWriter.Length == 0 ? null : reliableUpdates3x3PktWriter;

            // All reliable updates within a one-chunk radius will be added to the same list
            List<INetSerializable>[] updates = new List<INetSerializable>[WCommon.TICKS_PER_SNAPSHOT];
            var chunks = WSChunkManager.GetNeighboringChunks(Coords, true, false);
            bool containsUpdates = false;
            for(int updateIndex=0; updateIndex<WCommon.TICKS_PER_SNAPSHOT; updateIndex++) {
                updates[updateIndex] = new();

                for(int chunkIndex=0; chunkIndex<chunks.Length; chunkIndex++) {
                    updates[updateIndex].AddRange(chunks[chunkIndex].ReliableUpdates[updateIndex]);
                }
                if(updates[updateIndex].Count > 0) {
                    containsUpdates = true;
                }
            }

            if(!containsUpdates) {
                isReliableUpdates3x3Written = true;
                return null;
            }

            WPacketCommunication.StartMultiPacket(reliableUpdates3x3PktWriter, WSNetServer.Tick);
            new WSChunkReliableUpdatesPkt() { updates = updates, startTick = WSNetServer.Tick - WCommon.TICKS_PER_SNAPSHOT }.Serialize(reliableUpdates3x3PktWriter);

            isReliableUpdates3x3Written = true;
            return reliableUpdates3x3PktWriter;
        }


        public void ResetUpdates() {
            for (int i = 0; i < WCommon.TICKS_PER_SNAPSHOT; i++) {
                unreliableGeneralUpdates[i].Clear();
                unreliableEntityUpdates[i].Clear();
                ReliableUpdates[i].Clear();
            }

            cachedUnreliableDeltaSnapshot = null;
            isUnreliableDeltaSnapshot3x3PktWritten = false;

            isReliableUpdates3x3Written = false;
        }


        /// <returns> Whether this chunk is loaded. </returns>
        public bool AddEntityUpdate(int tick, int id, INetSerializable update) {
            if (!isLoaded)
                return false;

            // Want insertAt to start at 0, not 1. Need to do add ticksPerSnapshot - 1 to make it start at 0
            int insertAt = (tick + WCommon.TICKS_PER_SNAPSHOT - 1) % WCommon.TICKS_PER_SNAPSHOT;

            if (!unreliableEntityUpdates[insertAt].TryGetValue(id, out var entityUpdatesList)) {
                List<INetSerializable> newEntityUpdatesList = new();

                unreliableEntityUpdates[insertAt][id] = newEntityUpdatesList;
                entityUpdatesList = newEntityUpdatesList;
            }

            entityUpdatesList.Add(update);

            return true;
        }


        /// <returns> Whether the chunk is loaded. </returns>
        public bool AddGenericUpdate(int tick, INetSerializable update, bool reliable) {
            if (!isLoaded)
                return false;
            
            List<INetSerializable>[] updatesToAppendTo = reliable ? ReliableUpdates : unreliableGeneralUpdates;
            updatesToAppendTo[tick % WCommon.TICKS_PER_SNAPSHOT].Add(update);

            return true;
        }


        public void Unload() {
            isLoaded = false;
        }


        public void AddChunkLoader(WSEntity entity) {
            if (presentLoaders.Count == 0) {
                WSChunkManager.chunksMarkedToUnload.Remove(Coords);
            }

            presentLoaders.Add(entity);
        }


        public void RemoveChunkLoader(WSEntity loader) {
            presentLoaders.Remove(loader);

            if (presentLoaders.Count == 0) {
                WSChunkManager.chunksMarkedToUnload.Add(Coords);
            }  
        }

        // This is called from WSEntityManager
        public void KillEntity(WSEntity entity, WEntityKillReason killReason) {
            PresentEntities.Remove(entity);
            WSEntityKillPkt packet = new() { reason = killReason };
            AddEntityUpdate(WSNetServer.Tick, entity.Id, packet);

            if(entity.IsChunkLoader)
                WSChunkManager.RemoveChunkLoader(Coords, entity, true);
            
        }

        // This is called from WSEntityManager
        public void SpawnEntity(WSEntity entity, WEntitySpawnReason reason) {
            PresentEntities.Add(entity);
            WSEntitySpawnPkt packet = new()
            {
                entity = entity.GetSerializedEntity(WSNetServer.Tick),
                reason = reason
            };
            //AddEntityUpdate(WSNetServer.Tick, entity.Id, packet);
        }
    }
}
