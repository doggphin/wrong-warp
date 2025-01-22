using LiteNetLib.Utils;

namespace Networking.Shared {
    public class InventoryDeltaSerializable : INetSerializable {
        public int index;
        public InventorySlotSerializable inventorySlot;

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