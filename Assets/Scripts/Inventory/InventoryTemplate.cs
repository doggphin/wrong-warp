using System.Collections.Generic;

namespace Inventories {
    public class InventoryTemplate {
        public readonly int slotsCount;
        public readonly bool acceptsNewItems;
        private readonly Dictionary<int, int> inventorySlotRestrictionFlags;
        
        /// <param name="inventorySlotRestrictionFlagsPair"> Pairs of (inventory index, restriction flags) </param>
        public InventoryTemplate(int slotsCount, bool acceptsNewItems, params (int, int)[] inventorySlotRestrictionFlagsPairs) {
            this.slotsCount = slotsCount;
            this.acceptsNewItems = acceptsNewItems;
            foreach(var inventorySlotRestrictionFlagsPair in inventorySlotRestrictionFlagsPairs) {
                inventorySlotRestrictionFlags[inventorySlotRestrictionFlagsPair.Item1] = inventorySlotRestrictionFlagsPair.Item2;
            }
        }

        ///<summary> Returns whether an item classifications can be accepted into an inventory index. </summary>
        public bool AllowsItemAtIndex(int inventoryIndex, ItemClassification itemClassification) {
            if(!acceptsNewItems)
                return false;

            // If restrictions exist on this slot,
            if(inventorySlotRestrictionFlags.TryGetValue(inventoryIndex, out var restrictionFlags)) {
                return (restrictionFlags & (1 << (int)itemClassification)) != 0;
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