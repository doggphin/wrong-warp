using System.Collections.Generic;
using LiteNetLib.Utils;
using Mono.Cecil;

namespace Networking.Shared {
    public class WSInventoryDeltaCollectionPkt : INetSerializable {
        public class InventoryDelta : INetSerializable {
            int index;
            WInventorySlot inventorySlot;

            public void Deserialize(NetDataReader reader)
            {
                index = reader.GetInt();
                inventorySlot = new();
                inventorySlot.Deserialize(reader);
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(index);
                inventorySlot.Serialize(writer);
            }
        }

        public Dictionary<int, List<InventoryDelta>> inventoryIdsToDeltas;

        public void Deserialize(NetDataReader reader) {
            inventoryIdsToDeltas = new();

            int amountOfInventoryIdsToDeltas = reader.GetByte();

            for(int i=0; i<amountOfInventoryIdsToDeltas; i++) {
                int inventoryId = reader.GetInt();
                int amountOfDeltas = (int)reader.GetVarUInt();
                List<InventoryDelta> inventoryDeltas = new(amountOfDeltas);
                for(int j=0; j<amountOfDeltas; j++) {
                    InventoryDelta inventoryDelta = new();
                    inventoryDelta.Deserialize(reader);
                    inventoryDeltas.Add(inventoryDelta);
                }
                inventoryIdsToDeltas[inventoryId] = inventoryDeltas;
            }
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketType.SInventoryDeltaCollection);

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