
using System.Collections.Generic;
using UnityEngine;

using Networking.Shared;

namespace Networking.Client {
    public class WCEntityManager : BaseSingleton<WCEntityManager> {
        private static Dictionary<int, WCEntity> entities = new();

        public static void KillEntity(WSEntityKillPkt killPacket) {
            if (!entities.TryGetValue(killPacket.entityId, out WCEntity entity)) {
                Debug.LogWarning("Tried to delete an entity that did not exist!");
                return;
            }

            entity.Kill(killPacket.reason);
            entities.Remove(killPacket.entityId);
        }


        public static WCEntity Spawn(WSEntitySpawnPkt spawnPacket) {
            if(entities.ContainsKey(spawnPacket.entity.entityId))
                return null;

            GameObject prefabToSpawn = WPrefabLookup.GetById(spawnPacket.entity.prefabId);

            var gameObject = Instantiate(prefabToSpawn, Instance.transform);
            var ret = gameObject.AddComponent<WCEntity>();

            entities[spawnPacket.entity.entityId] = ret;

            ret.Init(spawnPacket);

            return ret;
        }


        public static WCEntity GetEntityById(int id) => entities.GetValueOrDefault(id, null);

        
        public static void SetEntityTransformForTick(int tick, WSEntityTransformUpdatePkt transformPacket) {
            if(!entities.TryGetValue(transformPacket.entityId, out WCEntity entity))
                return;

            entity.SetTransformForTick(tick, transformPacket.transform);
        }
    }
}
