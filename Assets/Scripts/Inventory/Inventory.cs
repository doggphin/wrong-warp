using UnityEngine;
using Networking.Shared;
using System;
using System.Collections.Generic;

namespace Inventories {
    class Inventory {
        public SlottedItem[] items;
        /// <summary> Where int represents the slot index that was modified </summary>
        public Action<int> Modified;

        public void MoveItem(int fromIndex, int toIndex, Inventory toInventory = null) { 
            // A null toInventory signifies moving within self
            toInventory ??= this;

            // Don't allow interactions outside the bounds of the inventories' items array
            if(fromIndex < 0 || fromIndex >= items.Length || toIndex < 0 || toIndex >= toInventory.items.Length)
                return;
            
            // If moving into an empty slot or a slot that contains an item that cannot be merged into,
            if(toInventory.items[toIndex] == null && !toInventory.items[toIndex].TryMerge(items[fromIndex])) {
                // Swap the places of the items
                SlottedItem toItem = toInventory.items[toIndex];
                toInventory.items[toIndex] = items[fromIndex];
                items[toIndex] = toItem;
            }

            // Invoke actions to alert both inventories as having been modified
            Modified?.Invoke(fromIndex);
            toInventory.Modified?.Invoke(toIndex);
        }


        public void AddItem(SlottedItem item, int? intoIndex = null) {
            // If intoIndex is null, attempt to store into each slot until an open space is found
            List<int> emptySlots = new();

            for(int i=0; i<items.Length; i++) {
                if(items[i] == null) {
                    
                }
            }
        }
    }
}