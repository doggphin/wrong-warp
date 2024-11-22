using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using Networking.Shared;

namespace Networking.Server {
    public static class WSEntityManager {
        private static Dictionary<int, WSEntity> entities = new();
        private static int nextEntityId = -1;

        public static GameObject SpawnHolder {get; set; }

        public static WSEntity SpawnEntity(WPrefabId prefabId, bool updatePosition, bool updateRotation, bool updateScale, bool isChunkLoader = false) {
            GameObject gameObject = Object.Instantiate(WPrefabLookup.GetById(prefabId), SpawnHolder.transform);

            var netEntity = gameObject.AddComponent<WSEntity>();

            while (!entities.TryAdd(++nextEntityId, netEntity));

            netEntity.gameObject.name = $"{nextEntityId:0000000000}_{prefabId}";
            netEntity.updatePosition = updatePosition;
            netEntity.updateRotation = updateRotation;
            netEntity.updateScale = updateScale;

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


        public static void AdvanceTick(int tick) {
            // TODO: This could be done more efficiently
            foreach (WSEntity netEntity in entities.Values.ToList()) {
                netEntity.Poll(tick);
            }
        }
    }
}