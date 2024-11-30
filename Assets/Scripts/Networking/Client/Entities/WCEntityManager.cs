
using System.Collections.Generic;
using UnityEngine;
using Networking.Client;

using Networking.Shared;
using Networking.Server;

namespace Networking.Client {
    public static class WCEntityManager {
        private static Dictionary<int, WCEntity> entities = new();
        public static GameObject SpawnHolder {get; set; }

        public static void KillEntity(WSEntityKillPkt killPacket) {
            if (!entities.TryGetValue(killPacket.entityId, out WCEntity entity)) {
                Debug.LogWarning("Tried to delete an entity that did not exist!");
                return;
            }

            entity.Kill(killPacket.reason);
            entities.Remove(killPacket.entityId);
        }


        public static void ReadyForNextTick() {
            foreach (WCEntity entity in entities.Values) {
                entity.ApplyPreviousTransform();
            }
        }


        public static WCEntity Spawn(WSEntitySpawnPkt spawnPacket) {
            if(entities.ContainsKey(spawnPacket.entity.entityId)) {
                return null;
            }

            GameObject prefabToSpawn = WPrefabLookup.GetById(spawnPacket.entity.prefabId);

            var gameObject = Object.Instantiate(prefabToSpawn, SpawnHolder.transform);
            var ret = gameObject.AddComponent<WCEntity>();

            entities[spawnPacket.entity.entityId] = ret;

            ret.Init(spawnPacket);

            return ret;
        }


        public static WCEntity GetEntityById(int id) {
            return entities.GetValueOrDefault(id, null);
        }

        
        public static void UpdateEntityTransform(WSEntityTransformUpdatePkt transformPacket) {
            if(!entities.TryGetValue(transformPacket.entityId, out WCEntity entity))
                return;

            entity.UpdateTransform(transformPacket.transform);
        }
    }
}
