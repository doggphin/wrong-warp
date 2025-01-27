using LiteNetLib.Utils;
using System.Collections.Generic;
using UnityEngine;

using Networking.Shared;

namespace Networking.Server {
    // TODO: rewrite this abomination (1/22/25)
    public class SChunk {
        private HashSet<SEntity> presentLoaders = new();
        public HashSet<SEntity> PresentEntities { get; private set; } = new();
        public List<BasePacket>[] ReliableUpdates { get; private set; } = new List<BasePacket>[NetCommon.TICKS_PER_SNAPSHOT];
        private List<BasePacket>[] unreliableGeneralUpdates = new List<BasePacket>[NetCommon.TICKS_PER_SNAPSHOT];
        private Dictionary<int, List<BasePacket>>[] unreliableEntityUpdates = new Dictionary<int, List<BasePacket>>[NetCommon.TICKS_PER_SNAPSHOT];
        private bool isLoaded = false;
        public Vector2Int Coords { get; private set; }

        public void Load(Vector2Int coords) {
            isLoaded = true;
            Coords = coords;

            for (int i = 0; i < NetCommon.TICKS_PER_SNAPSHOT; i++) {
                unreliableGeneralUpdates[i] = new();
                unreliableEntityUpdates[i] = new();
                ReliableUpdates[i] = new();
            }
        }


        // Chunks will write their own WSChunkDeltaSnapshotPkts and cache them for each group of ticks.
        // Each player within each chunk is sent the WSChunkDeltaSnapshotPkt of each chunk in a 3x3 radius around them.
        // Work can be reduced by reusing individual chunk WSChunkDeltaSnapshotPkts and sending the same combined 3x3 chunks to players in same chunk
        private SChunkDeltaSnapshotPkt cachedUnreliableDeltaSnapshot = null;
        private bool isUnreliableDeltaSnapshot3x3PktWritten = false;
        private NetDataWriter unreliableDeltaSnapshot3x3PktWriter = new();
        public NetDataWriter GetPrepared3x3UnreliableDeltaSnapshotPacket() {
            if (isUnreliableDeltaSnapshot3x3PktWritten)
                return unreliableDeltaSnapshot3x3PktWriter;

            PacketCommunication.StartMultiPacket(unreliableDeltaSnapshot3x3PktWriter, SNetManager.Instance.GetTick());
            foreach(var chunk in SChunkManager.GetNeighboringChunks(Coords, true, false))
                chunk.GetUnreliableDeltaSnapshot().Serialize(unreliableDeltaSnapshot3x3PktWriter);

            isUnreliableDeltaSnapshot3x3PktWritten = true;
            return unreliableDeltaSnapshot3x3PktWriter;
        }
        private SChunkDeltaSnapshotPkt GetUnreliableDeltaSnapshot() {
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
            List<BasePacket>[] updates = new List<BasePacket>[NetCommon.TICKS_PER_SNAPSHOT];
            var chunks = SChunkManager.GetNeighboringChunks(Coords, true, false);
            bool containsUpdates = false;
            for(int updateIndex=0; updateIndex<NetCommon.TICKS_PER_SNAPSHOT; updateIndex++) {
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

            PacketCommunication.StartMultiPacket(reliableUpdates3x3PktWriter, SNetManager.Instance.GetTick());
            new SChunkReliableUpdatesPkt() { updates = updates, startTick = SNetManager.Instance.GetTick() - NetCommon.TICKS_PER_SNAPSHOT }.Serialize(reliableUpdates3x3PktWriter);

            isReliableUpdates3x3Written = true;
            return reliableUpdates3x3PktWriter;
        }


        public void ResetUpdates() {
            for (int i = 0; i < NetCommon.TICKS_PER_SNAPSHOT; i++) {
                unreliableGeneralUpdates[i].Clear();
                unreliableEntityUpdates[i].Clear();
                ReliableUpdates[i].Clear();
            }

            cachedUnreliableDeltaSnapshot = null;
            isUnreliableDeltaSnapshot3x3PktWritten = false;

            isReliableUpdates3x3Written = false;
        }


        /// <returns> Whether this chunk is loaded. </returns>
        public bool AddEntityUpdate(int tick, int id, BasePacket update) {
            if (!isLoaded)
                return false;

            // Want insertAt to start at 0, not 1. Need to do add ticksPerSnapshot - 1 to make it start at 0
            int insertAt = (tick + NetCommon.TICKS_PER_SNAPSHOT - 1) % NetCommon.TICKS_PER_SNAPSHOT;

            if (!unreliableEntityUpdates[insertAt].TryGetValue(id, out var entityUpdatesList)) {
                List<BasePacket> newEntityUpdatesList = new();

                unreliableEntityUpdates[insertAt][id] = newEntityUpdatesList;
                entityUpdatesList = newEntityUpdatesList;
            }

            entityUpdatesList.Add(update);

            return true;
        }


        /// <returns> Whether the chunk is loaded. </returns>
        public bool AddGenericUpdate(int tick, BasePacket update, bool reliable) {
            if (!isLoaded)
                return false;
            
            List<BasePacket>[] updatesToAppendTo = reliable ? ReliableUpdates : unreliableGeneralUpdates;
            updatesToAppendTo[tick % NetCommon.TICKS_PER_SNAPSHOT].Add(update);

            return true;
        }


        public void Unload() {
            isLoaded = false;
        }


        public void AddChunkLoader(SEntity entity) {
            if (presentLoaders.Count == 0) {
                SChunkManager.Instance.chunksMarkedToUnload.Remove(Coords);
            }

            presentLoaders.Add(entity);
        }


        public void RemoveChunkLoader(SEntity loader) {
            presentLoaders.Remove(loader);

            if (presentLoaders.Count == 0) {
                SChunkManager.Instance.chunksMarkedToUnload.Add(Coords);
            }  
        }

        // This is called from WSEntityManager
        public void KillEntity(SEntity entity, WEntityKillReason killReason) {
            PresentEntities.Remove(entity);
            SEntityKillPkt packet = new() { reason = killReason };
            AddEntityUpdate(SNetManager.Instance.GetTick(), entity.Id, packet);

            if(entity.IsChunkLoader)
                SChunkManager.RemoveChunkLoader(Coords, entity, true);
            
        }

        // This is called from WSEntityManager
        public void SpawnEntity(SEntity entity, WEntitySpawnReason reason) {
            PresentEntities.Add(entity);
            SEntitySpawnPkt packet = new()
            {
                entity = entity.GetSerializedEntity(SNetManager.Instance.GetTick()),
                reason = reason
            };
            
            AddEntityUpdate(SNetManager.Tick, entity.Id, packet);
        }
    }
}
