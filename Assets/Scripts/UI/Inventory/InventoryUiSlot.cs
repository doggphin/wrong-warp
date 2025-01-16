using System;
using Inventories;
using TMPro;
using UnityEngine;

public class InventoryUiSlot {
    [SerializeField] Sprite itemSprite;
    [SerializeField] TMP_Text stackSizeText;

    public Action<int> WasClicked;
    
    public void SetVisibleSlottedItem(SlottedItem slottedItem) {
        var baseItem = slottedItem.GetBaseItem();

        stackSizeText.text = slottedItem.stackSize.ToString();
        itemSprite = baseItem.slotSprite;
    }

    public void Init(int inventoryIndex) {

    }
}