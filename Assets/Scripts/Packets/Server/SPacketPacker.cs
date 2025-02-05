using LiteNetLib;
using LiteNetLib.Utils;

public class SPacketPacker : PacketPacker<SPacketPacker> {
    public void StartFragmentedPacketCollection(NetDataWriter writer, int tick) {
        StartPacketCollection(writer, tick);
        FragmentedPacketInfo fragmentInfo = new() { curFragmentIdx = 0, finalPacketPayloadLen = 0 };
        fragmentInfo.Serialize(writer);
    }
}