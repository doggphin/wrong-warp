using Inventories;
using LiteNetLib.Utils;

namespace Networking.Shared {
    public class WSSetPersonalInventoryIdPkt : INetSerializable, IClientApplicablePacket {
        public int personalInventoryId;

        public void Deserialize(NetDataReader reader) {
            personalInventoryId = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketIdentifier.SSetPersonalInventoryId);

            writer.Put(personalInventoryId);
        }

        // TODO: implement this!!!!!
        public bool ShouldCache => throw new System.NotImplementedException();
        public void ApplyOnClient(int tick)
        {
            throw new System.NotImplementedException();
        }
    }
}