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
        ///<param name="amountToTryToAbsorb"> If left null, will fill out with otherSlottedItem stack size </param>
        ///<returns> Whether otherSlottedItem was modified. </returns>
        public bool TryAbsorbSlottedItem(SlottedItem otherSlottedItem, int? amountToTryToAbsorb = null) {
            if(otherSlottedItem == null)
                return false;
            
            amountToTryToAbsorb ??= otherSlottedItem.stackSize;
            
            bool isSameItem = SlottedItemType == otherSlottedItem.SlottedItemType;
            bool otherItemIsFullStack = otherSlottedItem.stackSize == otherSlottedItem.BaseItemRef.MaxStackSize;
            if(!isSameItem || otherItemIsFullStack)
                return false;

            int maxAmountCouldTake = BaseItemRef.MaxStackSize - stackSize;
            int finalAmountToTake = Mathf.Min(amountToTryToAbsorb.Value, maxAmountCouldTake, otherSlottedItem.stackSize);

            if(finalAmountToTake == 0)
                return false;

            otherSlottedItem.stackSize -= finalAmountToTake;
            stackSize += finalAmountToTake;
            
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
