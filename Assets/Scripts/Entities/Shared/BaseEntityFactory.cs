
using System.Collections.Generic;
using Networking.Shared;
using Unity.VisualScripting;
using UnityEngine;

namespace Networking.Shared {
    public abstract class BaseEntityFactory<T> : BaseSingleton<BaseEntityFactory<T>> where T : BaseEntity {
        //protected T GenerateBaseEntity
        protected T GenerateBaseEntity(EntityPrefabId id, out EntitySO entitySO) {
            entitySO = EntityPrefabLookup.Lookup(id);
            return Instantiate(entitySO.EntityPrefab).AddComponent<T>();
        }

        protected virtual T OverrideableGenerateEntity(EntityPrefabId identifier) {
            return GenerateBaseEntity(identifier, out _);
        }

        public static T GenerateEntity(EntityPrefabId identifier, int id) {
            T ret = Instance.OverrideableGenerateEntity(identifier);
            ret.name = $"{id:D11}_{identifier}";
            return ret;
        }
    }
}