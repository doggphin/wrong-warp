using LiteNetLib;
using LiteNetLib.Utils;

public class PacketPacker<ClassType> : BaseSingleton<PacketPacker<ClassType>> {
    ///<summary> Puts tick (4 bytes) and on unreliable packets, fragmented packet info (2 bytes) </summary>
    public void StartPacketCollection(NetDataWriter writer, int tick) {
        writer.Reset();
        writer.Put(tick);
    }

    public void SendSingleReliable<T>(NetDataWriter writer, NetPeer peer, int tick, T packet) where T : INetSerializable
    {
        StartPacketCollection(writer, tick);
        packet.Serialize(writer);
        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }
}