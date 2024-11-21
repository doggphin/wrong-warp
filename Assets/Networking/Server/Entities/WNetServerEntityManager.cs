using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using Networking.Shared;

namespace Networking.Server {
    public static class WNetServerEntityManager {
        private static Dictionary<int, WNetServerEntity> entities = new();
        private static int nextEntityId = -1;

        public static GameObject SpawnHolder {get; set; }

        public static WNetServerEntity SpawnEntity(WNetPrefabId prefabId, bool isChunkLoader = false) {
            GameObject gameObject = Object.Instantiate(WNetPrefabLookup.GetById(prefabId), SpawnHolder.transform);

            var netEntity = gameObject.AddComponent<WNetServerEntity>();

            while (!entities.TryAdd(++nextEntityId, netEntity));

            netEntity.gameObject.name = $"{nextEntityId:0000000000}_{prefabId}";
            Debug.Log("Spawned a dude");

            if(WNetManager.IsServer)
                netEntity.InitServer(nextEntityId, isChunkLoader);

            return netEntity;
        }


        public static void KillEntity(int id, WEntityKillReason killReason = WEntityKillReason.Unload) {
            Debug.Log($"Killing {id}!");

            if (!entities.TryGetValue(id, out WNetServerEntity netEntity)) {
                Debug.LogError("Tried to delete an entity that did not exist!");
                return;
            }

            netEntity.Kill(killReason);
            entities.Remove(id);
        }


        public static void AdvanceTick(int tick) {
            // TODO: This could be done more efficiently
            foreach (WNetServerEntity netEntity in entities.Values.ToList()) {
                netEntity.Poll(tick);
            }
        }
    }
}