using System.Collections.Generic;
using Inventories;
using LiteNetLib.Utils;

namespace Networking.Shared {
    public class WSAddInventoryPkt : INetSerializable, IClientApplicablePacket {
        public Inventory fullInventory;

        public void Deserialize(NetDataReader reader) {
            fullInventory.Deserialize(reader);
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketType.SAddInventory);

            fullInventory.Serialize(writer);
        }

        // TODO: implement this!!!!!
        public void ApplyOnClient(int tick)
        {
            throw new System.NotImplementedException();
        }
    }
}