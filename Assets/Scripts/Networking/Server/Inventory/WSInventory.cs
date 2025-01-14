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
        private HashSet<int> deltas;
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
            deltas.Add(index);
        }

        public List<INetSerializable> GetUpdates() {
            List<INetSerializable> updates = new();

            List<WInventoryDelta> inventoryDeltas = new();
            foreach(int index in deltas) {
                inventoryDeltas.Add()
            }
            WSInventoryDeltaCollectionPkt inventoryDeltaCollectionPkt = new();
            deltas.Clear();
            throw new System.Exception("Not implemented!");
        }
    }
}