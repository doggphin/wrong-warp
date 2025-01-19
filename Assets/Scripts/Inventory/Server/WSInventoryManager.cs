using System.Collections.Generic;
using UnityEngine;
using Inventories;
using Unity.VisualScripting;
using UnityEngine.Video;

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
            Inventory inventory = new(inventoryTemplate);
            WSInventory wsInventory = AttachInventoryToEntity(entity, inventoryId, inventory);

            return wsInventory;
        }


        public static void DeleteInventory(WSInventory inventory) {
            Instance.inventories.Remove(inventory.Id);
            
        }


        private static WSInventory AttachInventoryToEntity(WSEntity entity, int inventoryId, Inventory inventory) {
            WSInventory entityWsInventory = entity.AddComponent<WSInventory>();
            entityWsInventory.Init(inventory, inventoryId);

            Instance.inventories[inventoryId] = entityWsInventory;

            entity.Player?.SetPersonalInventory(entityWsInventory);

            return entityWsInventory;
        }
    }
}