using System.Collections.Generic;
using Controllers.Shared;
using Inventories;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;


public class InventoryUiManager : BaseUiElement {
    public enum InventoryOwnershipType {
        MainInventory,
        OtherInventory
    }
    public class InventoryUiPartition {
        public Inventory inventory;
        public InventoryUiSlotPlacementInfo[] inventoryPlacementInfos;
    }
    public class InventoryUiSlotPlacementInfo {
        public Transform container;
        public InventoryUiSlot slot;
    }

    [SerializeField] private GameObject inventorySlotPrefab;
    [SerializeField] private Transform hotbarSlots;
    [SerializeField] private Transform mainSlots;
    [SerializeField] private Transform otherInventorySlots;

    private Dictionary<InventoryOwnershipType, InventoryUiPartition> inventoryUiInfo;

    private ObjectPool<InventoryUiSlot> inventoryUiSlotPool;

    void Start() {
        if(Instance) {
            Destroy(gameObject);
        }
        Instance = this;

        IsOpen = true;
        Close();
        inventoryUiInfo = new();
    }

    public void SetInventorySlots(InventoryOwnershipType inventoryWindowType, Inventory inventory) {
        // First, release all slots inventory previously had, if any
        if(inventoryUiInfo.TryGetValue(inventoryWindowType, out var inventoryUiPartition)) {
            foreach(var inventoryPlacementInfo in inventoryUiPartition.inventoryPlacementInfos) {
                inventoryUiSlotPool.Release(inventoryPlacementInfo.slot);
            }
        }

        // Next, create slots again
        if(inventoryWindowType == InventoryOwnershipType.MainInventory) {
            if(inventory.SlottedItems.Length < 7) {
                throw new System.Exception($"Inventories must have at least 7 item spaces to be useable as a player inventory, {inventory.Id} only had {inventory.SlottedItems.Length}!");
            }

        } else if(inventoryWindowType == InventoryOwnershipType.OtherInventory) {

        }
    }

    public void AddInventorySlots(Transform to, Inventory inventory, int[] inventoryIndices) {
        
    }

    public static InventoryUiManager Instance { get; private set; }

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