using Inventories;
using LiteNetLib.Utils;

namespace Networking.Shared {
    public class InventoryDeltaSerializable : INetSerializable {
        public int idx;
        public SlottedItem slottedItem;

        public void Deserialize(NetDataReader reader)
        {
            idx = reader.GetInt();
            slottedItem = new();
            slottedItem.Deserialize(reader);
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(idx);
            slottedItem.Serialize(writer);
        }
    }
}