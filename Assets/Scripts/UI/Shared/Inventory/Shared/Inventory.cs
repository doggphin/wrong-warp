using UnityEngine;
using LiteNetLib.Utils;
using System;

namespace Inventories { 
    public class Inventory : INetSerializable {
        public int Id { get; private set; }
        public InventoryTemplateSO Template { get; protected set; }
        public SlottedItem[] SlottedItems { get; protected set; }
        
        public Inventory(int id) {
            Id = id;
        }
        public Inventory(int id, InventoryTemplateSO template) {
            Id = id;
            SetTemplate(template);
        }
        public void SetTemplate(InventoryTemplateSO template) {
            Template = template;
            SlottedItems = new SlottedItem[template.SlotsCount];
        }


        public void Deserialize(NetDataReader reader)
        {
            ushort templateTypeCode = reader.GetUShort();
            InventoryTemplateType templateType = (InventoryTemplateType)templateTypeCode;
            SetTemplate(InventoryTemplateLookup.Lookup(templateType));

            for(int i=(int)reader.GetVarUInt(); i<SlottedItems.Length; i+=(int)reader.GetVarUInt()) {
                if(i<SlottedItems.Length) {
                    Debug.Log("Deserializing an item!");
                    SlottedItem item = new();
                    SlottedItems[i] = item;
                    item.Deserialize(reader);
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

                if(item == null) {
                    amountOfBlanks++;
                } else {
                    writer.PutVarUInt(amountOfBlanks);
                    item.Serialize(writer);
                    amountOfBlanks = 0;
                    Debug.Log("Put an item");
                }
            }
            
            // Finally, if there's any blanks left, put the amount of them
            if(amountOfBlanks != 0) {
                Debug.Log($"Putting last {amountOfBlanks}!");
                writer.PutVarUInt(amountOfBlanks);
            }
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