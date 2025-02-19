using System.Collections.Generic;
using LiteNetLib.Utils;

namespace Networking.Shared {
    public class SInventoryDeltasPkt : SPacket<SInventoryDeltasPkt> {
        public int inventoryId;
        public List<InventoryDeltaSerializable> deltas;

        public override void Deserialize(NetDataReader reader) {
            inventoryId = reader.GetInt();
            int deltasCount = (int)reader.GetVarUInt();
            deltas = new(deltasCount);

            for(int i=0; i<deltasCount; i++) {
                InventoryDeltaSerializable inventoryDelta = new();
                inventoryDelta.Deserialize(reader);
                deltas.Add(inventoryDelta);
            }
        }

        public override void Serialize(NetDataWriter writer) {
            writer.Put(PacketIdentifier.SInventoryDeltas);

            writer.Put(inventoryId);
            writer.PutVarUInt(deltas.Count);
            foreach(var delta in deltas) {
                delta.Serialize(writer);
            }
        }
        
        public override bool ShouldCache => false;
    }
}