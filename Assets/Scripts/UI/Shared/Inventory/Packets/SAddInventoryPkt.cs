using System.Collections.Generic;
using Inventories;
using LiteNetLib.Utils;

namespace Networking.Shared {
    public class SAddInventoryPkt : SPacket<SAddInventoryPkt> {
        public int id;
        public Inventory inventory;

        public override void Deserialize(NetDataReader reader) {
            id = reader.GetInt();

            inventory = new(id);
            inventory.Deserialize(reader);
        }

        public override void Serialize(NetDataWriter writer) {
            writer.Put(PacketIdentifier.SAddInventory);

            writer.Put(id);
            inventory.Serialize(writer);
        }

        public override bool ShouldCache => true;
    }
}