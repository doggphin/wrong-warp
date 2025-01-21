using LiteNetLib.Utils;

namespace Networking.Shared {
    public class WSEntityTransformUpdatePkt : NetPacketForClient<WSEntityTransformUpdatePkt>, IEntityUpdate {
        public WTransformSerializable transform;
        public int CEntityId { get; set; }

        public override void Deserialize(NetDataReader reader) {
            transform.Deserialize(reader);
        }

        public override void Serialize(NetDataWriter writer) {
            writer.Put(WPacketIdentifier.SEntityTransformUpdate);

            transform.Serialize(writer);
        }

        public override bool ShouldCache => true;
    }
}
