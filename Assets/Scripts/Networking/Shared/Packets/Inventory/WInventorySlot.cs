using Inventories;
using LiteNetLib.Utils;

namespace Networking.Shared {
    /// <summary> This is literally just a wrapper for a SlottedItem optimized for empty values </summary>
    public class WInventorySlot : INetSerializable {
        public SlottedItem item;

        public void Deserialize(NetDataReader reader)
        {
            if(reader.GetBool() == false) {
                item = null;
                return;
            }

            item = new SlottedItem();
            item.Deserialize(reader);
        }

        public void Serialize(NetDataWriter writer)
        {
            if(item == null) {
                writer.Put(false);
                return;
            }

            writer.Put(item != null);
            item.Serialize(writer);
        }
    }
}