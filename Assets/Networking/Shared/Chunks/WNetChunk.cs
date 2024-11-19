using LiteNetLib.Utils;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Serialization;
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

        public NetDataWriter writer { get; private set; } = new();

        private bool isSerialized;
        public void SerializeAllUpdates() {
            if (isSerialized)
                return;

            WSWorldUpdatePkt chunkUpdate = new() { generalUpdates = generalUpdates, entityUpdates = entityUpdates };



            isSerialized = true;
        }

        public void Load(Vector2Int coords) {
            isLoaded = true;
            generalUpdates = new List<INetSerializable>[WNetCommon.TICKS_PER_UPDATE];
            entityUpdates = new Dictionary<int, List<INetSerializable>>[WNetCommon.TICKS_PER_UPDATE];

            for (int i = 0; i < WNetCommon.TICKS_PER_UPDATE; i++) {
                generalUpdates[i] = new();
                entityUpdates[i] = new();
            }

            Coords = coords;
        }


        public void ResetUpdates() {
            for(int i=0; i < WNetCommon.TICKS_PER_UPDATE; i++) {
                generalUpdates[i].Clear();
                entityUpdates[i].Clear();
            }
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

            if (!entityUpdates[tick % WNetCommon.TICKS_PER_UPDATE].TryGetValue(id, out var entityUpdatesList)) {
                List<INetSerializable> newEntityUpdatesList = new();
                entityUpdates[tick % WNetCommon.TICKS_PER_UPDATE].Add(id, newEntityUpdatesList);
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
