using System.Collections.Generic;
using TriInspector;
using UnityEngine;

public class PlayerInventoryDisplay : BaseInventoryDisplay {
    [SerializeField][AssetsOnly] private GameObject VisualSlotPrefab;

    [SerializeField] private RectTransform hotbarOrigin;
    [SerializeField] private RectTransform headOrigin;
    [SerializeField] private RectTransform chestOrigin;
    [SerializeField] private RectTransform legsOrigin;
    [SerializeField] private RectTransform feetOrigin;
    [SerializeField] private RectTransform mainOrigin;

    [SerializeField] private PlayerViewer playerViewer;


    protected override void GenerateSlots(int count)
    {
        for(int i=0; i<count; i++) {
            Transform origin = 
                i <= 3 ? hotbarOrigin :
                i == 4 ? headOrigin :
                i == 5 ? chestOrigin :
                i == 6 ? legsOrigin : 
                i == 7 ? feetOrigin :
                mainOrigin;

            idxToSlots[i] = Instantiate(VisualSlotPrefab, origin).GetComponent<InventoryUiVisualSlot>();
        }
    }


    void LateUpdate() {
        var parts = playerViewer.CurrentPlayerPartWorldSpacePositions;
        headOrigin.anchoredPosition = parts.head;
        chestOrigin.anchoredPosition = parts.body;
        legsOrigin.anchoredPosition = parts.legs;
        feetOrigin.anchoredPosition = parts.feet;
    } 
}

