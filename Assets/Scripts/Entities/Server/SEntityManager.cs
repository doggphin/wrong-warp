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

        public static SEntity SpawnEntity(EntityPrefabId entityIdentifier, Vector3? position, Quaternion? rotation = null, Vector3? scale = null, SPlayer player = null) {
            int entityId = idGenerator.GetNextEntityId(Instance.entities);
            var entity = SEntityFactory.GenerateEntity(entityIdentifier, entityId);

            entity.transform.parent = Instance.transform;
            entity.Init(
                entityId,
                entityIdentifier,
                player,
                position.GetValueOrDefault(Vector3.zero),
                rotation.GetValueOrDefault(Quaternion.identity),
                scale.GetValueOrDefault(Vector3.one)
            );

            // Don't allow spawning a non-player entity into an unloaded chunk
            Vector2Int entityChunkCoordinates = NewSChunkManager.ProjectToGrid(position.GetValueOrDefault(Vector3.zero));
            if(!NewSChunkManager.AddEntityToSystem(entity, entityChunkCoordinates)) {
                Debug.Log("Couldn't add entity to the system!");
                Destroy(entity.gameObject);
                return null;
            }

            Debug.Log("Added the entity to the system!");

            entity.FinishedDying += DeleteEntity;

            Instance.entities.Add(entityId, entity);
            Debug.Log($"Successfully created {entity}, with ID {entityId}!");
            return entity;
        }


        public static void UpdateEntityChunks() {
            foreach(SEntity entity in Instance.entities.Values) {
                Vector2Int newChunkCoords = NewSChunkManager.ProjectToGrid(entity.positionsBuffer[SNetManager.Tick]);
                if (newChunkCoords == entity.Chunk.Coords)
                    continue;
                
                NewSChunkManager.MoveEntity(entity, newChunkCoords);
            }
        }


        public static void DeleteEntity(SEntity entity) {
            if(entity == null)
                return;

            entity.FinishedDying -= DeleteEntity;
            NewSChunkManager.RemoveEntityFromSystem(entity);
            Instance.entities.Remove(entity.Id);
            Destroy(entity.gameObject);
        }


        public static void PollFinalizeAdvanceEntities() {
            Physics.Simulate(NetCommon.SECONDS_PER_TICK);
            foreach (SEntity netEntity in Instance.entities.Values.ToList()) {
                netEntity.PollAndFinalizeTransform();
            }
        }
    }
}