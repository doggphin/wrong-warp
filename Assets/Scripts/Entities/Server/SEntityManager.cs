using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using Networking.Shared;
using System;
using Unity.Burst.Intrinsics;

namespace Networking.Server {
    [RequireComponent(typeof(SEntityFactory))]
    public class SEntityManager : BaseSingleton<SEntityManager> {
        private Dictionary<int, SEntity> entities = new();
        private static BaseIdGenerator idGenerator = new();

        //public static Action<WSEntity, WEntitySpawnReason> EntitySpawned;
        //public static Action<WSEntity, WEntityKillReason> EntityKilled;

        public static SEntity SpawnEntity(EntityPrefabId entityIdentifier, bool isChunkLoader = false) {
            int entityId = idGenerator.GetNextEntityId(Instance.entities);
            var entity = SEntityFactory.GenerateEntity(entityIdentifier, entityId);
            Instance.entities.Add(entityId, entity);
            entity.transform.parent = Instance.transform;

            entity.Init(entityId, entityIdentifier, isChunkLoader);
        
            entity.CurrentChunk.SpawnEntity(entity, WEntitySpawnReason.Spawn);
            
            entity.Killed += KillEntity;
            return entity;
        }


        public static void KillEntity(SEntity entity, WEntityKillReason killReason = WEntityKillReason.Unload) {
            if(entity == null)
                return;
            
            entity.CurrentChunk.KillEntity(entity, killReason);
            entity.Killed -= KillEntity;
            Instance.entities.Remove(entity.Id);
        }


        public static void PollFinalizeAdvanceEntities() {
            Physics.Simulate(NetCommon.SECONDS_PER_TICK);
            foreach (SEntity netEntity in Instance.entities.Values.ToList()) {
                netEntity.PollAndFinalizeTransform();
            }
        }
    }
}