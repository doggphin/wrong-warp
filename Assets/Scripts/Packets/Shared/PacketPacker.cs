using LiteNetLib;
using LiteNetLib.Utils;

public class PacketPacker<T> : BaseSingleton<PacketPacker<T>> {
    ///<summary> Puts tick (4 bytes) and on unreliable packets, fragmented packet info (2 bytes) </summary>
    public void StartPacketCollection(NetDataWriter writer, int tick) {
        writer.Reset();
        writer.Put(tick);
    }
}