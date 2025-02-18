using LiteNetLib.Utils;

namespace Networking.Shared {
    public class CChatMessagePkt : CPacket<CChatMessagePkt> {
        public string message;

        public override void Serialize(NetDataWriter writer) {
            writer.Put((ushort)PacketIdentifier.CChatMessage);

            writer.Put(message);
        }

        public override void Deserialize(NetDataReader reader) {
            message = reader.GetString();
        }
    }
}