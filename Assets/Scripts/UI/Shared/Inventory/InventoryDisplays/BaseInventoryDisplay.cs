using System.Collections.Generic;
using Alchemy.Inspector;
using Inventories;
using UnityEngine;

public abstract class BaseInventoryDisplay : MonoBehaviour {
    protected Inventory inventory;
    protected Dictionary<int, InventoryUiVisualSlot> idxToSlots = new();

    public void Init(Inventory inventory)
    {
        this.inventory = inventory;
        GenerateSlots(inventory.SlottedItems.Length);
    }


    protected abstract void GenerateSlots(int count);


    public void UpdateSlotVisual(int idx) {
        idxToSlots[idx].SetVisibleSlottedItem(inventory[idx]);
    }
}