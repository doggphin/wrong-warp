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
            var entity = entityPrefab.AddComponent<WSEntity>();

            
            WPrefabTransformUpdateTypes transformUpdateTypes = WPrefabLookup.PrefabUpdateTypes[prefabId];
            entity.updatePosition = transformUpdateTypes.updatePosition;
            entity.updateRotation = transformUpdateTypes.updateRotation;
            entity.updateScale = transformUpdateTypes.updateScale;

            while (!Instance.entities.TryAdd(++nextEntityId, entity));

            entity.gameObject.name = $"{nextEntityId:0000000000}_{prefabId}";

            entity.Init(nextEntityId, prefabId, isChunkLoader);

            entity.CurrentChunk.SpawnEntity(entity, WEntitySpawnReason.Spawn);
            
            entity.Killed += KillEntity;
            return entity;
        }


        public static void KillEntity(WSEntity entity, WEntityKillReason killReason = WEntityKillReason.Unload) {
            if(entity == null)
                return;
            entity.CurrentChunk.KillEntity(entity, killReason);
            entity.Killed -= KillEntity;
            Instance.entities.Remove(entity.Id);
        }


        public static void PollFinalizeAdvanceEntities() {
            foreach (WSEntity netEntity in Instance.entities.Values.ToList()) {
                netEntity.PollAndFinalizeTransform();
            }
        }
    }
}