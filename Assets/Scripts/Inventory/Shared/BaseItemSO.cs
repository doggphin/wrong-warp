using System.Collections.Generic;
using UnityEngine;

namespace Inventories {
    [CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/ItemSO")]
    public class BaseItemSO : ScriptableObject
    {
        [SerializeField] private ItemClassification[] serializedItemClassifications;
        public ItemClassification[] ItemClassificationsArray => serializedItemClassifications;
        public int ItemClassificationBitflags { get; private set; }

        [SerializeField] private string itemName;
        public string ItemName => itemName;

        [TextArea(3, 3)][SerializeField] private string description;
        public string Description => description;

        [SerializeField] private Sprite slotSprite;
        public Sprite SlotSprite => slotSprite;

        [SerializeField] private int maxStackSize = 1;
        public int MaxStackSize => maxStackSize;

        void OnEnable() {
            ItemClassificationBitflags = InventoryTemplateSO.GenerateItemRestrictionFlags(ItemClassificationsArray);
        }
    }
}
