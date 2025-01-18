using System.Collections.Generic;
using UnityEngine;
using Inventories;
using Unity.VisualScripting;

namespace Networking.Server {
    public class WSInventoryManager : BaseSingleton<WSInventoryManager> {
        private Dictionary<int, WSInventory> inventories = new();
        private BaseIdGenerator idGenerator = new();

        private HashSet<WSInventory> wsInventoriesWithUpdatesCache = new();
        
        public static WSInventory CreateNewInventoryForEntity(WSEntity entity, InventoryTemplate inventoryTemplate) {
            if(entity.GetComponent<WSInventory>() != null) {
                Debug.Log($"{entity.name} already had an attached inventory!");
                return null;
            }

            int inventoryId = Instance.idGenerator.GetNextEntityId(Instance.inventories);
            Inventory inventory = new(inventoryId, inventoryTemplate);

            WSInventory wsInventory = AttachInventoryToEntity(entity, inventory);
            return wsInventory;
        }

        private static WSInventory AttachInventoryToEntity(WSEntity entity, Inventory inventory) {
            WSInventory entityWsInventory = entity.AddComponent<WSInventory>();
            entityWsInventory.Init(inventory, inventory.Id);

            Instance.inventories[inventory.Id] = entityWsInventory;

            entity.Player?.SetPersonalInventory(entityWsInventory);

            return entityWsInventory;
        }
    }
}