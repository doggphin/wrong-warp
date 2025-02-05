using LiteNetLib;
using LiteNetLib.Utils;

public class CPacketPacker : PacketPacker<CPacketPacker> {
    public void SendSingleUnreliable<T>(NetDataWriter writer, NetPeer peer, int tick, T packet) where T : INetSerializable {
        StartPacketCollection(writer, tick);
        packet.Serialize(writer);
        peer.Send(writer, DeliveryMethod.Unreliable);
    }
}