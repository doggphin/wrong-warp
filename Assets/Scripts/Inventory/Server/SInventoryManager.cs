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
            SInventory sInventory = Instance.AttachInventoryToEntity(entity, inventoryId, inventoryTemplate);

            return sInventory;
        }


        public static void DeleteInventory(SInventory sInventory) {
            sInventory.Modified -= Instance.AddModifiedSInventory;
            Instance.inventories.Remove(sInventory.Id);
            Destroy(sInventory);
        }


        private SInventory AttachInventoryToEntity(SEntity entity, int inventoryId, InventoryTemplateSO inventoryTemplate) {
            SInventory sInventory = entity.AddComponent<SInventory>();
            sInventory.Init(inventoryId, inventoryTemplate);

            Instance.inventories[inventoryId] = sInventory;

            entity.Player?.SetPersonalInventory(sInventory);

            sInventory.Modified += AddModifiedSInventory;

            return sInventory;
        }


        private void AddModifiedSInventory(SInventory sInventory) {
            inventoriesWithUpdatesBuffer.Add(sInventory);
        }


        private void SendOutUpdates(SInventory sInventory) {
            
        }
    }
}