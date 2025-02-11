using System.Collections.Generic;
using UnityEngine;

namespace Networking.Shared {
    public class EntityPrefabLookup : BaseLookup<EntityPrefabId, EntitySO> {
        protected override string ResourcesPath { get => "Entities"; }
    }
}
