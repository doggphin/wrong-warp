using System;
using Inventories;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryUiVisualSlot : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IDropHandler /* IPointerUpHandler */ {
    [SerializeField] Image itemSprite;
    [SerializeField] TMP_Text stackSizeText;

    public Action<int, int> Inspect;
    public static Action<int, int, PointerEventData.InputButton> StartDrag;
    public static Action<int, int, PointerEventData.InputButton> Drop;
    
    public void SetVisibleSlottedItem(SlottedItem slottedItem) {
        var color = itemSprite.color;

        if(slottedItem != null) {
            stackSizeText.text = slottedItem.stackSize.ToString();
            var baseItem = slottedItem.BaseItemRef;
            itemSprite.sprite = baseItem.SlotSprite;
            color.a = 1;
        } else {
            color.a = 0;
        }

        itemSprite.color = color;
    }

    private int inventoryId;
    private int inventoryIndex;
    public void Init(int inventoryId, int inventoryIndex, SlottedItem item) {
        this.inventoryId = inventoryId;
        this.inventoryIndex = inventoryIndex;
        SetVisibleSlottedItem(item);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"Clicked pointer with {eventData.button} on {inventoryIndex}!");
        Inspect?.Invoke(inventoryId, inventoryIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"Started dragging pointer with {eventData.button} from {inventoryIndex}!");
        StartDrag?.Invoke(inventoryId, inventoryIndex, eventData.button);
    }

    // This needs to be here to use OnBeginDrag and OnEndDrag
    public void OnDrag(PointerEventData eventData) { }

    public void OnDrop(PointerEventData eventData) {
        Debug.Log($"Let go of pointer with {eventData.button} on {inventoryIndex}!");
        Drop?.Invoke(inventoryId, inventoryIndex, eventData.button);
    }

    /*public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"Let go of pointer with {eventData.button} on {inventoryIndex}!");
    }*/
}