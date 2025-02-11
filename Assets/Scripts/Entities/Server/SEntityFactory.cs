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

            entity.updatePositionOverNetwork = entitySO.UpdatePositionOverNetwork;
            entity.updateRotationOverNetwork = entitySO.UpdateRotationOverNetwork;
            entity.updateScaleOverNetwork = entitySO.UpdateScaleOverNetwork;

            if(entity.GetComponent<Rigidbody>()) {
                entity.setVisualPositionAutomatically = false;
                entity.setVisualRotationAutomatically = false;
                entity.isRigidbody = true;
            }

            if(entitySO.InventoryTemplate != null) {
                AddModifiedSInventory.CreateNewInventoryForEntity(entity, entitySO.InventoryTemplate);
            }

            return entity;
        }
    }
}