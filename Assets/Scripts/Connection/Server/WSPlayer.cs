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
        public NetDataWriter reliableWriter = new();
        private WSInventory personalInventory;

        public void SetPersonalInventory(WSInventory inventory) {
            WSSetPersonalInventoryIdPkt setPersonalInventoryPkt = new() { personalInventoryId = inventory.Id };
            personalInventory = inventory;
        }
        private bool isInitialized = false;

        public WSPlayer(NetPeer peer, WSEntity entity) {
            Peer = peer;
            SetEntity(entity);
        }
        
        ///<summary> Sets the entity of this player and sends a notification to the peer that their entity has changed. </summary>
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

        
        public void PutPacket(INetSerializable packet, bool reliable = false) {
            packet.Serialize(reliable ? reliableWriter : unreliableWriter);
        }

        public void SendInstantPacket(INetSerializable packet, bool reliable) {
            WPacketCommunication.SendSingle(
                null, Peer, WSNetServer.Instance.GetTick(), packet, reliable ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Unreliable
            );
        }
    }
}
