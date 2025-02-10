using System.Collections.Generic;
using UnityEngine;
using Inventories;
using Unity.VisualScripting;
using UnityEngine.Video;

namespace Networking.Server {
    public class SInventoryManager : BaseSingleton<SInventoryManager> {
        private Dictionary<int, SInventory> inventories = new();
        private BaseIdGenerator idGenerator = new();

        private HashSet<SInventory> inventoriesWithUpdatesBuffer = new();
        
        public static SInventory CreateNewInventoryForEntity(SEntity entity, InventoryTemplateSO inventoryTemplate) {
            if(entity.GetComponent<SInventory>() != null) {
                Debug.Log($"{entity.name} already had an attached inventory!");
                return null;
            }

            int inventoryId = Instance.idGenerator.GetNextEntityId(Instance.inventories);
            Inventory inventory = new(inventoryTemplate);
            SInventory wsInventory = AttachInventoryToEntity(entity, inventoryId, inventory);

            return wsInventory;
        }


        public static void DeleteInventory(SInventory inventory) {
            Instance.inventories.Remove(inventory.Id);
        }


        private static SInventory AttachInventoryToEntity(SEntity entity, int inventoryId, Inventory inventory) {
            SInventory entityWsInventory = entity.AddComponent<SInventory>();
            entityWsInventory.Init(inventory, inventoryId);

            Instance.inventories[inventoryId] = entityWsInventory;

            entity.Player?.SetPersonalInventory(entityWsInventory);

            return entityWsInventory;
        }
    }
}