using LiteNetLib.Utils;

namespace Networking.Shared {
    public class CMoveSlotRequest : CPacket<CMoveSlotRequest>
    {
        public int fromInventoryId;
        public int fromIndex;
        public int toInventoryId;
        public int toIndex;
        public int amountToMove;

        public override void Deserialize(NetDataReader reader)
        {
            fromInventoryId = reader.GetInt();
            fromIndex = (int)reader.GetVarUInt();
            toInventoryId = reader.GetInt();
            toIndex = (int)reader.GetVarUInt();
            amountToMove = (int)reader.GetVarUInt();
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(PacketIdentifier.CMoveSlotRequest);

            writer.Put(fromInventoryId);
            writer.PutVarUInt((uint)fromIndex);
            writer.Put(toInventoryId);
            writer.PutVarUInt((uint)toIndex);
            writer.PutVarUInt((uint)amountToMove);
        }
    }
}