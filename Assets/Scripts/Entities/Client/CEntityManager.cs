
using System.Collections.Generic;
using UnityEngine;

using Networking.Shared;

namespace Networking.Client {
    [RequireComponent(typeof(CEntityFactory))]
    public class CEntityManager : BaseSingleton<CEntityManager> {
        private Dictionary<int, CEntity> entities = new();
        public static CEntity GetEntityById(int id) => Instance.entities.GetValueOrDefault(id, null);

        protected override void Awake() {
            SPacket<SEntitySpawnPkt>.ApplyUnticked += HandleSpawnEntity;
            SPacket<SEntityKillPkt>.ApplyUnticked += KillEntity;
            SPacket<SFullEntitiesSnapshotPkt>.ApplyUnticked += HandleFullEntitiesSnapshot;
            SPacket<SEntityTransformUpdatePkt>.Apply += SetEntityTransformForTick;
            base.Awake();
        }

        protected override void OnDestroy() {
            SPacket<SEntitySpawnPkt>.ApplyUnticked -= HandleSpawnEntity;
            SPacket<SEntityKillPkt>.ApplyUnticked -= KillEntity;
            SPacket<SFullEntitiesSnapshotPkt>.ApplyUnticked -= HandleFullEntitiesSnapshot;
            SPacket<SEntityTransformUpdatePkt>.Apply -= SetEntityTransformForTick;
            base.OnDestroy();
        }


        private void HandleSpawnEntity(SEntitySpawnPkt pkt) => SpawnEntity(pkt);
        private CEntity SpawnEntity(SEntitySpawnPkt spawnPacket) {
            if(entities.ContainsKey(spawnPacket.entity.entityId)) {
                Debug.Log($"Entity with ID {spawnPacket.entity.entityId} already exists");
                return null;
            }

            Debug.Log("Spawning entity!");
            CEntity entity = CEntityFactory.GenerateEntity(spawnPacket.entity.entityPrefabId, spawnPacket.entity.entityId);
            entity.transform.parent = transform;

            TransformSerializable serializedTransform = spawnPacket.entity.transform;
            entity.transform.SetPositionAndRotation(serializedTransform.position.Value, serializedTransform.rotation.Value);
            entity.transform.localScale = serializedTransform.scale.Value;

            entities[spawnPacket.entity.entityId] = entity;
            
            entity.Init(spawnPacket);

            return entity;
        }


        private void KillEntity(SEntityKillPkt killPacket) {
            if (!Instance.entities.TryGetValue(killPacket.entityId, out CEntity entity)) {
                Debug.LogWarning("Tried to delete an entity that did not exist!");
                return;
            }

            entity.StartDeath(killPacket.reason);
            Instance.entities.Remove(killPacket.entityId);
        }

        
        private void SetEntityTransformForTick(int tick, SEntityTransformUpdatePkt transformPacket) {
            if(!Instance.entities.TryGetValue(transformPacket.CEntityId, out CEntity entity))
                return;

            entity.SetTransformForTick(tick, transformPacket.transform);
        }


        private void HandleFullEntitiesSnapshot(SFullEntitiesSnapshotPkt pkt) {
            Dictionary<int, WEntitySerializable> receivedEntities = new();
            foreach(var serializedEntity in pkt.entities) {
                receivedEntities.Add(serializedEntity.entityId, serializedEntity);
            }

            // Find entities that might exist on client but not in received
            // Delete these first to not iterate over new entities
            foreach(var clientEntityId in Instance.entities.Keys) {
                if(!receivedEntities.ContainsKey(clientEntityId)) {
                    Debug.Log("Killing an entity that exists on the client but not the server!");
                    KillEntity(new SEntityKillPkt() { entityId = clientEntityId, reason = WEntityKillReason.Unload });
                }
            }

            foreach(var receivedEntity in receivedEntities) {
                if(!Instance.entities.ContainsKey(receivedEntity.Key)) {
                    // Does not exist on client; must create new entity for it
                    Debug.Log("Spawning an entity that exists on the server but not the client!");
                    SpawnEntity(new SEntitySpawnPkt() { entity = receivedEntity.Value, reason = WEntitySpawnReason.Load });
                }
            }
        }
    }
}
