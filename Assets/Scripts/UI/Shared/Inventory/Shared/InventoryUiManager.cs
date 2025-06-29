using System.Collections.Generic;
using Controllers.Shared;
using UnityEngine;
using TriInspector;
using Networking.Shared;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using JetBrains.Annotations;
using Audio.Shared;

namespace Inventories {
    public class InventoryUiManager : BaseUiElement<InventoryUiManager> {
        public override bool RequiresMouse => true;
        public override bool AllowsMovement => true;

        public static Action<CMoveSlotRequestPkt> RequestToMoveItem;

        private Dictionary<Inventory, BaseInventoryDisplay> inventoryDisplays = new();

        [AssetsOnly][SerializeField] private GameObject dragSlotPrefab;
        private DragSlot dragSlot;

        protected override void Awake() {                      
            InventoryUiVisualSlot.StartDrag += StartDrag;
            InventoryUiVisualSlot.Drop += Drop;

            dragSlot = Helpers.InstantiateAndGetComponent<DragSlot>(transform, dragSlotPrefab);

            base.Awake();
        }
        protected override void OnDestroy()
        {
            InventoryUiVisualSlot.StartDrag -= StartDrag;
            InventoryUiVisualSlot.Drop -= Drop;

            base.OnDestroy();
        }


        public void AddInventory(Inventory inventory) {
            var inventoryDisplay = Instantiate(inventory.Template.InventoryDisplayPrefab, transform).GetComponent<BaseInventoryDisplay>();
            inventoryDisplay.Init(inventory);
            inventoryDisplays.Add(
                inventory,
                inventoryDisplay
            );

            // Drag image should show on top of everything, always
            dragSlot.transform.SetAsLastSibling();
        }


        public void RemoveInventory(Inventory inventory) {
            Destroy(inventoryDisplays[inventory]);
        }


        public void UpdateSlotOfInventory(Inventory inventory, int slotIdx) {
            inventoryDisplays[inventory].UpdateSlotVisual(slotIdx);
        }
        
        int? fromIndex, toIndex;
        Inventory fromInventory, toInventory;
        PointerEventData.InputButton? draggingButton;
        private int GetDragStackSize(int stackSize, PointerEventData.InputButton button) {
            return button switch {
                PointerEventData.InputButton.Left => stackSize,
                PointerEventData.InputButton.Middle => stackSize / 2,
                PointerEventData.InputButton.Right => 1,
                _ => throw new NotImplementedException()
            };
        }
        private void StartDrag(Inventory inventory, int index, PointerEventData.InputButton button) {
            if(inventory[index] == null || inventory[index].stackSize == 0) {
                Debug.Log("CANT DRAG SLOT");
                return;
            }

            Debug.Log("START DRAG SLOT");
            var slotToDrag = inventoryDisplays[inventory].GetSlot(index);
            dragSlot.Show(slotToDrag);

            fromInventory ??= inventory;
            fromIndex ??= index;
            draggingButton ??= button;

            AudioManager.PlaySFX(inventory[index].BaseItemRef.AudioCollectionAddressable);
        }

        private void Drop(Inventory inventory, int index, PointerEventData.InputButton button) {
            toInventory = inventory;
            toIndex ??= index;

            if(draggingButton.HasValue && draggingButton.Value == button) {
                // Play the sound of the slot that will be being swapped with if possible
                // Otherwise just replay the sound effect of the item being moved
                if(toInventory[index] != null) {
                    AudioManager.PlaySFX(toInventory[toIndex.Value].BaseItemRef.AudioCollectionAddressable);
                } else {
                    AudioManager.PlaySFX(fromInventory[fromIndex.Value].BaseItemRef.AudioCollectionAddressable);
                }
                
                RequestToMoveItem?.Invoke(new CMoveSlotRequestPkt() {
                    buttonType = button,
                    fromInventoryId = fromInventory.Id,
                    fromIndex = fromIndex.Value,
                    toInventoryId = toInventory.Id,
                    toIndex = toIndex.Value,
                });
                Debug.Log("Requesting to move!");
            }
            
            Debug.Log("DROP DRAG SLOT");
            dragSlot.Hide();
            fromInventory = toInventory = null;
            fromIndex  = toIndex = null;
            draggingButton = null;
        }


        public void UpdateSlotVisual(Inventory inventory, int slot) {
            inventoryDisplays[inventory].UpdateSlotVisual(slot);
        }
    }
}