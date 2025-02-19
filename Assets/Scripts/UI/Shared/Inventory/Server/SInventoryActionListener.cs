using Networking.Server;
using Networking.Shared;
using UnityEngine;

public class SInventoryActionListener : BaseInventoryActionListener<SInventoryActionListener> {
    protected override void HandleMoveSlotRequest(CMoveSlotRequestPkt request) {
        Debug.Log("HANDLING MOVE SLOT REQUEST");
        request.BroadcastApply(SNetManager.Tick);
    }
        
}