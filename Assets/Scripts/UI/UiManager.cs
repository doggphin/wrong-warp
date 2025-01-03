using System.Collections.Generic;
using Controllers.Shared;
using Unity.VisualScripting;
using UnityEngine;

public class UiManager : BaseSingleton<UiManager>
{   
    [SerializeField] private GameObject chatUiPrefab;
    [SerializeField] private GameObject escapeUiPrefab;
    private EscapeManager escapeUi;

    public static IUiElement ActiveUiElement { get; private set; }

    void Start() {
        ControlsManager.EscapeClicked += Escape;
        Instantiate(chatUiPrefab, transform);
        escapeUi = Instantiate(escapeUiPrefab, transform).GetComponent<EscapeManager>();
    }

    /// <returns> Whether a UI element was closed. </returns>
    public static bool CloseActiveUiElement() {
        if(ActiveUiElement == null)
            return false;

        ActiveUiElement.Close();
        ActiveUiElement = null;

        Cursor.lockState = CursorLockMode.Locked;
        ControlsManager.SetGameplayControlsEnabled(true);
        return true;
    }


    /// <returns> Whether the UI element was set. </returns>
    public static bool SetActiveUiElement(IUiElement uiElement, bool disableControls) {
        if(ActiveUiElement != null)
            return false;

        ActiveUiElement = uiElement;
        ActiveUiElement.Open();

        Cursor.lockState = CursorLockMode.None;
        ControlsManager.SetGameplayControlsEnabled(false);
        return true;
    }

    
    // When escape is pressed, either close the current ui or open the escape UI
    private static void Escape() {
        if(!CloseActiveUiElement()) {
            Debug.Log("Closed!");
            Instance.escapeUi.Open();
        }
    }
}
