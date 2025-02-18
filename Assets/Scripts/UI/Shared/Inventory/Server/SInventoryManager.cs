using System.Collections.Generic;
using UnityEngine;
using Inventories;
using Unity.VisualScripting;
using UnityEngine.Video;
using Networking.Shared;

namespace Networking.Server {
    public class SInventoryManager : BaseSingleton<SInventoryManager> {
        private Dictionary<int, SInventory> inventories = new();
        private BaseIdGenerator idGenerator = new();

        private HashSet<SInventory> inventoriesWithUpdatesBuffer = new();

        protected override void Awake()
        {
            InteractableTakeable.InteractedStart += HandlePickUpItem;
            base.Awake();
        }

        protected override void OnDestroy()
        {
            InteractableTakeable.InteractedStart -= HandlePickUpItem;
            base.OnDestroy();
        }


        private void HandlePickUpItem(InteractableTakeable takeable, BaseEntity entity) {
            // If the entity doesn't have an inventory or can't pick any of the item up, skip
            if(!entity.TryGetComponent(out SInventory inventory) || !inventory.TryAddItem(takeable.item))
                return;

            SEntity takeableEntity = takeable.GetComponent<SEntity>();
            int newStackSize = takeable.item.stackSize;
            if(newStackSize <= 0) {
                takeableEntity.StartDeath(EntityKillReason.Despawn);
            } else {
                takeableEntity.PushReliableUpdate(takeableEntity, new TakeableStackSizeUpdatePkt() {
                    stackSize = newStackSize
                });
            }
        }


        public static SInventory CreateNewInventoryForEntity(SEntity entity, InventoryTemplateSO inventoryTemplate) {
            if(entity.GetComponent<SInventory>() != null) {
                Debug.Log($"{entity.name} already had an attached inventory!");
                return null;
            }

            int inventoryId = Instance.idGenerator.GetNextEntityId(Instance.inventories);
            SInventory sInventory = entity.AddComponent<SInventory>();
            sInventory.Init(inventoryId, inventoryTemplate);
            Instance.inventories[inventoryId] = sInventory;

            sInventory.Modified += Instance.AddModifiedSInventoryToBuffer;

            return sInventory;
        }


        public static void DeleteInventory(SInventory sInventory) {
            sInventory.Modified -= Instance.AddModifiedSInventoryToBuffer;
            Instance.inventories.Remove(sInventory.Id);
            Destroy(sInventory);
        }


        private void AddModifiedSInventoryToBuffer(SInventory sInventory) {
            inventoriesWithUpdatesBuffer.Add(sInventory);
        }


        public void SendInventoryUpdates() {
            foreach(var inventory in inventoriesWithUpdatesBuffer) {
                inventory.SendAndClearUpdates();
            }
        }
    }
}