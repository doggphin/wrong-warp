using LiteNetLib.Utils;

namespace Networking.Shared {
    public class CDropSlotRequest : CPacket<CDropSlotRequest>
    {
        public int fromInventoryId;
        public int fromIndex;
        public int amountToMove;

        public override void Deserialize(NetDataReader reader)
        {
            fromInventoryId = reader.GetInt();
            fromIndex = (int)reader.GetVarUInt();
            amountToMove = (int)reader.GetVarUInt();
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(PacketIdentifier.CDropSlotRequest);

            writer.Put(fromInventoryId);
            writer.PutVarUInt((uint)fromIndex);
            writer.PutVarUInt((uint)fromIndex);
        }
    }
}