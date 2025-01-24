using System.Collections.Generic;
using Networking.Shared;
using Unity.VisualScripting;
using UnityEngine;

namespace Networking.Server {
    public class SEntityFactory : BaseEntityFactory<SEntity>
    {
        protected override SEntity OverrideableGenerateEntity(EntityPrefabId identifier)
        {
            SEntity entity = GenerateBaseEntity(identifier, out EntitySO entitySO);

            entity.updatePositionOverNetwork = entitySO.updatePositionOverNetwork;
            entity.updateRotationOverNetwork = entitySO.updateRotationOverNetwork;
            entity.updateScaleOverNetwork = entitySO.updateScaleOverNetwork;

            if(entity.GetComponent<Rigidbody>()) {
                entity.setVisualPositionAutomatically = false;
                entity.setVisualRotationAutomatically = false;
                entity.isRigidbody = true;
            }
            
            /*switch(entitySO.autoMovementType) {
                case AutomaticMovementType.Velocity:
                    entity.AddComponent<EntityVelocity>();
                    break;
                case AutomaticMovementType.Rigidbody:
                    entity.AddComponent<Rigidbody>();
                    entity.setVisualPositionAutomatically = false;
                    entity.setVisualRotationAutomatically = false;
                    entity.isRigidbody = true;
                    break;
                default:
                    break;
            }*/

            return entity;
        }
    }
}