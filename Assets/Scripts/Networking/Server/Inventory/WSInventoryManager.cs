using System.Collections.Generic;
using UnityEngine;
using Inventories;
using Unity.VisualScripting;

namespace Networking.Server {
    public class WSInventoryManager : BaseSingleton<WSInventoryManager> {
        private Dictionary<int, WSInventory> inventories = new();
        private BaseIdGenerator idGenerator = new();

        private HashSet<WSInventory> wsInventoriesWithUpdatesCache = new();

        public static bool AttachInventoryToEntity(WSEntity entity, Inventory inventory) {
            if(entity.GetComponent<WSInventory>() != null) {
                Debug.Log($"{entity.name} already had an attached inventory!");
                return false;
            }

            int inventoryId = Instance.idGenerator.GetNextEntityId(Instance.inventories);
            WSInventory entityWsInventory = entity.AddComponent<WSInventory>();
            entityWsInventory.Init(inventory, inventoryId);
            
            Instance.inventories[inventoryId] = entityWsInventory;
            return true;
        }
    }
}