using LiteNetLib.Utils;
using Networking.Server;
using System;
using System.Collections.Generic;
using UnityEngine;

using Networking.Shared;

namespace Networking.Server {
    public class WNetChunk {
        private HashSet<WNetServerEntity> presentLoaders = new();
        public HashSet<WNetServerEntity> PresentEntities { get; private set; } = new();

        List<INetSerializable>[] generalUpdates;
        Dictionary<int, List<INetSerializable>>[] entityUpdates;

        private bool isLoaded;
        public Vector2Int Coords { get; private set; }


        private WSChunkDeltaSnapshotPkt deltaSnapshot = null;
        private bool isDeltaSnapshot3x3PktWritten = false;
        private NetDataWriter deltaSnapshot3x3PktWriter = new();

        private NetDataWriter GetPrepared3x3WorldStatePacket() {
            throw new NotImplementedException();
        }
        

        public NetDataWriter GetPrepared3x3SnapshotPacket() {
            if (isDeltaSnapshot3x3PktWritten) {
                Debug.Log("Returning already written snapshot!");
                return deltaSnapshot3x3PktWriter;
            }

            deltaSnapshot3x3PktWriter.Reset();

            WNetPacketComms.StartMultiPacket(deltaSnapshot3x3PktWriter, WNetServer.Instance.Tick);
            GetSnapshot().Serialize(deltaSnapshot3x3PktWriter);

            WNetChunk[] neighbors = WNetServerChunkManager.GetNeighboringChunks(Coords, false, false);
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
            for (int i = 0; i < WNetCommon.TICKS_PER_SNAPSHOT; i++) {
                generalUpdates[i].Clear();
                entityUpdates[i].Clear();
            }

            deltaSnapshot = null;
            isDeltaSnapshot3x3PktWritten = false;
        }


        public void Load(Vector2Int coords) {
            isLoaded = true;
            generalUpdates = new List<INetSerializable>[WNetCommon.TICKS_PER_SNAPSHOT];
            entityUpdates = new Dictionary<int, List<INetSerializable>>[WNetCommon.TICKS_PER_SNAPSHOT];

            for (int i = 0; i < WNetCommon.TICKS_PER_SNAPSHOT; i++) {
                generalUpdates[i] = new();
                entityUpdates[i] = new();
            }

            Coords = coords;
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

            if (!entityUpdates[tick % WNetCommon.TICKS_PER_SNAPSHOT].TryGetValue(id, out var entityUpdatesList)) {
                List<INetSerializable> newEntityUpdatesList = new();
                entityUpdates[tick % WNetCommon.TICKS_PER_SNAPSHOT].Add(id, newEntityUpdatesList);
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

            generalUpdates[tick].Add(update);

            return true;
        }


        public void Unload() {
            isLoaded = false;
        }


        public void AddChunkLoader(WNetServerEntity entity) {
            if (presentLoaders.Count == 0) {
                WNetServerChunkManager.chunksMarkedToUnload.Remove(Coords);
            }
                

            presentLoaders.Add(entity);
        }


        public void RemoveChunkLoader(WNetServerEntity loader) {
            presentLoaders.Remove(loader);

            if (presentLoaders.Count == 0) {
                WNetServerChunkManager.chunksMarkedToUnload.Add(Coords);
            }  
        }
    }
}
