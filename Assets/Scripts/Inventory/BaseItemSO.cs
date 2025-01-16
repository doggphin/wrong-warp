using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

namespace Inventories {
    [CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/ItemSO")]
    public class BaseItemSO : ScriptableObject
    {
        public enum BaseItemClassification {
            Helmet,
            Chestplate,
            Boots,
            Gun,
            Sword,
            Consumable
        }
        [SerializeField] private BaseItemClassification[] serializedItemClassifications;
        public HashSet<BaseItemClassification> ItemClassifications { get; private set; }
        public readonly string itemName;
        public readonly Sprite slotSprite;
        public readonly int maxStackSize;

        void OnEnable() {
            ItemClassifications = new(serializedItemClassifications);
        }
    }
}
