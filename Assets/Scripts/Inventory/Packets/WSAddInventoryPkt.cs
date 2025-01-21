using System.Collections.Generic;
using Inventories;
using LiteNetLib.Utils;

namespace Networking.Shared {
    public class WSAddInventoryPkt : NetPacketForClient<WSAddInventoryPkt> {
        int id;
        public Inventory fullInventory;

        public override void Deserialize(NetDataReader reader) {
            id = reader.GetInt();
            fullInventory.Deserialize(reader);
        }

        public override void Serialize(NetDataWriter writer) {
            writer.Put(WPacketIdentifier.SAddInventory);

            writer.Put(id);
            fullInventory.Serialize(writer);
        }

        public override bool ShouldCache => throw new System.NotImplementedException();
    }
}