using Inventories;
using LiteNetLib.Utils;

namespace Networking.Shared {
    public class WSRemoveInventoryPkt : NetPacketForClient<WSRemoveInventoryPkt> {
        public int inventoryId;

        public override void Deserialize(NetDataReader reader) {
            inventoryId = reader.GetInt();
        }

        public override void Serialize(NetDataWriter writer) {
            writer.Put(WPacketIdentifier.SRemoveInventory);

            writer.Put(inventoryId);
        }

        // TODO: implement this!!!!!
        public override bool ShouldCache => throw new System.NotImplementedException();
    }
}