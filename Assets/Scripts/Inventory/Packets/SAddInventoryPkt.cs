using System.Collections.Generic;
using Inventories;
using LiteNetLib.Utils;

namespace Networking.Shared {
    public class SAddInventoryPkt : SPacket<SAddInventoryPkt> {
        int id;
        public Inventory fullInventory;

        public override void Deserialize(NetDataReader reader) {
            id = reader.GetInt();
            fullInventory.Deserialize(reader);
        }

        public override void Serialize(NetDataWriter writer) {
            writer.Put(PacketIdentifier.SAddInventory);

            writer.Put(id);
            fullInventory.Serialize(writer);
        }

        public override bool ShouldCache => throw new System.NotImplementedException();
    }
}