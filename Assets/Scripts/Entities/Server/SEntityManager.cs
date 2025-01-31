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

        public static Action<SEntity> EntityDeleted;

        protected override void Awake()
        {
            SChunk.UnloadEntity += DeleteEntity;
            base.Awake();
        }

        protected override void OnDestroy()
        {
            SChunk.UnloadEntity -= DeleteEntity;
            base.OnDestroy();
        }

        
        public SEntity SpawnEntity(EntityPrefabId entityIdentifier, Vector3? position, Quaternion? rotation = null, Vector3? scale = null, SPlayer player = null) {
            int entityId = idGenerator.GetNextEntityId(Instance.entities);
            var entity = SEntityFactory.GenerateEntity(entityIdentifier, entityId);

            entity.transform.parent = transform;
            entity.Init(
                entityId,
                entityIdentifier,
                position.GetValueOrDefault(Vector3.zero),
                rotation.GetValueOrDefault(Quaternion.identity),
                scale.GetValueOrDefault(Vector3.one)
            );

            // Don't allow spawning a non-player entity into an unloaded chunk
            Vector2Int entityChunkCoordinates = SChunkManager.ProjectToGrid(position.GetValueOrDefault(Vector3.zero));
            if(!SChunkManager.Instance.AddEntityAndOrPlayerToSystem(entity, entityChunkCoordinates, player)) {
                Debug.Log("Couldn't add entity to the system!");
                Destroy(entity.gameObject);
                return null;
            }

            entity.FinishedDying += DeleteEntity;

            entities.Add(entityId, entity);
            return entity;
        }


        private void DeleteEntity(SEntity entity) {
            if(entity == null)
                return;

            entity.FinishedDying -= DeleteEntity;

            EntityDeleted?.Invoke(entity);

            entities.Remove(entity.Id);
            Destroy(entity.gameObject);
        }


        public void AdvanceEntities() {
            Physics.Simulate(NetCommon.SECONDS_PER_TICK);
            foreach (SEntity netEntity in Instance.entities.Values.ToList()) {
                netEntity.PollAndFinalizeTransform();
            }
        }


        public void UpdateChunksOfEntities() {
            foreach(SEntity entity in SChunkManager.Instance.UpdateEntityChunkLocations(entities.Values.ToList(), SNetManager.Tick)) {
                DeleteEntity(entity);
            }
        }
    }
}