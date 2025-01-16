using System;
using Inventories;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryUiSlot : MonoBehaviour, IPointerClickHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler {
    [SerializeField] Image itemSprite;
    [SerializeField] TMP_Text stackSizeText;

    public Action<int, int> Inspect;
    public Action<int, int, PointerEventData.InputButton> StartDrag;
    public Action<int, int, PointerEventData.InputButton> PointerUp;
    
    public void SetVisibleSlottedItem(SlottedItem slottedItem) {
        var baseItem = slottedItem.GetBaseItem();

        stackSizeText.text = slottedItem.stackSize.ToString();
        itemSprite.sprite = baseItem.SlotSprite;
    }

    private int inventoryId;
    private int inventoryIndex;
    public void Init(int inventoryId, int inventoryIndex) {
        this.inventoryId = inventoryId;
        this.inventoryIndex = inventoryIndex;
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

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"Let go of pointer with {eventData.button} on {inventoryIndex}!");
    }
}