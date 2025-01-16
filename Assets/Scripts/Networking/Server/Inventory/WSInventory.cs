using UnityEngine;
using System.Collections.Generic;
using Inventories;
using LiteNetLib.Utils;
using Networking.Shared;

namespace Networking.Server {
    class WSInventory : MonoBehaviour {
        WSEntity entity;
        public int Id => entity.Id;
        private Inventory inventory;
        private HashSet<int> deltasBuffer;
        private List<WSPlayer> observers;
        

        void Awake() {
            entity = GetComponent<WSEntity>();
            // This is done instead of using RequireComponent as WSEntities must be added via WSEntityManager
            if(entity == null) {
                Debug.LogError("Can't add to a non-entity object!");
                Destroy(gameObject);
            }
        }


        public void Init(Inventory inventory) {
            this.inventory = inventory;
            inventory.Modified += InventoryModified;
        }


        public void InventoryModified(int index) {
            deltasBuffer.Add(index);
        }


        public List<WInventoryDelta> GetAndClearUpdates() {
            List<WInventoryDelta> inventoryDeltas = new();
            foreach(int index in deltasBuffer) {
                inventoryDeltas.Add(new WInventoryDelta { 
                    index = index, 
                    inventorySlot = new WInventorySlot { 
                        item = inventory.SlottedItems[index] 
                    }
                });
            }
            
            deltasBuffer.Clear();
            return inventoryDeltas;
        }
    }
}