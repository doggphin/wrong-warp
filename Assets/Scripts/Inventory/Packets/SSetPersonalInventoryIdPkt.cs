using Inventories;
using LiteNetLib.Utils;

namespace Networking.Shared {
    public class SSetPersonalInventoryIdPkt : SPacket<SSetPersonalInventoryIdPkt> {
        public int personalInventoryId;

        public override void Deserialize(NetDataReader reader) {
            personalInventoryId = reader.GetInt();
        }

        public override void Serialize(NetDataWriter writer) {
            writer.Put(PacketIdentifier.SSetPersonalInventoryId);

            writer.Put(personalInventoryId);
        }

        public override bool ShouldCache => throw new System.NotImplementedException();
    }
}