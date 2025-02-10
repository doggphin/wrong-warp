using System;
using System.Collections.Generic;
using Alchemy.Serialization;
using UnityEngine;

namespace Inventories {
    [CreateAssetMenu(fileName = "EntitySO", menuName = "Scriptable Objects/InventoryTemplateSO")]
    [AlchemySerialize]
    public partial class InventoryTemplateSO : ScriptableObject {
        public InventoryTemplateType templateType;

        public int slotsCount;
        public bool acceptsNewItems;
        
        [AlchemySerializeField, NonSerialized]
        private Dictionary<int, HashSet<InventoryTemplateType>> slotRestrictions = new();
        public HashSet<InventoryTemplateType> inventoryRestrictions;

        private Dictionary<int, int> slotRestrictionFlags = new();
        private int inventoryRestrictionFlags = 0;  // TODO: test

        ///<summary> Returns whether an item classifications can be accepted into an inventory index. </summary>
        public bool AllowsItemAtIndex(int inventoryIndex, int itemClassificationBitflags) {
            if(!acceptsNewItems)
                return false;

            // If restrictions exist on this slot, return whether slots are compatible
            if(slotRestrictionFlags.TryGetValue(inventoryIndex, out var restrictionFlags)) {
                // If item classification and restriction bitflags ANDed with one another aren't 0, there's some overlap, so return true
                return (restrictionFlags & itemClassificationBitflags & inventoryRestrictionFlags) != 0;
            }
            // Return true if there are not restrictions on this slot
            return true;
        }

        ///<summary> Converts an ItemClassification[] to bitflags </summary>
        public static int GenerateItemRestrictionFlags(params ItemClassification[] classifications) {
            int ret = 0;
            foreach(var classification in classifications) {
                ret |= (int)classification;
            }
            return ret;
        }
    }
}