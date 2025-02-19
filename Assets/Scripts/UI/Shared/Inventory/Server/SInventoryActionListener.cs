using Networking.Server;
using Networking.Shared;
using UnityEngine;

namespace Networking.Server {
    public class SInventoryActionListener : BaseInventoryActionListener<SInventoryActionListener> {
        protected override void HandleMoveSlotRequest(CMoveSlotRequestPkt request) {
            request.BroadcastApply(SNetManager.Tick);
        }       
    }
}