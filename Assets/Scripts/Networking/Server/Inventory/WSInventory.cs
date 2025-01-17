using UnityEngine;
using System.Collections.Generic;
using Inventories;
using LiteNetLib.Utils;
using Networking.Shared;
using System;

namespace Networking.Server {
    public class WSInventory : MonoBehaviour {
        WSEntity entityRef;
        private int id;
        public int Id => id;
        private Inventory inventory;
        private HashSet<int> deltasBuffer;
        private List<WSPlayer> observers;

        public Action<WSInventory> Modified;

        void Awake() {
            entityRef = GetComponent<WSEntity>();
            // This is done instead of using RequireComponent as WSEntities must be added via WSEntityManager
            if(entityRef == null) {
                Debug.LogError("Can't add to a non-entity object!");
                Destroy(gameObject);
            }
        }


        public void Init(Inventory inventory, int id) {
            this.inventory = inventory;
            this.id = id;
            inventory.Modified += InventoryModified;
        }


        public void InventoryModified(int index) {
            deltasBuffer.Add(index);
            Modified?.Invoke(this);
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