using LiteNetLib;
using LiteNetLib.Utils;
using Networking.Client;
using Networking.Server;
using Networking.Shared;

public class PacketPacker<ClassType> : BaseSingleton<PacketPacker<ClassType>> {
    private static NetDataWriter singlesWriter = new();
    protected static NetDataWriter GetSinglesWriter() {
        singlesWriter.Reset();
        return singlesWriter;
    }

    ///<summary> Puts tick (4 bytes) and on unreliable packets, fragmented packet info (2 bytes) </summary>
    public static void StartPacketCollection(NetDataWriter writer, int tick) {
        writer.Reset();
        writer.Put(tick);
    }

    public static void SendSingleReliable<T>(NetDataWriter writer, int tick, T packet) where T : INetSerializable
    {
        StartPacketCollection(writer, tick);
        packet.Serialize(writer);
        CNetManager.Instance.ServerPeer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public static void SendSingleReliable<T>(T packet, int? tick = null) where T : INetSerializable {
        SendSingleReliable(GetSinglesWriter(), tick ?? WWNetManager.GetTick(), packet);
    }
}