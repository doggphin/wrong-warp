using Inventories;
using LiteNetLib.Utils;
using Unity.VisualScripting;

namespace Networking.Shared {
    public class TakeableStackSizeUpdatePkt : SPacket<TakeableStackSizeUpdatePkt> {
        public int stackSize;

        public override void Deserialize(NetDataReader reader) {
            stackSize = (int)reader.GetVarUInt();
        }


        public override void Serialize(NetDataWriter writer) {
            writer.Put(PacketIdentifier.STakeableStackSizeUpdate);
            writer.PutVarUInt(stackSize);
        }

        public override bool ShouldCache => true;
    }
}