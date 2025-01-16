using Controllers.Shared;
using UnityEngine;

public class InventoryUiManager : BaseUiElement {
    [SerializeField] GameObject inventorySlot;
    public static InventoryUiManager Instance { get; private set; }

    void Start() {
        if(Instance) {
            Destroy(gameObject);
        }
        Instance = this;

        IsOpen = true;
        Close();
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