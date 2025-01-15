using System.Collections.Generic;
using Controllers.Shared;
using UnityEngine;

public class UiManager : BaseSingleton<UiManager>
{   
    [SerializeField] private GameObject chatUiPrefab;
    [SerializeField] private GameObject escapeUiPrefab;
    [SerializeField] private GameObject inventoryUiPrefab;

    private EscapeManager escapeUi;

    public static IUiElement ActiveUiElement { get; private set; }

    void Start() {
        ControlsManager.EscapeClicked += OpenEscape;
        Instantiate(chatUiPrefab, transform);
        Instantiate(inventoryUiPrefab, transform);
        escapeUi = Instantiate(escapeUiPrefab, transform).GetComponent<EscapeManager>();
    }

    /// <returns> Whether a UI element was closed. </returns>
    public static void CloseActiveUiElement() {
        if(ActiveUiElement == null)
            return;

        ActiveUiElement.Close();
        ActiveUiElement = null;

        Cursor.lockState = CursorLockMode.Locked;
        ControlsManager.SetGameplayControlsEnabled(true);
    }


    /// <returns> Whether the UI element was set. </returns>
    public static void SetActiveUiElement(IUiElement uiElement, bool disableControls) {
        if(ActiveUiElement != null)
            return;

        ActiveUiElement = uiElement;
        ActiveUiElement.Open();

        Cursor.lockState = CursorLockMode.None;
        ControlsManager.SetGameplayControlsEnabled(false);
    }

    
    // When escape is pressed, either close the current ui or open the escape UI
    private static void OpenEscape() {
        if(ActiveUiElement != null) {
            CloseActiveUiElement();   
        } else {
            Instance.escapeUi.Open();
        }
    }
}
