using System;
using System.Collections.Generic;
using UnityEngine;
using Alchemy;
using Alchemy.Serialization;

namespace Inventories {
    [CreateAssetMenu(fileName = "EntitySO", menuName = "Scriptable Objects/InventoryTemplateSO")]
    [AlchemySerialize]
    public partial class InventoryTemplateSO : ScriptableObject {
        [field: SerializeField] public InventoryTemplateType TemplateType { get; private set; }
        [field: SerializeField] public int SlotsCount { get; private set; }

        [SerializeField] private bool acceptsNewItems;
        
        [AlchemySerializeField, NonSerialized] private Dictionary<int, HashSet<ItemCategory>> slotsAllowed = new();
        private Dictionary<int, int> slotsAllowedFlags = new();

        [AlchemySerializeField, NonSerialized] private HashSet<ItemCategory> inventoryAllowed = new();
        private int inventoryAllowedFlags;


        ///<summary> Converts all item classifications into flags. </summary>
        private void OnEnable() {
            foreach(var kvp in slotsAllowed)
                slotsAllowedFlags[kvp.Key] = GenerateItemCategoryFlags(kvp.Value);
            
            // Generate inventoryAllowedFlags if inventoryAllowed was defined in inspector; otherwise, allow all items
            inventoryAllowedFlags = inventoryAllowed.Count > 0 ? GenerateItemCategoryFlags(inventoryAllowed) : int.MaxValue;
        }


        ///<summary> Returns whether an item classifications can be accepted into an inventory index. </summary>
        public bool AllowsItemAtIndex(int inventoryIndex, int itemClassificationBitflags) {
            if(!acceptsNewItems)
                return false;

            // If restrictions exist on this slot, return whether slots are compatible
            if(!slotsAllowedFlags.TryGetValue(inventoryIndex, out var restrictionFlags)) {
                return true;
            }

            // Return true if there are not restrictions on this slot
            return (restrictionFlags & itemClassificationBitflags & inventoryAllowedFlags) != 0;
        }


        ///<summary> Converts an ItemCategory to bitflags </summary>
        public static int GenerateItemCategoryFlags(IEnumerable<ItemCategory> categories) {
            int ret = 0;
            foreach(var category in categories) {
                ret |= (int)category;
            }
            return ret;
        }
    }
}