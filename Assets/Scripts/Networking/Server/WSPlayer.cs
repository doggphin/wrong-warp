using System.Runtime.Serialization;
using LiteNetLib;
using LiteNetLib.Utils;
using Networking.Shared;
using Unity.VisualScripting;

namespace Networking.Server {
    public class WSPlayer {
        public static WSPlayer FromPeer(NetPeer peer) {
            return peer.Tag == null ? null : (WSPlayer)peer.Tag;
        }
        public static bool FromPeer(NetPeer peer, out WSPlayer player) {
            if(peer.Tag != null) {
                player = (WSPlayer)peer.Tag;
                return true;
            }

            player = null;
            return false;
        }

        public WSChunk previousChunk = null;
        public WSEntity Entity { get; private set; }
        public NetPeer Peer { get; private set; }
        public NetDataWriter unreliableWriter = new();

        private bool isInitialized = false;

        public void Init(NetPeer peer, WSEntity entity) {
            if (isInitialized)
                return;

            Peer = peer;
            SetEntity(entity);

            isInitialized = true;
        }

        public void SetEntity(WSEntity entity) {
            if(ReferenceEquals(entity, Entity))
                return;
            
            if(Entity != null)
                Entity.SetPlayer(null);

            entity.SetPlayer(this);
            Entity = entity;
            
            // Notify player that their entity has changed
            WSSetPlayerEntityPkt setPlayerPkt = new() {
                entityId = entity.Id
            };
            SendInstantPacket(setPlayerPkt, true);
        }

        
        public void AddUnreliableData(INetSerializable packet) {
            packet.Serialize(unreliableWriter);
        }

        public void SendInstantPacket(INetSerializable packet, bool reliable) {
            WPacketCommunication.SendSingle(
                null, Peer, WSNetServer.Instance.GetTick(), packet, reliable ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Unreliable
            );
        }
    }
}
