
using System.Collections.Generic;
using UnityEngine;

using Networking.Shared;
using UnityEditor;
using System.Data.Common;

namespace Networking.Client {
    public static class WNetClientEntityManager {
        private static Dictionary<int, WNetClientEntity> entities = new();

        public static GameObject SpawnHolder {get; set; }

        public static void KillEntity(int id, WEntityKillReason killReason) {
            if (!entities.TryGetValue(id, out WNetClientEntity netEntity)) {
                Debug.LogError("Tried to delete an entity that did not exist!");
                return;
            }

            netEntity.Kill(killReason);
            entities.Remove(id);
        }


        public static WNetClientEntity Spawn(WSEntitySpawnPkt spawnPacket) {
            if(entities.ContainsKey(spawnPacket.entity.entityId)) {
                Debug.LogError("Tried to add an entity with an ID that already exists!");
                return null;
            }

            var gameObject = Object.Instantiate(WNetPrefabLookup.GetById(spawnPacket.entity.prefabId), SpawnHolder.transform);
            var ret = gameObject.AddComponent<WNetClientEntity>();
            entities[spawnPacket.entity.entityId] = ret;

            ret.Init(spawnPacket);

            return ret;
        }
    }
}
