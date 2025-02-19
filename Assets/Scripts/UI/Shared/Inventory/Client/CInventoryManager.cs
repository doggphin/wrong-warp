using System.Collections.Generic;
using Inventories;
using Networking.Server;
using Networking.Shared;
using UnityEngine;

namespace Networking.Client {
    [RequireComponent(typeof(CInventoryActionListener))]
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
            Debug.Log("THIS IS BEING CALLED ASDF");
            Inventory inventory = inventories[pkt.inventoryId];
            foreach(var delta in pkt.deltas) {
                inventory[delta.idx] = delta.slottedItem;
                InventoryUiManager.Instance.UpdateSlotVisual(inventory, delta.idx);
                Debug.Log($"Updated {delta.idx}!");
            }
        }


        private void HandleRemoveInventory(SRemoveInventoryPkt pkt) {
            Inventory inventoryToRemove = inventories[pkt.inventoryId];
            inventories.Remove(pkt.inventoryId);
            InventoryUiManager.Instance.RemoveInventory(inventoryToRemove);
        }


        private void HandleAddInventory(SAddInventoryPkt pkt) {
            inventories[pkt.id] = pkt.inventory;
            InventoryUiManager.Instance.AddInventory(pkt.inventory);
            Debug.Log("Adding an inventory visual!!!!");
        }
    }
}