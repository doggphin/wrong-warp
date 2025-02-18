using System;
using Inventories;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryUiVisualSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IDropHandler /* IPointerUpHandler, IPointerClickHandler, */ {
    [field: SerializeField] public Image ItemImage { get; private set; }
    [SerializeField] private TMP_Text stackSizeText;

    private Inventory inventory;
    private int inventoryIndex;

    public Action<Inventory, int> Inspect;
    public static Action<Inventory, int, PointerEventData.InputButton> StartDrag;
    public static Action<Inventory, int, PointerEventData.InputButton> Drop;
    
    public void SetVisibleSlottedItem(SlottedItem slottedItem) {
        var color = ItemImage.color;

        if(slottedItem != null) {
            stackSizeText.text = slottedItem.stackSize.ToString();
            var baseItem = slottedItem.BaseItemRef;
            ItemImage.sprite = baseItem.SlotSprite;
            color.a = 1;
        } else {
            color.a = 0;
        }

        ItemImage.color = color;
    }

    public void Init(Inventory inventory, int inventoryIndex, SlottedItem item) {
        this.inventory = inventory;
        this.inventoryIndex = inventoryIndex;
        SetVisibleSlottedItem(item);
    }

    /*public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"Clicked pointer with {eventData.button} on {inventoryIndex}!");
        Inspect?.Invoke(inventory, inventoryIndex);
    }*/

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"Started dragging pointer with {eventData.button} from {inventoryIndex}!");
        StartDrag?.Invoke(inventory, inventoryIndex, eventData.button);
    }

    // This needs to be here to use OnBeginDrag and OnEndDrag
    public void OnDrag(PointerEventData eventData) { }

    public void OnDrop(PointerEventData eventData) {
        Debug.Log($"Let go of pointer with {eventData.button} on {inventoryIndex}!");
        Drop?.Invoke(inventory, inventoryIndex, eventData.button);
    }

    /*public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"Let go of pointer with {eventData.button} on {inventoryIndex}!");
    }*/
}