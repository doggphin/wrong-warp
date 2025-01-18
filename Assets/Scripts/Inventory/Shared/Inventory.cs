using System;
using LiteNetLib.Utils;
using Unity.VisualScripting;

namespace Inventories { 
    public class Inventory : INetSerializable {
        public int Id { get; private set; }
        public InventoryTemplate Template { get; private set; }
        public SlottedItem[] SlottedItems { get; private set; }

        ///<summary> Generates an empty inventory from a template. </summary>
        public Inventory(int id, InventoryTemplate template) {
            Init(id, template);
        }

        private void Init(int id, InventoryTemplate template) {
            Id = id;
            Template = template;
            SlottedItems = new SlottedItem[template.slotsCount];
        }

        /// <summary> Where int represents the slot index that was modified </summary>
        public Action<int> Modified;
        
        ///<summary> Tries to move an item from an index in this inventory to an index in another inventory. If toInventory is not specified, uses this inventory. </summary>
        public void MoveItem(int fromIndex, int toIndex, Inventory toInventory = null) {
            // A null toInventory signifies moving within self
            // TODO: moving within self would mean movements within own inventory could be seriously optimized by sending MoveItem arguments rather than deltas
            toInventory ??= this;

            // Don't allow interactions outside the bounds of the inventories' items array
            if(fromIndex < 0 || fromIndex >= SlottedItems.Length || toIndex < 0 || toIndex >= toInventory.SlottedItems.Length)
                return;

            SlottedItem fromItem = SlottedItems[fromIndex];
            if(!toInventory.AllowsItemClassificationAtIndex(fromIndex, fromItem.BaseItemRef.ItemClassificationBitflags))
                return;
            
            // If moving into an empty slot or a slot that contains an item that cannot be merged into,
            if(toInventory.SlottedItems[toIndex] == null && !toInventory.SlottedItems[toIndex].TryAbsorbSlottedItem(SlottedItems[fromIndex], SlottedItems[fromIndex].stackSize)) {
                // Swap the places of the items
                SlottedItem toItem = toInventory.SlottedItems[toIndex];
                toInventory.SlottedItems[toIndex] = SlottedItems[fromIndex];
                SlottedItems[toIndex] = toItem;
            }

            // Invoke actions to alert both inventories as having been modified
            Modified?.Invoke(fromIndex);
            toInventory.Modified?.Invoke(toIndex);
        }


        ///<returns> Whether the item was modified/consumed. </returns>
        public bool TryAddItem(SlottedItem itemToAdd) {
            int initialStackSize = itemToAdd.stackSize;

            // During first run, try to stack the item into each item in the inventory;
            // Also, keep track of the most recent null slot in case it can't be stacked into anything
            int? firstEmptyIndex = null;
            for(int i=0; i<SlottedItems.Length; i++) {
                SlottedItem slot = SlottedItems[i];
                // Save first empty slot for use later if necessary
                if(slot == null && AllowsItemClassificationAtIndex(i, itemToAdd.BaseItemRef.ItemClassificationBitflags)) {
                    firstEmptyIndex ??= i;
                    continue;
                }
                // Try to merge item into slots it can
                if(!slot.TryAbsorbSlottedItem(itemToAdd))
                    continue;
                // If fully, successfully merged, 
                if(itemToAdd.stackSize == 0)
                    return true;
            }
            // If there's still items left and an empty space was found, store rest of item into an empty space
            if(firstEmptyIndex == null)
                return itemToAdd.stackSize == initialStackSize;
            // Put item in empty slot
            SlottedItems[firstEmptyIndex.Value] = itemToAdd;
            return true;
        }

        
        public bool AllowsItemClassificationAtIndex(int inventoryIndex, int itemClassificationBitFlags) {
            return Template.AllowsItemAtIndex(inventoryIndex, itemClassificationBitFlags);
        }

        public void Deserialize(NetDataReader reader)
        {
            int id = reader.GetInt();
            InventoryTemplateType templateType = (InventoryTemplateType)reader.GetUShort();

            Init(id, InventoryTemplateLookup.GetTemplate(templateType));

            for(int i=0; i<SlottedItems.Length; i++) {
                uint amountOfBlanks = reader.GetVarUInt();
                i += (int)amountOfBlanks;

                if(i<SlottedItems.Length) {
                    SlottedItem item = new();
                    item.Deserialize(reader);
                    SlottedItems[i] = item;
                }
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put((ushort)Template.templateType);

            // Put all items
            // For this, use run-length encoding; before every item, include amount of spaces since last item
            uint amountOfBlanks = 0;
            for(int i=0; i<SlottedItems.Length; i++) {
                SlottedItem item = SlottedItems[i];
                if(item != null) {
                    writer.PutVarUInt(amountOfBlanks);
                    amountOfBlanks = 0;
                    item.Serialize(writer);
                } else {
                    amountOfBlanks++;
                }
            }
            
            // Finally, if there's any blanks left, put the amount of them
            if(amountOfBlanks != 0)
                writer.Put(amountOfBlanks);
        }
    }
}