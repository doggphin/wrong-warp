using LiteNetLib.Utils;

namespace Networking.Shared {
    public class SEntityTransformUpdatePkt : SPacket<SEntityTransformUpdatePkt>, IEntityUpdate {
        public TransformSerializable transform;
        public int CEntityId { get; set; }

        public override void Deserialize(NetDataReader reader) {
            transform.Deserialize(reader);
        }

        public override void Serialize(NetDataWriter writer) {
            writer.Put(PacketIdentifier.SEntityTransformUpdate);

            transform.Serialize(writer);
        }

        public override bool ShouldCache => true;
    }
}
