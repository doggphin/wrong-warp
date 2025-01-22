using Inventories;
using LiteNetLib.Utils;

namespace Networking.Shared {
    public class SRemoveInventoryPkt : SPacket<SRemoveInventoryPkt> {
        public int inventoryId;

        public override void Deserialize(NetDataReader reader) {
            inventoryId = reader.GetInt();
        }

        public override void Serialize(NetDataWriter writer) {
            writer.Put(PacketIdentifier.SRemoveInventory);

            writer.Put(inventoryId);
        }

        // TODO: implement this!!!!!
        public override bool ShouldCache => throw new System.NotImplementedException();
    }
}