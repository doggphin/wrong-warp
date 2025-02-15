using Networking.Shared;
using UnityEngine;

public class EscapeUiManager : BaseUiElement<EscapeUiManager>
{
    public override bool RequiresMouse => true;
    public override bool AllowsMovement => false;

    public void Disconnect() {
        WWNetManager.Disconnect(new WDisconnectInfo { reason = "Clicked Disconnect", wasExpected = true});
    }

    public void OpenSettings() {
        Debug.Log("Not implemented!");
    }

    public void ExitGame() {
        Debug.Log("Not implemented!");
    }
}
