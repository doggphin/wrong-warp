using UnityEngine;
using System.Collections.Generic;
using Inventories;
using Networking.Shared;
using LiteNetLib.Utils;
using System;
using System.Linq;

namespace Networking.Server {
    public class SInventory : MonoBehaviour {
        private SEntity entityRef;

        private HashSet<int> indicesThatHaveChanged = new();
        private HashSet<SPlayer> observers = new();

        private bool hasBroadcastModified;
        public Action<SInventory> Modified;

        public Inventory inventory;

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

            Debug.Log($"Adding as an observer!");

            if(player.IsHost) {
                InventoryUiManager.Instance.AddInventory(inventory);   
            } else {
                SAddInventoryPkt addInventoryPacket = new(){ inventory = inventory };
                player.ReliablePackets?.AddPacket(SNetManager.Tick, addInventoryPacket);
            }
        }


        public void RemoveObserver(SPlayer player) {
            if(player.IsHost) {
                InventoryUiManager.Instance.RemoveInventory(inventory);
            } else {
                if(!observers.Remove(player)) {
                    Debug.LogError("Tried to remove an observer from an inventory that they were not observing!");
                    return;
                }
            }
        }


        public bool IsObservedBy(SPlayer player) => observers.Contains(player);


        public void OnDestroy()
        {
            foreach(var observer in observers) {
                RemoveObserver(observer);
            }
        }


        public void SendAndClearUpdates() {
            List<InventoryDeltaSerializable> deltas = new(indicesThatHaveChanged.Count);
            foreach (int indexThatChanged in indicesThatHaveChanged) {
                deltas.Add(new() {
                    idx = indexThatChanged,
                    slottedItem = inventory.SlottedItems[indexThatChanged]
                });
            }
            SInventoryDeltasPkt packet = new() {
                deltas = deltas
            };

            foreach(SPlayer player in observers) {
                if(player.IsHost) {
                    foreach(int index in indicesThatHaveChanged) {
                        InventoryUiManager.Instance.UpdateSlotOfInventory(inventory, index);
                    } 
                } else {
                    player.ReliablePackets.AddPacket(SNetManager.Tick, packet);
                }
            }

            indicesThatHaveChanged.Clear();
            hasBroadcastModified = false;
        }


        public void RecognizeModified(int index) {           
            indicesThatHaveChanged.Add(index);
            
            if(hasBroadcastModified)
                return;

            Modified?.Invoke(this);
            hasBroadcastModified = true;
        }

        
        ///<summary> Tries to move an item from an index in this inventory to an index in another inventory. If toSInventory is null, drops the item </summary>
        public void MoveItem(int fromIndex, int toIndex, SInventory toSInventory) {
            // Don't allow interactions outside the bounds of the inventories' items array
            if(fromIndex < 0 || fromIndex >= inventory.SlottedItems.Length || toIndex < 0 || toIndex >= toSInventory.inventory.SlottedItems.Length)
                return;

            SlottedItem fromItem = inventory.SlottedItems[fromIndex];
            if(!toSInventory.inventory.AllowsItemClassificationAtIndex(toIndex, fromItem.BaseItemRef.ItemClassificationBitflags))
                return;
            
            // If "to" slot is empty, just move it to it
            if(toSInventory.inventory.SlottedItems[toIndex] == null) {
                // Swap the places of the items
                toSInventory.inventory.SlottedItems[toIndex] = fromItem;
                inventory.SlottedItems[fromIndex] = null;
            // Otherwise try to merge it... If that doesn't work, return early
            } else if(!toSInventory.inventory.SlottedItems[toIndex].TryAbsorbSlottedItem(inventory.SlottedItems[fromIndex], inventory.SlottedItems[fromIndex].stackSize)) {
                return;
            }

            // Invoke actions to alert both inventories as having been modified
            RecognizeModified(fromIndex);      
            toSInventory.RecognizeModified(toIndex);
        }


        ///<summary> Tries to add an item to somewhere in this inventory. </summary>
        ///<param name="itemToAdd"> The item to try to add. This gets modified within the function! </param>
        ///<returns> Whether the item was modified/consumed. </returns>
        public bool TryAddItem(SlottedItem itemToAdd) {
            // During first run, try to stack the item into each item in the inventory
            // Also, keep track of the first empty slot in case it can't be stacked into anything
            int? firstEmptyIndex = null;
            for(int i=0; i<inventory.SlottedItems.Length; i++) {
                SlottedItem slot = inventory.SlottedItems[i];
                // Save first empty slot for use later if necessary
                if(slot == null) {
                    if(inventory.AllowsItemClassificationAtIndex(i, itemToAdd.BaseItemRef.ItemClassificationBitflags)) {
                        firstEmptyIndex ??= i;
                    }
                    continue;
                }

                // Try to merge item into slots it can
                if(!slot.TryAbsorbSlottedItem(itemToAdd)) {
                    Debug.Log($"Trying to absorb into {i}!");
                    continue;
                }
                    
                
                RecognizeModified(i);
                // Quit early if stack size went down to 0
                if(itemToAdd.stackSize == 0) {
                    return true;
                }
            }

            // If item wasn't fully merged and there aren't any empty slots, return false
            if(firstEmptyIndex == null)
                return false;

            // Otherwise move the item into the empty slot
            // Do an additional check to see if stack size is above what should be allowed to stop any further glitches
            int amountToRemove = Math.Min(itemToAdd.BaseItemRef.MaxStackSize, itemToAdd.stackSize);
            SlottedItem copy = itemToAdd.ShallowCopy();
            copy.stackSize = amountToRemove;
            inventory.SlottedItems[firstEmptyIndex.Value] = copy;
            itemToAdd.stackSize -= amountToRemove;

            RecognizeModified(firstEmptyIndex.Value);
            return true;
        }
    }
}