using System.Collections.Generic;
using Controllers.Shared;
using Inventories;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;
using TriInspector;

namespace Inventories {
    public class InventoryUiManager : BaseUiElement<InventoryUiManager> {
        Dictionary<Inventory, BaseInventoryDisplay> inventoryDisplays;

        [AssetsOnly][SerializeField] private GameObject inventorySlotPrefab;

        private ObjectPool<InventoryUiDisplaySlot> inventoryUiSlotPool;

        void Start() {
            IsOpen = true;
            Close();
        }

        public InventoryUiDisplaySlot[] GetSlots(int amount) {
            InventoryUiDisplaySlot[] ret = new InventoryUiDisplaySlot[amount];
            for(int i=0; i<amount; i++) {
                ret[i] = inventoryUiSlotPool.Get();
            }
            return ret;
        }

        public void ReturnSlots(InventoryUiDisplaySlot[] displaySlots) {
            foreach(var slot in displaySlots) {
                inventoryUiSlotPool.Release(slot);
            }
        }


        public void AddInventory(Inventory inventory) {
            inventoryDisplays.Add(
                inventory,
                Instantiate(inventory.Template.InventoryDisplayPrefab, transform).GetComponent<BaseInventoryDisplay>()
            );
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