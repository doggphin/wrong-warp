using System.Runtime.InteropServices.WindowsRuntime;
using LiteNetLib.Utils;
using UnityEngine;

namespace Inventories {
    public class SlottedItem : INetSerializable {
        public ItemType itemType;
        public int stackSize;

        public BaseItemSO GetBaseItem() => ItemLookup.Lookup(itemType);

        // Tries to merge otherSlottedItem into this slottedItem.
        // Returns whether otherSlottedItem was modified.
        public bool TryMerge(SlottedItem otherSlottedItem) {
            if(otherSlottedItem == null || itemType != otherSlottedItem.itemType)
                return false;

            BaseItemSO baseItem = GetBaseItem();
            if(baseItem.maxStackSize <= stackSize)
                return false;

            int amountToMerge = Mathf.Min(baseItem.maxStackSize - stackSize, stackSize + otherSlottedItem.stackSize);
            otherSlottedItem.stackSize -= amountToMerge;
            stackSize += amountToMerge;
            
            return true;
        }

        public void Deserialize(NetDataReader reader)
        {
            itemType = (ItemType)reader.GetUShort();
            stackSize = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put((ushort)itemType);
            writer.Put(stackSize);
        }
    }
}
