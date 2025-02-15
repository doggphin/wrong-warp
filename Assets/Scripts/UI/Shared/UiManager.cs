using System.Collections.Generic;
using Controllers.Shared;
using Inventories;
using UnityEngine;

public class UiManager : BaseSingleton<UiManager>
{   
    [SerializeField] private GameObject chatUiPrefab;
    [SerializeField] private GameObject escapeUiPrefab;
    [SerializeField] private GameObject inventoryUiPrefab;
    [SerializeField] private GameObject interactableUiPrefab;

    private ChatUiManager chatUiManager;
    private InventoryUiManager inventoryUiManager;
    private InteractableUiManager interactableUiManager;
    private EscapeUiManager escapeUiManager;

    public IUiElement ActiveUiElement { get; private set; }

    protected override void Awake() {
        
        interactableUiManager = Helpers.InstantiateAndGetComponent<InteractableUiManager>(transform, interactableUiPrefab);
        chatUiManager = Helpers.InstantiateAndGetComponent<ChatUiManager>(transform, chatUiPrefab);
        inventoryUiManager = Helpers.InstantiateAndGetComponent<InventoryUiManager>(transform, inventoryUiPrefab);
        escapeUiManager = Helpers.InstantiateAndGetComponent<EscapeUiManager>(transform, escapeUiPrefab);

        ControlsManager.EscapeClicked += OpenEscape;
        ControlsManager.InventoryClicked += () => TryToggleUiElement(inventoryUiManager);
        ControlsManager.ChatClicked += () => TryToggleUiElement(chatUiManager);

        base.Awake();
    }

    protected override void OnDestroy()
    {
        ControlsManager.EscapeClicked -= OpenEscape;

        CloseActiveUiElement();

        base.OnDestroy();
        Cursor.lockState = CursorLockMode.None;
    }


    /// <returns> Whether a UI element was closed. </returns>
    public void CloseActiveUiElement() {
        if(Instance.ActiveUiElement == null)
            return;

        Instance.ActiveUiElement.Close();
        Instance.ActiveUiElement = null;

        Cursor.lockState = CursorLockMode.Locked;
        ControlsManager.SetKeyboardControlsEnabled(true);
        ControlsManager.SetMouseControlsEnabled(true);
    }


    /// <returns> Whether the UI element was set. </returns>
    private void SetActiveUiElement(IUiElement uiElement) {
        if(ActiveUiElement != null)
            return;

        ActiveUiElement = uiElement;
        ActiveUiElement.Open();

        Cursor.lockState = uiElement.RequiresMouse ? CursorLockMode.None : CursorLockMode.Locked;
        ControlsManager.SetKeyboardControlsEnabled(uiElement.AllowsMovement);
        ControlsManager.SetMouseControlsEnabled(!uiElement.RequiresMouse);
    }

    
    private void TryToggleUiElement(IUiElement uiElement) {
        if(ActiveUiElement == null) {
            SetActiveUiElement(uiElement);
        } else if (ReferenceEquals(uiElement, ActiveUiElement) && !ReferenceEquals(uiElement, chatUiManager)) {
            CloseActiveUiElement();
        }
    }

    private void OpenEscape() {
        if(ActiveUiElement != null) {
            CloseActiveUiElement();   
        } else {
            SetActiveUiElement(escapeUiManager);
        }
    }
}
