using Inventories;
using LiteNetLib.Utils;

namespace Networking.Shared {
    public class WSSetPersonalInventoryIdPkt : INetSerializable, IClientApplicablePacket {
        public int personalInventoryId;

        public void Deserialize(NetDataReader reader) {
            personalInventoryId = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketType.SSetPersonalInventoryId);

            writer.Put(personalInventoryId);
        }

        // TODO: implement this!!!!!
        public void ApplyOnClient(int tick)
        {
            throw new System.NotImplementedException();
        }
    }
}