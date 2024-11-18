using LiteNetLib.Utils;

namespace Networking.Shared {
    public class WSJoinAcceptPkt : INetSerializable {
        public string userName;

        public void Serialize(NetDataWriter writer) {
            writer.Put(userName);
        }

        public void Deserialize(NetDataReader reader) {
            userName = reader.GetString();
        }
    }
}
