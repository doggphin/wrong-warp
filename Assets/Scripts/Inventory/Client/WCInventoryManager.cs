using System.Collections.Generic;
using Inventories;
using Networking.Shared;

namespace Networking.Client {
    class WCInventoryManager : BaseSingleton<WCInventoryManager> {
        public const int MAX_CACHED_INVENTORIES = 20;
        public int PersonalInventoryId { get; private set; }
        private Dictionary<int, Inventory> inventories = new();
        private Queue<int> cachedInventoryQueue = new();

        public void SetPersonalInventoryId(int newId) {
            inventories.Remove(PersonalInventoryId);
            PersonalInventoryId = newId;
        }

        public void ReceiveInventoryFromServer(int id, Inventory inventory) {
            // Don't mess with queue if the inventory already exists in cache or it's our personal inventory
            if(!inventories.ContainsKey(id) && id != PersonalInventoryId) {
                // Put this inventory in the queue to be deleted after 20 more cached inventories
                cachedInventoryQueue.Enqueue(id);
                // If max queue size has been hit, delete the most recent one
                if(cachedInventoryQueue.Count > MAX_CACHED_INVENTORIES) {
                    int dequeuedInventory = cachedInventoryQueue.Dequeue();
                    inventories.Remove(dequeuedInventory);
                }
            }

            inventories[id] = inventory;
        }

        // TODO: save updates somewhere??
        public void ReceiveInventoryDeltaFromServer(int inventoryId, WInventoryDelta inventoryDelta) {
            inventories[inventoryId].SlottedItems[inventoryDelta.index] = inventoryDelta.inventorySlot.item;
        }
    }
}