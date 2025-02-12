using UnityEngine;
using System.Collections.Generic;
using Inventories;
using Networking.Shared;
using LiteNetLib.Utils;
using System;

namespace Networking.Server {
    public class SInventory : Inventory {
        private SEntity entityRef;

        private HashSet<int> indicesThatHaveChanged;
        private HashSet<SPlayer> observers;

        private bool hasBroadcastModified;
        public Action<SInventory> Modified;

        private NetDataWriter updatesWriter = new();
        private bool updatesWriterIsWritten = false;

        void Awake() {
            entityRef = GetComponent<SEntity>();
            // This is done instead of using RequireComponent as WSEntities must be added via WSEntityManager
            if(entityRef == null) {
                Debug.LogError("Can't add to a non-entity object!");
                Destroy(this);
            }
        }


        public void AddObserver(SPlayer player) {
            if(!observers.Add(player)) {
                Debug.LogError("Tried to add an observer to an inventory that was already observing it!");
                return;
            }

            SAddInventoryPkt addInventoryPacket = new(){ inventory = this };
            player.ReliablePackets?.AddPacket(SNetManager.Tick, addInventoryPacket);
        }


        public void RemoveObserver(SPlayer player) {
            if(!observers.Remove(player)) {
                Debug.LogError("Tried to remove an observer from an inventory that they were not observing!");
                return;
            }
        }


        public bool TryGetUpdates(out NetDataWriter outWriter) {
            if(indicesThatHaveChanged.Count > 0) {
                outWriter = updatesWriter;

                if(!updatesWriterIsWritten) {
                    foreach(int index in indicesThatHaveChanged)
                    new InventoryDeltaSerializable { 
                        index = index, 
                        inventorySlot = new InventorySlotSerializable { 
                            item = SlottedItems[index] 
                        }
                    }.Serialize(updatesWriter);
                    updatesWriterIsWritten = true;
                }

                return true;
            } else {
                outWriter = null;
                return false;
            }
        }


        public void RecognizeModified(int index) {
            SlotUpdated?.Invoke(index); // Used to make sure host gets updates too
            if(hasBroadcastModified)
                return;
            
            indicesThatHaveChanged.Add(index);
            Modified?.Invoke(this);
            hasBroadcastModified = true;
        }


        public void ResetUpdates() {
            indicesThatHaveChanged.Clear();
            updatesWriter.Reset();
            hasBroadcastModified = false;
            updatesWriterIsWritten = false;
        }

        
        ///<summary> Tries to move an item from an index in this inventory to an index in another inventory. If toInventory is not specified, uses this inventory. </summary>
        public void MoveItem(int fromIndex, int toIndex, SInventory toSInventory = null) {
            // A null toInventory signifies moving within self
            // TODO: moving within self would mean movements within own inventory could be seriously optimized by sending MoveItem arguments rather than deltas
            if(toSInventory == null) {
                toSInventory = this;
            }

            // Don't allow interactions outside the bounds of the inventories' items array
            if(fromIndex < 0 || fromIndex >= SlottedItems.Length || toIndex < 0 || toIndex >= toSInventory.SlottedItems.Length)
                return;

            SlottedItem fromItem = SlottedItems[fromIndex];
            if(!toSInventory.AllowsItemClassificationAtIndex(fromIndex, fromItem.BaseItemRef.ItemClassificationBitflags))
                return;
            
            // If moving into an empty slot or a slot that contains an item that cannot be merged into,
            if(toSInventory.SlottedItems[toIndex] == null && !toSInventory.SlottedItems[toIndex].TryAbsorbSlottedItem(SlottedItems[fromIndex], SlottedItems[fromIndex].stackSize)) {
                // Swap the places of the items
                SlottedItem toItem = toSInventory.SlottedItems[toIndex];
                toSInventory.SlottedItems[toIndex] = SlottedItems[fromIndex];
                SlottedItems[toIndex] = toItem;
            }

            // Invoke actions to alert both inventories as having been modified
            RecognizeModified(fromIndex);        
            toSInventory.RecognizeModified(toIndex);
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
                if(slot == null && AllowsItemClassificationAtIndex(i, itemToAdd.BaseItemRef.ItemClassificationBitflags)) {
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
    }
}