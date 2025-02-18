using System;
using LiteNetLib.Utils;
using UnityEngine.EventSystems;

namespace Networking.Shared {
    public class CMoveSlotRequest : CPacket<CMoveSlotRequest>
    {
        public PointerEventData.InputButton buttonType;

        public int fromInventoryId;
        public int fromIndex;

        public int? toInventoryId;
        public int? toIndex;

        public override void Deserialize(NetDataReader reader)
        {
            byte buttonByte = reader.GetByte();
            buttonType = Enum.IsDefined(typeof(PointerEventData.InputButton), buttonByte) ?
                (PointerEventData.InputButton)buttonByte :
                PointerEventData.InputButton.Left;

            fromInventoryId = reader.GetInt();
            fromIndex = (int)reader.GetVarUInt();

            if(reader.GetBool()) {
                toInventoryId = reader.GetInt();
                toIndex = (int)reader.GetVarUInt();
            }
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(PacketIdentifier.CMoveSlotRequest);

            writer.Put((byte)buttonType);
            
            writer.Put(fromInventoryId);
            writer.PutVarUInt((uint)fromIndex);
            
            writer.Put(toInventoryId.HasValue);
            if(toInventoryId.HasValue) {
                writer.Put(toInventoryId.Value);
                writer.PutVarUInt((uint)toIndex);
            }
        }
    }
}