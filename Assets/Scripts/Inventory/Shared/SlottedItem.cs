using LiteNetLib.Utils;
using UnityEngine;

namespace Inventories {
    public class SlottedItem : INetSerializable {
        public ItemType SlottedItemType { get; private set; }
        public BaseItemSO BaseItemRef { get; private set; }
        public int stackSize;

        public SlottedItem() { }
        public SlottedItem(ItemType itemType, int stackSize) {
            Initialize(itemType, stackSize);
        }
        private void Initialize(ItemType itemType, int stackSize) {
            SlottedItemType = itemType;
            this.stackSize = stackSize;
            BaseItemRef = BaseItemRef = ItemLookup.Lookup(itemType);
        }
        public SlottedItem ShallowCopy() {
            return (SlottedItem)MemberwiseClone();
        }

        

        ///<summary> Tries to merge otherSlottedItem into this slottedItem. </summary>
        ///<param name="amountToTryAbsorb"> Defaults to itemToAbsorb's stack size </param>
        ///<returns> Whether otherSlottedItem was modified. </returns>
        public bool TryAbsorbSlottedItem(SlottedItem itemToAbsorb, int? amountToTryAbsorb = null) {
            if(itemToAbsorb == null)
                return false;
            
            amountToTryAbsorb ??= itemToAbsorb.stackSize;
            
            int roomLeft = BaseItemRef.MaxStackSize - stackSize;

            bool isDifferentItem = SlottedItemType != itemToAbsorb.SlottedItemType;
            bool isAlreadyFull = roomLeft <= 0;
            if(isDifferentItem || isAlreadyFull)
                return false;

            int amountToAbsorb = Mathf.Min(amountToTryAbsorb.Value, roomLeft, itemToAbsorb.stackSize);

            if(amountToAbsorb <= 0)
                return false;

            itemToAbsorb.stackSize -= amountToAbsorb;
            stackSize += amountToAbsorb;
            
            return true;
        }


        public void Deserialize(NetDataReader reader)
        {
            ItemType itemType = (ItemType)reader.GetUShort();
            int stackSize = reader.GetInt();

            Initialize(itemType, stackSize);
        }


        public void Serialize(NetDataWriter writer)
        {
            writer.Put((ushort)SlottedItemType);
            writer.Put(stackSize);
        }
    }
}
