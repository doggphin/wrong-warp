using UnityEditor;
using UnityEngine;

namespace Inventory {
    [CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/ItemSO")]
    public class BaseItemSO : ScriptableObject
    {
        public string itemName;
        public Sprite slotSprite;

        public int maxStackSize;
    }
}
