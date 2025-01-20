using Inventories;
using LiteNetLib.Utils;

namespace Networking.Shared {
    public class WSRemoveInventoryPkt : INetPacketForClient {
        public int inventoryId;

        public void Deserialize(NetDataReader reader) {
            inventoryId = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketIdentifier.SRemoveInventory);

            writer.Put(inventoryId);
        }

        // TODO: implement this!!!!!
        public bool ShouldCache => throw new System.NotImplementedException();
        public void ApplyOnClient(int tick)
        {
            throw new System.NotImplementedException();
        }
    }
}