using UnityEngine;
using System.Collections.Generic;
using Inventories;
using Networking.Shared;
using LiteNetLib.Utils;
using System;
using System.Linq;

namespace Networking.Server {
    public class SInventory : Inventory {
        private SEntity entityRef;

        private HashSet<int> indicesThatHaveChanged = new();
        private HashSet<SPlayer> observers = new();

        private bool hasBroadcastModified;
        public Action<SInventory> Modified;

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
                InventoryUiManager.Instance.AddInventory(this);   
            } else {
                SAddInventoryPkt addInventoryPacket = new(){ inventory = this };
                player.ReliablePackets?.AddPacket(SNetManager.Tick, addInventoryPacket);
            }
            
        }


        public void RemoveObserver(SPlayer player) {
            if(player.IsHost) {
                InventoryUiManager.Instance.RemoveInventory(this);
            } else {
                if(!observers.Remove(player)) {
                    Debug.LogError("Tried to remove an observer from an inventory that they were not observing!");
                    return;
                }
            }
        }


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
                    slottedItem = SlottedItems[indexThatChanged]
                });
            }
            SInventoryDeltasPkt packet = new() {
                deltas = new(indicesThatHaveChanged.Count)
            };

            Debug.Log("Sending and clearing.");
            foreach(SPlayer player in observers) {
                if(player.IsHost) {
                    Debug.Log("On the host");
                    foreach(int index in indicesThatHaveChanged) {
                        Debug.Log($"Updating {index}");
                        InventoryUiManager.Instance.UpdateSlotOfInventory(this, index);
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


        ///<summary> Tries to add an item to somewhere in this inventory. </summary>
        ///<param name="itemToAdd"> The item to try to add. This gets modified within the function! </param>
        ///<returns> Whether the item was modified/consumed. </returns>
        public bool TryAddItem(SlottedItem itemToAdd) {
            // During first run, try to stack the item into each item in the inventory
            // Also, keep track of the first empty slot in case it can't be stacked into anything
            int? firstEmptyIndex = null;
            for(int i=0; i<SlottedItems.Length; i++) {
                SlottedItem slot = SlottedItems[i];
                // Save first empty slot for use later if necessary
                if(slot == null) {
                    if(AllowsItemClassificationAtIndex(i, itemToAdd.BaseItemRef.ItemClassificationBitflags)) {
                        firstEmptyIndex ??= i;
                    }
                    continue;
                }

                // Try to merge item into slots it can
                if(!slot.TryAbsorbSlottedItem(itemToAdd))
                    continue;
                
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
            SlottedItems[firstEmptyIndex.Value] = itemToAdd.ShallowCopy();
            itemToAdd.stackSize = 0;
            RecognizeModified(firstEmptyIndex.Value);
            return true;
        }
    }
}