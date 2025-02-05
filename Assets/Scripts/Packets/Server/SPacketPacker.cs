using LiteNetLib;
using LiteNetLib.Utils;

public class SPacketPacker : PacketPacker<SPacketPacker> {
    public void StartFragmentedPacketCollection(NetDataWriter writer, int tick) {
        StartPacketCollection(writer, tick);
        FragmentedPacketInfo fragmentInfo = new() { curFragmentIdx = 0, finalPacketPayloadLen = 0 };
        fragmentInfo.Serialize(writer);
    }

    public void SendSingle<T>(NetDataWriter writer, NetPeer peer, int tick, T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
    {
        StartPacketCollection(writer, tick);
        packet.Serialize(writer);
        peer.Send(writer, deliveryMethod);
    }
}