using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using Networking.Shared;

namespace Networking.Server {
    public static class WSEntityManager {
        private static Dictionary<int, WSEntity> entities = new();
        private static int nextEntityId = -1;

        public static Transform spawnHolder;

        public static WSEntity SpawnEntity(WPrefabId prefabId, int tick, bool isChunkLoader = false) {
            GameObject gameObject = Object.Instantiate(WPrefabLookup.GetById(prefabId), spawnHolder);
            
            if(gameObject == null)
                throw new System.Exception("Couldn't instantiate prefab!!!!");

            var netEntity = gameObject.AddComponent<WSEntity>();

            
            WPrefabTransformUpdateTypes transformUpdateTypes = WPrefabLookup.PrefabUpdateTypes[prefabId];
            netEntity.updatePosition = transformUpdateTypes.updatePosition;
            netEntity.updateRotation = transformUpdateTypes.updateRotation;
            netEntity.updateScale = transformUpdateTypes.updateScale;

            while (!entities.TryAdd(++nextEntityId, netEntity));

            netEntity.gameObject.name = $"{nextEntityId:0000000000}_{prefabId}";

            netEntity.Init(nextEntityId, prefabId, isChunkLoader);

            netEntity.CurrentChunk.PresentEntities.Add(netEntity);
            
            return netEntity;
        }


        public static void KillEntity(int id, WEntityKillReason killReason = WEntityKillReason.Unload) {
            Debug.Log($"Killing {id}!");

            if (!entities.TryGetValue(id, out WSEntity netEntity)) {
                Debug.LogError("Tried to delete an entity that did not exist!");
                return;
            }

            netEntity.Kill(killReason);
            entities.Remove(id);
        }


        public static void PollFinalizeAdvanceEntities() {
            foreach (WSEntity netEntity in entities.Values.ToList()) {
                netEntity.PollAndFinalizeTransform();
            }
        }
    }
}