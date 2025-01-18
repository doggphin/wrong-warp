using LiteNetLib.Utils;

namespace Networking.Shared {
    public class WCChatMessagePkt : INetSerializable {
        public string message;

        public void Serialize(NetDataWriter writer) {
            writer.Put((ushort)WPacketType.CChatMessage);

            writer.Put(message);
        }

        public void Deserialize(NetDataReader reader) {
            message = reader.GetString();
        }
    }
}