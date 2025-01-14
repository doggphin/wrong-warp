using UnityEngine;

namespace Inventory {
    public class ItemLookup : BaseLookup<ItemType, BaseItemSO> {
        protected override string ResourcesPath { get => "Items"; }
    }
}