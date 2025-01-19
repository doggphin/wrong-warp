using System.Collections.Generic;
using Inventories;
using LiteNetLib.Utils;

namespace Networking.Shared {
    public class WSAddInventoryPkt : INetPacketForClient {
        int id;
        public Inventory fullInventory;

        public void Deserialize(NetDataReader reader) {
            id = reader.GetInt();
            fullInventory.Deserialize(reader);
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketType.SAddInventory);

            writer.Put(id);
            fullInventory.Serialize(writer);
        }

        public bool ShouldCache => throw new System.NotImplementedException();
        // TODO: implement this!!!!!
        public void ApplyOnClient(int tick)
        {
            throw new System.NotImplementedException();
        }
    }
}