using Networking.Shared;
using Unity.VisualScripting;
using UnityEngine;

namespace Networking.Client {
    public class CEntityFactory : BaseEntityFactory<CEntity> {
        protected override CEntity OverrideableGenerateEntity(EntityPrefabId identifier)
        {
            CEntity entity = GenerateBaseEntity(identifier, out EntitySO entitySO);

            /*
            switch(entitySO.autoMovementType) {
                case AutomaticMovementType.Rigidbody:
                    entity.AddComponent<Rigidbody>();
                    break;
                default:
                    break;
            }
            */

            return entity;
        }
    }
}