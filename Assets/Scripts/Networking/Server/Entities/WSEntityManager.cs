using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using Networking.Shared;

namespace Networking.Server {
    public class WSEntityManager : BaseSingleton<WSEntityManager> {
        private Dictionary<int, WSEntity> entities = new();
        private static int nextEntityId = -1;

        public static WSEntity SpawnEntity(WPrefabId prefabId, bool isChunkLoader = false) {
            GameObject entityPrefab = Instantiate(WPrefabLookup.GetById(prefabId), Instance.transform);
            var netEntity = entityPrefab.AddComponent<WSEntity>();

            
            WPrefabTransformUpdateTypes transformUpdateTypes = WPrefabLookup.PrefabUpdateTypes[prefabId];
            netEntity.updatePosition = transformUpdateTypes.updatePosition;
            netEntity.updateRotation = transformUpdateTypes.updateRotation;
            netEntity.updateScale = transformUpdateTypes.updateScale;

            while (!Instance.entities.TryAdd(++nextEntityId, netEntity));

            netEntity.gameObject.name = $"{nextEntityId:0000000000}_{prefabId}";

            netEntity.Init(nextEntityId, prefabId, isChunkLoader);

            netEntity.CurrentChunk.PresentEntities.Add(netEntity);
            
            return netEntity;
        }


        public static void KillEntity(int id, WEntityKillReason killReason = WEntityKillReason.Unload) {
            Debug.Log($"Killing {id}!");

            if (!Instance.entities.TryGetValue(id, out WSEntity netEntity)) {
                Debug.LogError("Tried to delete an entity that did not exist!");
                return;
            }

            netEntity.Kill(killReason);
            Instance.entities.Remove(id);
        }


        public static void PollFinalizeAdvanceEntities() {
            foreach (WSEntity netEntity in Instance.entities.Values.ToList()) {
                netEntity.PollAndFinalizeTransform();
            }
        }
    }
}