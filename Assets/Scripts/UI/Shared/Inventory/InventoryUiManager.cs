using System.Collections.Generic;
using Controllers.Shared;
using UnityEngine;
using TriInspector;
using Networking.Shared;

namespace Inventories {
    public class InventoryUiManager : BaseUiElement<InventoryUiManager> {
        private Dictionary<Inventory, BaseInventoryDisplay> inventoryDisplays = new();

        [AssetsOnly][SerializeField] private GameObject inventorySlotPrefab;

        protected override void Awake() {
            IsOpen = true;
            Close();
            
            WWNetManager.Disconnected += CleanupDisplays;

            base.Awake();
        }


        protected override void OnDestroy()
        {
            WWNetManager.Disconnected -= CleanupDisplays;
            base.OnDestroy();
        }


        private void CleanupDisplays(WDisconnectInfo _) {
            foreach(Transform child in transform) {
                Destroy(child.gameObject);
            }
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


        public override void Open()
        {
            if(IsOpen)
                return;
            
            base.Open();
            
            ControlsManager.InventoryClicked -= SetAsActive;
            ControlsManager.InventoryClicked += UiManager.CloseActiveUiElement;
        }


        public override void Close()
        {
            if(!IsOpen)
                return;
            
            base.Close();

            ControlsManager.InventoryClicked += SetAsActive;
            ControlsManager.InventoryClicked -= UiManager.CloseActiveUiElement;
        }


        private void SetAsActive() {
            UiManager.SetActiveUiElement(this, true);
        }
    }
}