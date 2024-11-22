
using System.Collections.Generic;
using UnityEngine;

using Networking.Shared;
using UnityEditor;
using System.Data.Common;

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


        public static void UpdateEntityTransform(WSEntityTransformUpdatePkt transformPacket) {
            if(!entities.TryGetValue(transformPacket.entityId, out WCEntity entity))
                return;

            if(transformPacket.transform.position != null)
                entity.transform.position = (Vector3)transformPacket.transform.position;
            
            if(transformPacket.transform.rotation != null)
                entity.transform.rotation = (Quaternion)transformPacket.transform.rotation;

            if(transformPacket.transform.scale != null)
                entity.transform.localScale = (Vector3)transformPacket.transform.scale;
        }
    }
}
