using UnityEngine;
using System.Collections.Generic;
using Inventories;
using LiteNetLib.Utils;
using Networking.Shared;
using System;

namespace Networking.Server {
    public class SInventory : MonoBehaviour {
        public int Id { get; private set; }
        private Inventory inventory;
        private SEntity entityRef;

        private HashSet<int> indicesThatHaveChanged;
        private HashSet<SPlayer> observers;
 
        public Action<SInventory> Modified;

        void Awake() {
            entityRef = GetComponent<SEntity>();
            // This is done instead of using RequireComponent as WSEntities must be added via WSEntityManager
            if(entityRef == null) {
                Debug.LogError("Can't add to a non-entity object!");
                Destroy(gameObject);
            }
        }

        public void Init(Inventory inventory, int id) {
            this.inventory = inventory;
            Id = id;
            inventory.Modified += InventoryModified;
        }

        public void AddObserver(SPlayer player) {
            if(!observers.Add(player)) {
                Debug.LogError("Tried to add an observer to an inventory that was already observing it!");
                return;
            }

            SAddInventoryPkt addInventoryPacket = new(){ fullInventory = inventory };
            player.ReliablePackets.AddPacket(SNetManager.Tick, addInventoryPacket);
        }

        public void RemoveObserver(SPlayer player) {
            if(!observers.Remove(player)) {
                Debug.LogError("Tried to remove an observer from an inventory that they were not observing!");
                return;
            }
        }

        public void InventoryModified(int index) {
            indicesThatHaveChanged.Add(index);
            Modified?.Invoke(this);
        }

        public List<InventoryDeltaSerializable> GetAndClearUpdates() {
            List<InventoryDeltaSerializable> inventoryDeltas = new();
            foreach(int index in indicesThatHaveChanged) {
                inventoryDeltas.Add(new InventoryDeltaSerializable { 
                    index = index, 
                    inventorySlot = new InventorySlotSerializable { 
                        item = inventory.SlottedItems[index] 
                    }
                });
            }
            
            indicesThatHaveChanged.Clear();
            return inventoryDeltas;
        }
    }
}