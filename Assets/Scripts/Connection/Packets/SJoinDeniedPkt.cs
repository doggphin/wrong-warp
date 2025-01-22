using LiteNetLib.Utils;

namespace Networking.Shared {
    public class SJoinDeniedPkt : INetSerializable {
        public string reason;

        public void Serialize(NetDataWriter writer) {
            writer.Put(PacketIdentifier.SJoinDenied);

            writer.Put(reason);
        }

        public void Deserialize(NetDataReader reader) {
            reason = reader.GetString();
        }
    }
}
