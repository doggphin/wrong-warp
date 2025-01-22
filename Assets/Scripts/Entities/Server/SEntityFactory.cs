using System.Collections.Generic;
using Networking.Shared;
using UnityEngine;

namespace Networking.Server {
    public class SEntityFactory : BaseEntityFactory<SEntity>
    {
        protected override SEntity OverrideableGenerateEntity(EntityPrefabId identifier)
        {
            SEntity entity = GenerateBaseEntity(identifier, out EntitySO entitySO);

            entity.updatePosition = entitySO.updatePosition;
            entity.updateRotation = entitySO.updateRotation;
            entity.updateScale = entitySO.updateScale;

            return entity;
        }
    }
}