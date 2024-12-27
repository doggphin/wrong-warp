using LiteNetLib.Utils;

namespace Networking.Shared {
    public class WSJoinAcceptPkt : INetSerializable {
        public string userName;
        public int playerEntityId;
        public int tick;

        public void Serialize(NetDataWriter writer) {
            writer.Put((ushort)WPacketType.SJoinAccept);

            writer.Put(userName);
            writer.Put(playerEntityId);
            writer.Put(tick);
        }

        public void Deserialize(NetDataReader reader) {
            userName = reader.GetString();
            playerEntityId = reader.GetInt();
            tick = reader.GetInt();
        }
    }
}
