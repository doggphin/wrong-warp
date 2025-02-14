using System.Collections.Generic;
using Controllers.Shared;
using UnityEngine;

public class UiManager : BaseSingleton<UiManager>
{   
    [SerializeField] private GameObject chatUiPrefab;
    [SerializeField] private GameObject escapeUiPrefab;
    [SerializeField] private GameObject inventoryUiPrefab;
    [SerializeField] private GameObject interactableUiPrefab;

    private EscapeUiManager escapeUi;

    public IUiElement ActiveUiElement { get; private set; }

    protected override void Awake() {
        ControlsManager.EscapeClicked += OpenEscape;
        
        Instantiate(interactableUiPrefab, transform);
        Instantiate(chatUiPrefab, transform);
        Instantiate(inventoryUiPrefab, transform);
        escapeUi = Instantiate(escapeUiPrefab, transform).GetComponent<EscapeUiManager>();

        base.Awake();
    }

    protected override void OnDestroy()
    {
        CloseActiveUiElement();
        base.OnDestroy();
    }

    /// <returns> Whether a UI element was closed. </returns>
    public static void CloseActiveUiElement() {
        if(Instance.ActiveUiElement == null)
            return;

        Instance.ActiveUiElement.Close();
        Instance.ActiveUiElement = null;

        Cursor.lockState = CursorLockMode.Locked;
        ControlsManager.SetGameplayControlsEnabled(true);
    }


    /// <returns> Whether the UI element was set. </returns>
    public static void SetActiveUiElement(IUiElement uiElement, bool disableControls) {
        if(Instance.ActiveUiElement != null)
            return;

        Instance.ActiveUiElement = uiElement;
        Instance.ActiveUiElement.Open();

        Cursor.lockState = CursorLockMode.None;
        ControlsManager.SetGameplayControlsEnabled(false);
    }

    
    // When escape is pressed, either close the current ui or open the escape UI
    private static void OpenEscape() {
        if(Instance.ActiveUiElement != null) {
            CloseActiveUiElement();   
        } else {
            Instance.escapeUi.Open();
        }
    }
}
