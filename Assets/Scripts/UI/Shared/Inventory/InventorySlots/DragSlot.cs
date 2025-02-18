using Alchemy.Serialization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class DragSlot : MonoBehaviour {
    private Image image;
    private RectTransform rectTransform;

    private void Awake() {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        gameObject.SetActive(false);
    }


    private void Update() => MoveToMouse();


    private void MoveToMouse() => rectTransform.position = Mouse.current.position.ReadValue();


    public void Show(InventoryUiVisualSlot slotToReplicate) {
        image.sprite = slotToReplicate.ItemImage.sprite;
        image.color = slotToReplicate.ItemImage.color;
        rectTransform.sizeDelta = slotToReplicate.GetComponent<RectTransform>().sizeDelta;
        gameObject.SetActive(true);
        MoveToMouse();
    }

    public void Hide() => gameObject.SetActive(false);
}