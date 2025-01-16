using UnityEngine;
using Networking.Shared;
using System;
using System.Collections.Generic;

namespace Inventories {
    public enum InventoryType {
        Player,
        Chest,
    }
    
    public class Inventory {
        public int Id { get; private set; }
        public SlottedItem[] SlottedItems { get; private set; }

        public readonly Dictionary<InventoryType, int> BaseInventorySizes = new() {
            { InventoryType.Player, 23 },
            { InventoryType.Chest, 4 }
        };

        public Inventory(int id, InventoryType inventoryType) {
            SlottedItems = new SlottedItem[BaseInventorySizes[inventoryType]];
        }

        /// <summary> Where int represents the slot index that was modified </summary>
        public Action<int> Modified;
        
        public void MoveItem(int fromIndex, int toIndex, Inventory toInventory = null) { 
            // A null toInventory signifies moving within self
            toInventory ??= this;

            // Don't allow interactions outside the bounds of the inventories' items array
            if(fromIndex < 0 || fromIndex >= SlottedItems.Length || toIndex < 0 || toIndex >= toInventory.SlottedItems.Length)
                return;
            
            // If moving into an empty slot or a slot that contains an item that cannot be merged into,
            if(toInventory.SlottedItems[toIndex] == null && !toInventory.SlottedItems[toIndex].TryMerge(SlottedItems[fromIndex])) {
                // Swap the places of the items
                SlottedItem toItem = toInventory.SlottedItems[toIndex];
                toInventory.SlottedItems[toIndex] = SlottedItems[fromIndex];
                SlottedItems[toIndex] = toItem;
            }

            // Invoke actions to alert both inventories as having been modified
            Modified?.Invoke(fromIndex);
            toInventory.Modified?.Invoke(toIndex);
        }


        public void AddItem(SlottedItem item, int? intoIndex = null) {
            // If intoIndex is null, attempt to store into each slot until an open space is found
            List<int> emptySlots = new();

            for(int i=0; i<SlottedItems.Length; i++) {
                if(SlottedItems[i] == null) {
                    
                }
            }
        }
    }
}