using System.Collections.Generic;
using UnityEngine;
using Alchemy.Serialization;
using System;

namespace Inventories {
    [CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/ItemSO")]
    [AlchemySerialize]
    public partial class BaseItemSO : ScriptableObject
    {
        [field: SerializeField] public string ItemName { get; private set; }

        [TextArea(3, 3)][field: SerializeField] public string Description { get; private set; }

        [field: SerializeField] public Sprite SlotSprite { get; private set; }

        [field: SerializeField] public int MaxStackSize { get; private set; } = 1;

        [AlchemySerializeField, NonSerialized] private HashSet<ItemCategory> categories = new();
        public int ItemClassificationBitflags { get; private set; }

        [field: SerializeField] public string AudioCollectionAddressable { get; private set; } = "Koth/Inventory/Slide";

        private void OnEnable() {
            ItemClassificationBitflags = 
                InventoryTemplateSO.GenerateItemCategoryFlags(categories);
        }
    }
}
