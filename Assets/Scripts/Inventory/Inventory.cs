using UnityEngine;
using Networking.Shared;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Inventories { 
    public class Inventory {
        public readonly InventoryTemplate template;
        public SlottedItem[] SlottedItems { get; private set; }
        
        ///<summary> Generates an empty inventory from a template. </summary>
        public Inventory(int id, InventoryTemplate template) {
            this.template = template;
            SlottedItems = new SlottedItem[template.slotsCount];
        }

        /// <summary> Where int represents the slot index that was modified </summary>
        public Action<int> Modified;
        
        ///<summary> Tries to move an item from an index in this inventory to an index in another inventory. If toInventory is not specified, uses this inventory. </summary>
        public void MoveItem(int fromIndex, int toIndex, Inventory toInventory = null) { 
            // A null toInventory signifies moving within self
            toInventory ??= this;

            // Don't allow interactions outside the bounds of the inventories' items array
            if(fromIndex < 0 || fromIndex >= SlottedItems.Length || toIndex < 0 || toIndex >= toInventory.SlottedItems.Length)
                return;

            SlottedItem fromItem = SlottedItems[fromIndex];
            if(!toInventory.AllowsItemClassificationAtIndex(fromIndex, fromItem.BaseItemRef.ItemClassificationsArray))
                return;
            
            // If moving into an empty slot or a slot that contains an item that cannot be merged into,
            if(toInventory.SlottedItems[toIndex] == null && !toInventory.SlottedItems[toIndex].TryAbsorbSlottedItem(SlottedItems[fromIndex], SlottedItems[fromIndex].stackSize)) {
                // Swap the places of the items
                SlottedItem toItem = toInventory.SlottedItems[toIndex];
                toInventory.SlottedItems[toIndex] = SlottedItems[fromIndex];
                SlottedItems[toIndex] = toItem;
            }

            // Invoke actions to alert both inventories as having been modified
            Modified?.Invoke(fromIndex);
            toInventory.Modified?.Invoke(toIndex);
        }


        ///<returns> Whether the item was modified/consumed. </returns>
        public bool TryAddItem(SlottedItem itemToAdd) {
            int initialStackSize = itemToAdd.stackSize;

            // During first run, try to stack the item into each item in the inventory;
            // Also, keep track of the most recent null slot in case it can't be stacked into anything
            int? firstEmptyIndex = null;
            for(int i=0; i<SlottedItems.Length; i++) {
                SlottedItem slot = SlottedItems[i];
                // Save first empty slot for use later if necessary
                if(slot == null) {
                    firstEmptyIndex ??= i;
                    continue;
                }
                // Try to merge item into slots it can
                if(!slot.TryAbsorbSlottedItem(itemToAdd))
                    continue;
                // If fully, successfully merged, 
                if(itemToAdd.stackSize == 0)
                    return true;
            }
            // If there's still items left and an empty space was found, store rest of item into an empty space
            if(firstEmptyIndex == null)
                return itemToAdd.stackSize == initialStackSize;
            // Put item in empty slot
            SlottedItems[firstEmptyIndex.Value] = itemToAdd;
            return true;
        }

        
        public bool AllowsItemClassificationAtIndex(int inventoryIndex, params ItemClassification[] itemClassifications) {
            foreach(var itemClassification in itemClassifications) {
                if (!template.AllowsItemAtIndex(inventoryIndex, itemClassification)) {
                    return false;
                }
            }
            
            return true;
        }
    }
}