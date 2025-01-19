using System.Collections.Generic;
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
        private WSInventory personalInventory;

        public TickedPacketCollection ReliablePackets { get; private set; } = new();

        public void SetPersonalInventory(WSInventory inventory) {
            WSSetPersonalInventoryIdPkt setPersonalInventoryPkt = new() { personalInventoryId = inventory.Id };
            personalInventory = inventory;
        }

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
            ReliablePackets.AddPacket(WSNetServer.Tick, setPlayerPkt);
        }
    }
}
