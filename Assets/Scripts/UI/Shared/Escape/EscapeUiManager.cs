using Networking.Shared;
using UnityEngine;

public class EscapeUiManager : BaseUiElement
{
    public static EscapeUiManager Instance { get; private set; }

    
    void Start() {
        if(Instance) {
            Destroy(gameObject);
        }

        Instance = this;
        Close();
    }

    public void BackToGame() {
        UiManager.CloseActiveUiElement();
    }

    public void Disconnect() {
        WWNetManager.Disconnect(new WDisconnectInfo { reason = "Clicked Disconnect", wasExpected = true});
        UiManager.CloseActiveUiElement();
        Cursor.lockState = CursorLockMode.None;
    }

    public void OpenSettings() {
        Debug.Log("Not implemented!");
    }

    public void ExitGame() {
        Debug.Log("Not implemented!");
    }

    public override void Open()
    {
        if(!IsOpen) {
            base.Open();
            UiManager.SetActiveUiElement(this, true);
        }
    }
}
