using LiteNetLib.Utils;

namespace Networking.Shared {
    public class InventoryDelta : INetSerializable {
        int index;
        WInventorySlot inventorySlot;

        public void Deserialize(NetDataReader reader)
        {
            index = reader.GetInt();
            inventorySlot = new();
            inventorySlot.Deserialize(reader);
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(index);
            inventorySlot.Serialize(writer);
        }
    }
}