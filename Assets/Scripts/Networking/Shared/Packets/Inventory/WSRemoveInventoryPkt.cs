using Inventories;
using LiteNetLib.Utils;

namespace Networking.Shared {
    public class WSRemoveInventoryPkt : INetSerializable, IClientApplicablePacket {
        public int inventoryId;

        public void Deserialize(NetDataReader reader) {
            inventoryId = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketType.SRemoveInventory);

            writer.Put(inventoryId);
        }

        // TODO: implement this!!!!!
        public void ApplyOnClient(int tick)
        {
            throw new System.NotImplementedException();
        }
    }
}