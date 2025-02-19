using LiteNetLib;
using LiteNetLib.Utils;
using Networking.Client;
using Networking.Shared;

public class CPacketPacker : PacketPacker<CPacketPacker> {
    public static void SendSingleUnreliable<T>(T packet, int? tick = null) where T : INetSerializable {
        NetDataWriter writer = GetSinglesWriter();

        StartPacketCollection(writer, tick ?? WWNetManager.GetTick());
        packet.Serialize(writer);

        CNetManager.Instance.ServerPeer.Send(writer, DeliveryMethod.Unreliable);
    }
}