using System.Xml.Linq;
using LiteNetLib.Utils;

public class FragmentedPacketInfo : INetSerializable
{
    public const int LENGTH = 3;
    
    public ushort finalPacketPayloadLen;
    public byte curFragmentIdx;

    public void Deserialize(NetDataReader reader)
    {
        finalPacketPayloadLen = reader.GetUShort();
        curFragmentIdx = reader.GetByte();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(finalPacketPayloadLen);
        writer.Put(curFragmentIdx);
    }
}