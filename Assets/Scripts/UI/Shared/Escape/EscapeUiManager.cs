using Networking.Shared;
using UnityEngine;

public class EscapeUiManager : BaseUiElement<EscapeUiManager>
{
    void Start() {
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
