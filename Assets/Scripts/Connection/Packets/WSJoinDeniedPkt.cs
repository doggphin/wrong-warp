using LiteNetLib.Utils;

namespace Networking.Shared {
    public class WSJoinDeniedPkt : INetSerializable {
        public string reason;

        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketType.SJoinDenied);

            writer.Put(reason);
        }

        public void Deserialize(NetDataReader reader) {
            reason = reader.GetString();
        }
    }
}
