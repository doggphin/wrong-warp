using UnityEngine;

namespace Inventories {
    public class ItemLookup : BaseLookup<ItemType, BaseItemSO> {
        protected override string ResourcesPath { get => "Items"; }
    }
}