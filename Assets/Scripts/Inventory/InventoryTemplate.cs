using System.Collections.Generic;

namespace Inventories {
    public class InventoryTemplate {
        public readonly InventoryTemplateType templateType;
        public readonly int slotsCount;
        public readonly bool acceptsNewItems;
        private readonly Dictionary<int, int> inventorySlotRestrictionFlags;
        
        /// <param name="inventorySlotRestrictionFlagsPair"> Pairs of (inventory index, restriction flags) </param>
        public InventoryTemplate(InventoryTemplateType templateType, int slotsCount, bool acceptsNewItems, params (int, int)[] inventorySlotRestrictionFlagsPairs) {
            this.templateType = templateType;
            this.slotsCount = slotsCount;
            this.acceptsNewItems = acceptsNewItems;
            foreach(var inventorySlotRestrictionFlagsPair in inventorySlotRestrictionFlagsPairs) {
                inventorySlotRestrictionFlags[inventorySlotRestrictionFlagsPair.Item1] = inventorySlotRestrictionFlagsPair.Item2;
            }
        }

        ///<summary> Returns whether an item classifications can be accepted into an inventory index. </summary>
        public bool AllowsItemAtIndex(int inventoryIndex, int itemClassificationBitflags) {
            if(!acceptsNewItems)
                return false;

            // If restrictions exist on this slot, return whether slots are compatible
            if(inventorySlotRestrictionFlags.TryGetValue(inventoryIndex, out var restrictionFlags)) {
                // If item classification and restriction bitflags ANDed with one another aren't 0, there's some overlap, so return true
                return (restrictionFlags & itemClassificationBitflags) != 0;
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