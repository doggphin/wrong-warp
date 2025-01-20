using System.Collections.Generic;
using LiteNetLib.Utils;

namespace Networking.Shared {
    public class WSInventoryDeltaCollectionPkt : INetSerializable {
        public Dictionary<int, List<WInventoryDelta>> inventoryIdsToDeltas;

        public void Deserialize(NetDataReader reader) {
            inventoryIdsToDeltas = new();

            int amountOfInventoryIdsToDeltas = reader.GetByte();

            for(int i=0; i<amountOfInventoryIdsToDeltas; i++) {
                int inventoryId = reader.GetInt();
                int amountOfDeltas = (int)reader.GetVarUInt();
                List<WInventoryDelta> inventoryDeltas = new(amountOfDeltas);
                for(int j=0; j<amountOfDeltas; j++) {
                    WInventoryDelta inventoryDelta = new();
                    inventoryDelta.Deserialize(reader);
                    inventoryDeltas.Add(inventoryDelta);
                }
                inventoryIdsToDeltas[inventoryId] = inventoryDeltas;
            }
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketIdentifier.SInventoryDeltaCollection);

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