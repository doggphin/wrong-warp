using LiteNetLib.Utils;

namespace Networking.Shared {
    public class WSJoinAcceptPkt : INetSerializable {
        public string userName;
        public int tick;

        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketType.SJoinAccept);

            writer.Put(userName);
            writer.Put(tick);
        }

        public void Deserialize(NetDataReader reader) {
            userName = reader.GetString();
            tick = reader.GetInt();
        }
    }
}
