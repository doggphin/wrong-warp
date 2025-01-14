
using System.Collections.Generic;
using UnityEngine;

using Networking.Shared;

namespace Networking.Client {
    public class WCEntityManager : BaseSingleton<WCEntityManager> {
        private Dictionary<int, WCEntity> entities = new();

        public static void KillEntity(WSEntityKillPkt killPacket) {
            if (!Instance.entities.TryGetValue(killPacket.entityId, out WCEntity entity)) {
                Debug.LogWarning("Tried to delete an entity that did not exist!");
                return;
            }

            entity.Kill(killPacket.reason);
            Instance.entities.Remove(killPacket.entityId);
        }


        public static WCEntity Spawn(WSEntitySpawnPkt spawnPacket) {
            if(Instance.entities.ContainsKey(spawnPacket.entity.entityId)) {
                Debug.Log($"Entity with ID {spawnPacket.entity.entityId} already exists");
                return null;
            }

            GameObject prefabToSpawn = NetPrefabLookup.Lookup(spawnPacket.entity.prefabId);

            var instantiatedPrefab = Instantiate(prefabToSpawn, Instance.transform);
            var entity = instantiatedPrefab.AddComponent<WCEntity>();
            WTransformSerializable transform = spawnPacket.entity.transform;
            
            instantiatedPrefab.transform.position = transform.position.Value;
            instantiatedPrefab.transform.rotation = transform.rotation.Value;
            instantiatedPrefab.transform.localScale = transform.scale.Value;
            

            Instance.entities[spawnPacket.entity.entityId] = entity;

            entity.Init(spawnPacket);

            return entity;
        }


        public static WCEntity GetEntityById(int id) => Instance.entities.GetValueOrDefault(id, null);

        
        public static void SetEntityTransformForTick(int tick, WSEntityTransformUpdatePkt transformPacket) {
            if(!Instance.entities.TryGetValue(transformPacket.entityId, out WCEntity entity))
                return;

            entity.SetTransformForTick(tick, transformPacket.transform);
        }


        public static void HandleFullEntitiesSnapshot(WSFullEntitiesSnapshotPkt pkt) {
            Dictionary<int, WEntitySerializable> receivedEntities = new();
            foreach(var serializedEntity in pkt.entities) {
                receivedEntities.Add(serializedEntity.entityId, serializedEntity);
            }

            // Find entities that might exist on client but not in received
            // Delete these first to not iterate over new entities
            foreach(var clientEntityId in Instance.entities.Keys) {
                if(!receivedEntities.ContainsKey(clientEntityId)) {
                    Debug.Log("Killing an entity that exists on the client but not the server!");
                    KillEntity(new WSEntityKillPkt() { entityId = clientEntityId, reason = WEntityKillReason.Unload });
                }
            }

            foreach(var receivedEntity in receivedEntities) {
                if(!Instance.entities.ContainsKey(receivedEntity.Key)) {
                    // Does not exist on client; must create new entity for it
                    Debug.Log("Spawning an entity that exists on the server but not the client!");
                    Spawn(new WSEntitySpawnPkt() { entity = receivedEntity.Value, reason = WEntitySpawnReason.Load });
                }
            }

            
        }
    }
}
