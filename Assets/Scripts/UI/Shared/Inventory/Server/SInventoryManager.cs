using System.Collections.Generic;
using UnityEngine;
using Inventories;
using Unity.VisualScripting;
using UnityEngine.Video;
using Networking.Shared;
using LiteNetLib;

namespace Networking.Server {
    [RequireComponent(typeof(SInventoryActionListener))]
    public class SInventoryManager : BaseSingleton<SInventoryManager> {
        private Dictionary<int, SInventory> inventories = new();
        private BaseIdGenerator idGenerator = new();

        private HashSet<SInventory> inventoriesWithUpdatesBuffer = new();

        protected override void Awake()
        {
            InteractableTakeable.InteractedStart += HandlePickUpItem;
            CMoveSlotRequestPkt.ApplyUnticked += HandleMoveSlot;
            base.Awake();
        }

        protected override void OnDestroy()
        {
            InteractableTakeable.InteractedStart -= HandlePickUpItem;
            CMoveSlotRequestPkt.ApplyUnticked -= HandleMoveSlot;
            base.OnDestroy();
        }


        private void HandleMoveSlot(CMoveSlotRequestPkt pkt, NetPeer peer) {
            if(!SPlayer.TryFromPeer(peer, out SPlayer player)) {
                return;
            }
            
            if(!inventories.TryGetValue(pkt.fromInventoryId, out SInventory fromInventory) || !fromInventory.IsObservedBy(player)) {
                return;
            }

            SInventory toInventory;
            if(!pkt.toInventoryId.HasValue) {
                toInventory = fromInventory;
            } else {
                if(!inventories.TryGetValue(pkt.toInventoryId.Value, out toInventory) || !toInventory.IsObservedBy(player)) {
                    return;
                }
            }

            if(pkt.toIndex.HasValue) {
                fromInventory.MoveItem(pkt.fromIndex, pkt.toIndex.Value, toInventory);
            } else {
                // Drop the item, somehow
            }
            
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
            sInventory.inventory = new Inventory(inventoryId, inventoryTemplate);
            Instance.inventories[inventoryId] = sInventory;

            sInventory.Modified += Instance.AddModifiedSInventoryToBuffer;

            return sInventory;
        }


        public static void DeleteInventory(SInventory sInventory) {
            sInventory.Modified -= Instance.AddModifiedSInventoryToBuffer;
            Instance.inventories.Remove(sInventory.inventory.Id);
            Destroy(sInventory);
        }


        private void AddModifiedSInventoryToBuffer(SInventory sInventory) {
            inventoriesWithUpdatesBuffer.Add(sInventory);
        }


        public void SendInventoryUpdates() {
            foreach(var inventory in inventoriesWithUpdatesBuffer) {
                inventory.SendAndClearUpdates();
            }
            inventoriesWithUpdatesBuffer.Clear();
        }
    }
}