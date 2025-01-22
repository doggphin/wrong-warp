using System.Collections.Generic;
using LiteNetLib.Utils;

namespace Networking.Shared {
    public class SInventoryDeltaCollectionPkt : SPacket<SInventoryDeltaCollectionPkt> {
        public Dictionary<int, List<InventoryDeltaSerializable>> inventoryIdsToDeltas;

        public override void Deserialize(NetDataReader reader) {
            inventoryIdsToDeltas = new();

            int amountOfInventoryIdsToDeltas = reader.GetByte();

            for(int i=0; i<amountOfInventoryIdsToDeltas; i++) {
                int inventoryId = reader.GetInt();
                int amountOfDeltas = (int)reader.GetVarUInt();
                List<InventoryDeltaSerializable> inventoryDeltas = new(amountOfDeltas);
                for(int j=0; j<amountOfDeltas; j++) {
                    InventoryDeltaSerializable inventoryDelta = new();
                    inventoryDelta.Deserialize(reader);
                    inventoryDeltas.Add(inventoryDelta);
                }
                inventoryIdsToDeltas[inventoryId] = inventoryDeltas;
            }
        }

        public override void Serialize(NetDataWriter writer) {
            writer.Put(PacketIdentifier.SInventoryDeltaCollection);

            writer.Put((byte)inventoryIdsToDeltas.Count);
            
            foreach(var inventoryIdToDeltas in inventoryIdsToDeltas) {
                writer.Put(inventoryIdToDeltas.Key);
                writer.PutVarUInt((uint)inventoryIdToDeltas.Value.Count);
                foreach(var delta in inventoryIdToDeltas.Value) {
                    delta.Serialize(writer);
                }
            }
        }
    }
}