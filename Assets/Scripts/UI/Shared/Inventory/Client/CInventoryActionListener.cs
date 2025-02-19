using Networking.Client;
using Networking.Server;
using Networking.Shared;

public class CInventoryActionListener : BaseInventoryActionListener<SInventoryActionListener> {
    protected override void HandleMoveSlotRequest(CMoveSlotRequestPkt request) => 
        CPacketPacker.SendSingleReliable(request);
}