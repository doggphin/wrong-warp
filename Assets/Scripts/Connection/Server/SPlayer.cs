using System.Collections.Generic;
using System.Runtime.Serialization;
using LiteNetLib;
using LiteNetLib.Utils;
using Networking.Shared;
using Unity.VisualScripting;

namespace Networking.Server {
    public static class SPlayerExtensions {
        public static bool TryGetWSPlayer(this NetPeer peer, out SPlayer player) {
            if(peer.Tag == null) {
                player = null;
                return false;
            } else {
                player = (SPlayer)peer.Tag;
                return true;
            }
        }
    }
    public class SPlayer {
        public static SPlayer FromPeer(NetPeer peer) {
            return peer.Tag == null ? null : (SPlayer)peer.Tag;
        }

        public SChunk previousChunk = null;
        public SEntity Entity { get; private set; }
        public NetPeer Peer { get; private set; }
        private SInventory personalInventory;

        public TickedPacketCollection ReliablePackets { get; private set; } = new();

        public void SetPersonalInventory(SInventory inventory) {
            SSetPersonalInventoryIdPkt setPersonalInventoryPkt = new() { personalInventoryId = inventory.Id };
            personalInventory = inventory;
        }

        public SPlayer(NetPeer peer, SEntity entity) {
            Peer = peer;
            SetEntity(entity);
        }
        
        ///<summary> Sets the entity of this player and sends a notification to the peer that their entity has changed. </summary>
        public void SetEntity(SEntity entity) {
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
            ReliablePackets.AddPacket(SNetManager.Tick, setPlayerPkt);
        }
    }
}
