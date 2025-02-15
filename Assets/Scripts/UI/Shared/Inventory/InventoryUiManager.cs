using System.Collections.Generic;
using Controllers.Shared;
using UnityEngine;
using TriInspector;
using Networking.Shared;
using System;
using UnityEngine.EventSystems;

namespace Inventories {
    public struct MoveItemRequest {
        public int fromInventoryId, fromIndex, toInventoryId, toIndex;
        public PointerEventData.InputButton button;
    }


    public class InventoryUiManager : BaseUiElement<InventoryUiManager> {
        [AssetsOnly][SerializeField] private GameObject inventorySlotPrefab;

        private Dictionary<Inventory, BaseInventoryDisplay> inventoryDisplays = new();
        private MoveItemRequest moveItemRequest = new();
        public static Action<MoveItemRequest> RequestToMoveItem;

        public override bool RequiresMouse => true;
        public override bool AllowsMovement => true;
        protected override void Awake() {                      
            InventoryUiVisualSlot.StartDrag += StartDrag;
            InventoryUiVisualSlot.Drop += Drop;

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
        }


        public void RemoveInventory(Inventory inventory) {
            Destroy(inventoryDisplays[inventory]);
        }


        public void UpdateSlotOfInventory(Inventory inventory, int slotIdx) {
            inventoryDisplays[inventory].UpdateSlotVisual(slotIdx);
        }
        
        int? fromInventoryId, fromIndex, toInventoryId, toIndex;
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
            fromInventoryId ??= inventory.Id;
            fromIndex ??= index;
            draggingButton ??= button;
        }

        private void Drop(Inventory inventory, int index, PointerEventData.InputButton button) {
            toInventoryId ??= inventory.Id;
            toIndex ??= index;

            if(draggingButton.HasValue && draggingButton.Value == button) {
                RequestToMoveItem?.Invoke(new MoveItemRequest() {
                    button = button,
                    fromInventoryId = fromInventoryId.Value,
                    fromIndex = fromIndex.Value,
                    toInventoryId = toInventoryId.Value,
                    toIndex = toIndex.Value,
                });
                Debug.Log("Requesting to move!");
            }
            
            fromInventoryId = fromIndex = toInventoryId = toIndex = null;
            draggingButton = null;
        }
    }
}