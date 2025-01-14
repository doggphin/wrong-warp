using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace Inventory {
    public class SlottedItem {
        public ItemType itemType;
        public int stackSize;

        public BaseItemSO GetBaseItem() => ItemLookup.GetById(itemType);

        // Tries to merge otherSlottedItem into this slottedItem.
        // Returns whether otherSlottedItem was modified.
        public bool TryMerge(SlottedItem otherSlottedItem) {
            if(itemType != otherSlottedItem.itemType)
                return false;

            BaseItemSO baseItem = GetBaseItem();
            if(baseItem.maxStackSize <= stackSize)
                return false;

            int amountToMerge = Mathf.Min(baseItem.maxStackSize - stackSize, stackSize + otherSlottedItem.stackSize);
            otherSlottedItem.stackSize -= amountToMerge;
            stackSize += amountToMerge;
            
            return true;
        }
    }
}
