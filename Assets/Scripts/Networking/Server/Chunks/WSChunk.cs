using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

using Networking.Shared;

namespace Networking.Server {
    public class WSChunk {
        private HashSet<WSEntity> presentLoaders = new();
        public HashSet<WSEntity> PresentEntities { get; private set; } = new();
        List<INetSerializable>[] generalUpdates = new List<INetSerializable>[WCommon.TICKS_PER_SNAPSHOT];
        Dictionary<int, List<INetSerializable>>[] entityUpdates = new Dictionary<int, List<INetSerializable>>[WCommon.TICKS_PER_SNAPSHOT];
        private bool isLoaded = false;
        public Vector2Int Coords { get; private set; }

        public void Load(Vector2Int coords) {
            isLoaded = true;

            for (int i = 0; i < WCommon.TICKS_PER_SNAPSHOT; i++) {
                generalUpdates[i] = new();
                entityUpdates[i] = new();
            }

            Coords = coords;
        }


        private WSChunkDeltaSnapshotPkt deltaSnapshot = null;
        private bool isDeltaSnapshot3x3PktWritten = false;
        private NetDataWriter deltaSnapshot3x3PktWriter = new();
        public NetDataWriter GetPrepared3x3SnapshotPacket() {
            if (isDeltaSnapshot3x3PktWritten) {
                Debug.Log("Returning already written snapshot!");
                return deltaSnapshot3x3PktWriter;
            }

            deltaSnapshot3x3PktWriter.Reset();

            WPacketComms.StartMultiPacket(deltaSnapshot3x3PktWriter, WSNetServer.Tick);
            GetSnapshot().Serialize(deltaSnapshot3x3PktWriter);

            WSChunk[] neighbors = WSChunkManager.GetNeighboringChunks(Coords, false, false);
            for (int i = 0; i < 8; i++) {
                if (neighbors[i] == null) {
                    Debug.Log("A surrounding chunk was null!");
                    continue;
                }
                neighbors[i].GetSnapshot().Serialize(deltaSnapshot3x3PktWriter);
            }

            isDeltaSnapshot3x3PktWritten = true;

            return deltaSnapshot3x3PktWriter;
        }
        public WSChunkDeltaSnapshotPkt GetSnapshot() {
            if (deltaSnapshot != null)
                return deltaSnapshot;

            deltaSnapshot = new() { s_generalUpdates = generalUpdates, s_entityUpdates = entityUpdates };

            return deltaSnapshot;
        }


        public void ResetUpdates() {
            for (int i = 0; i < WCommon.TICKS_PER_SNAPSHOT; i++) {
                generalUpdates[i].Clear();
                entityUpdates[i].Clear();
            }

            deltaSnapshot = null;
            isDeltaSnapshot3x3PktWritten = false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="id"></param>
        /// <param name="update"></param>
        /// <returns> Whether this chunk is loaded. </returns>
        public bool AddEntityUpdate(int tick, int id, INetSerializable update) {
            if (!isLoaded)
                return false;

            // Want insertAt to start at 0, not 1. Need to do add ticksPerSnapshot - 1 to make it start at 0
            int insertAt = (tick + WCommon.TICKS_PER_SNAPSHOT - 1) % WCommon.TICKS_PER_SNAPSHOT;

            if (!entityUpdates[insertAt].TryGetValue(id, out var entityUpdatesList)) {
                List<INetSerializable> newEntityUpdatesList = new();

                entityUpdates[insertAt][id] = newEntityUpdatesList;
                entityUpdatesList = newEntityUpdatesList;
            }

            entityUpdatesList.Add(update);

            return true;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="update"></param>
        /// <returns> Whether this chunk is loaded. </returns>
        public bool AddGenericUpdate(int tick, INetSerializable update) {
            if (!isLoaded)
                return false;

            generalUpdates[tick % WCommon.TICKS_PER_SNAPSHOT].Add(update);

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
    }
}
