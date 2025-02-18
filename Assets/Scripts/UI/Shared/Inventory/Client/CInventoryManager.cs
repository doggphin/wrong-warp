using System.Collections.Generic;
using Inventories;
using Networking.Server;
using Networking.Shared;

namespace Networking.Client {
    class CInventoryManager : BaseSingleton<CInventoryManager> {
        public int PersonalInventoryId { get; private set; }
        private Dictionary<int, Inventory> inventories = new();


        protected override void Awake()
        {
            SAddInventoryPkt.ApplyUnticked += HandleAddInventory;
            SRemoveInventoryPkt.ApplyUnticked += HandleRemoveInventory;
            SInventoryDeltasPkt.ApplyUnticked += HandleInventoryDeltas;
            base.Awake();
        }


        protected override void OnDestroy()
        {
            SAddInventoryPkt.ApplyUnticked -= HandleAddInventory;
            SRemoveInventoryPkt.ApplyUnticked -= HandleRemoveInventory;
            SInventoryDeltasPkt.ApplyUnticked -= HandleInventoryDeltas;
            base.OnDestroy();
        }


        private void HandleInventoryDeltas(SInventoryDeltasPkt pkt) {
            Inventory inventory = inventories[pkt.inventoryId];
            foreach(var delta in pkt.deltas) {
                inventory[delta.idx] = delta.slottedItem;
            }
        }

        private void HandleRemoveInventory(SRemoveInventoryPkt pkt) {
            inventories.Remove(pkt.inventoryId);
        }

        private void HandleAddInventory(SAddInventoryPkt pkt) {
            inventories[pkt.id] = pkt.inventory;
        }

        public void SetPersonalInventoryId(int newId) {
            inventories.Remove(PersonalInventoryId);
            PersonalInventoryId = newId;
        }

        public void ReceiveInventoryFromServer(int id, Inventory inventory) {
            inventories[id] = inventory;
        }

        public void ReceiveInventoryDeltaFromServer(int inventoryId, InventoryDeltaSerializable inventoryDelta) {
            inventories[inventoryId].SlottedItems[inventoryDelta.idx] = inventoryDelta.slottedItem;
        }
    }
}