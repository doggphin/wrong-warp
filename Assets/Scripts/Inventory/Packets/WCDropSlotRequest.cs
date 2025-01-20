using LiteNetLib.Utils;

namespace Networking.Shared {
    public class WCDropSlotRequest : INetSerializable
    {
        public int fromInventoryId;
        public int fromIndex;
        public int amountToMove;

        public void Deserialize(NetDataReader reader)
        {
            fromInventoryId = reader.GetInt();
            fromIndex = (int)reader.GetVarUInt();
            amountToMove = (int)reader.GetVarUInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(WPacketIdentifier.CDropSlotRequest);

            writer.Put(fromInventoryId);
            writer.PutVarUInt((uint)fromIndex);
            writer.PutVarUInt((uint)fromIndex);
        }
    }
}