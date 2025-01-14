using UnityEngine;
using Networking.Shared;
using System.IO;
using System;

namespace Inventory {
    class Inventory : MonoBehaviour {
        public SlottedItem[] items;
        public Action<int> Modified;

        public void MoveItem(int fromIndex, int toIndex, Inventory toInventory = null) { 
            if(toInventory == null)
                toInventory = this;

            if(fromIndex < 0 || fromIndex >= items.Length || toIndex < 0 || toIndex >= toInventory.items.Length)
                return;
            
            if(toInventory.items[toIndex] == null) {
                items[fromIndex] = null;
                toInventory.items[toIndex] = items[fromIndex];
            } else {
                toInventory.items[toIndex].TryMerge(items[fromIndex]);
            }

            Modified?.Invoke(fromIndex);
            toInventory.Modified?.Invoke(toIndex);
        }
    }
}