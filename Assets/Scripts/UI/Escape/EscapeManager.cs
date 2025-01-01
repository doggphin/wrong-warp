using UnityEngine;

public class EscapeManager : BaseUiElement
{
    public static EscapeManager Instance { get; private set; }
    
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
        Debug.Log("Not implemented!");
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
