using LiteNetLib.Utils;
using Networking.Server;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Serialization;
using System.Xml;
using Unity.VisualScripting;
using UnityEngine;

namespace Networking.Shared {
    public class WNetChunk {
        private HashSet<WNetEntity> presentLoaders = new();
        public HashSet<WNetEntity> PresentEntities { get; private set; } = new();

        List<INetSerializable>[] generalUpdates;
        Dictionary<int, List<INetSerializable>>[] entityUpdates;

        private bool isLoaded;
        public Vector2Int Coords { get; private set; }


        private WSChunkSnapshotPkt snapshot = null;
        private bool is3x3SnapshotsWritten = false;
        private NetDataWriter writer = new();


        public NetDataWriter GetStartedMultiPacketWith3x3Snapshot() {
            if (is3x3SnapshotsWritten) {
                Debug.Log("Returning already written snapshot!");
                return writer;
            }
                

            Debug.Log("Writing a new snapshot!");
            writer.Reset();

            WNetPacketComms.StartMultiPacket(writer, WNetServer.Instance.Tick);
            GetSnapshot().Serialize(writer);

            WNetChunk[] neighbors = WNetChunkManager.GetNeighboringChunks(Coords, false, false);
            for (int i = 0; i < 8; i++) {
                if (neighbors[i] == null) {
                    Debug.Log("A surrounding chunk was null!");
                    continue;
                }
                neighbors[i].GetSnapshot().Serialize(writer);
            }

            is3x3SnapshotsWritten = true;

            return writer;
        }


        public WSChunkSnapshotPkt GetSnapshot() {
            if (snapshot != null)
                return snapshot;

            snapshot = new() { s_generalUpdates = generalUpdates, s_entityUpdates = entityUpdates };

            return snapshot;
        }


        public void ResetUpdates() {
            for (int i = 0; i < WNetCommon.TICKS_PER_SNAPSHOT; i++) {
                generalUpdates[i].Clear();
                entityUpdates[i].Clear();
            }

            snapshot = null;
            is3x3SnapshotsWritten = false;
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


        public void AddChunkLoader(WNetEntity entity) {
            if (presentLoaders.Count == 0) {
                WNetChunkManager.chunksMarkedToUnload.Remove(Coords);
            }
                

            presentLoaders.Add(entity);
        }


        public void RemoveChunkLoader(WNetEntity loader) {
            presentLoaders.Remove(loader);

            if (presentLoaders.Count == 0) {
                WNetChunkManager.chunksMarkedToUnload.Add(Coords);
            }  
        }
    }
}
