using UnityEngine;
using LiteNetLib.Utils;
using System;

namespace Inventories { 
    public abstract class Inventory : MonoBehaviour, INetSerializable {
        public int Id { get; private set; }
        public InventoryTemplateSO Template { get; protected set; }
        public SlottedItem[] SlottedItems { get; protected set; }

        public Action<int> SlotUpdated;

        public void Init(int id, InventoryTemplateSO template) {
            Id = id;
            Template = template;
            SlottedItems = new SlottedItem[template.SlotsCount];
        }


        public void Deserialize(NetDataReader reader)
        {
            Template = InventoryTemplateLookup.Lookup((InventoryTemplateType)reader.GetUShort());

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
            writer.Put((ushort)Template.TemplateType);

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


        public bool AllowsItemClassificationAtIndex(int inventoryIndex, int itemClassificationBitFlags) {
            return Template.AllowsItemAtIndex(inventoryIndex, itemClassificationBitFlags);
        }


        public SlottedItem this[int index] {
            get {
                return SlottedItems[index];
            } set {
                SlottedItems[index] = value;
            }
        }
    }
}