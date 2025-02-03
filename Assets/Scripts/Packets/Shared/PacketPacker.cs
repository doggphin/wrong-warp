using LiteNetLib;
using LiteNetLib.Utils;

public static class PacketPacker : BaseSingleton<PacketPacker> {
    ///<summary> Puts tick (4 bytes) and on unreliable packets, fragmented packet info (2 bytes) </summary>
    public void StartPacketCollection(NetDataWriter writer, int tick) {
        writer.Reset();
        writer.Put(tick);
    }

    public void StartFragmentedPacketCollection(NetDataWriter writer, int tick) {
        StartPacketCollection(writer, tick);
        FragmentedPacketInfo fragmentInfo = new() { curFragmentIdx = 0, finalPacketPayloadLen = 0 };
        fragmentInfo.Serialize(writer);
    }

    public void SendSingle<T>(NetDataWriter writer, NetPeer peer, int tick,  T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
    {
        StartPacketCollection(writer, tick);
        packet.Serialize(writer);
        peer.Send(writer, deliveryMethod);
    }
}