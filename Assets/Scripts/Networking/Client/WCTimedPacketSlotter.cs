

using System.Collections.Generic;
using UnityEngine;
using Networking.Shared;

namespace Networking.Client {
    public class WCPacketSlots {
        public bool applied = false;
        public List<WSEntityKillPkt> entityKillPackets = new();
        public List<WSEntitySpawnPkt> entitySpawnPackets = new();
        public List<WSEntityTransformUpdatePkt> entityTransformUpdatePackets = new();
    }

    public static class WCTimedPacketSlotter {
        private static Dictionary<int, WCPacketSlots> receivedPackets = new();

        private static WCPacketSlots GetPacketSlots(int tick) {
            if(receivedPackets.TryGetValue(tick, out var packetSlots))
                return packetSlots;

            WCPacketSlots newPacketSlots = new();
            receivedPackets[tick] = newPacketSlots;

            return newPacketSlots;
        }
        
        public static void SlotPacket(int tick, WSEntityKillPkt pkt) {
            WCPacketSlots slots = GetPacketSlots(tick);
            if(slots.applied)
                ApplyPacket(pkt);
            else
                slots.entityKillPackets.Add(pkt);
        }
        public static void SlotPacket(int tick, WSEntitySpawnPkt pkt) {
            WCPacketSlots slots = GetPacketSlots(tick);
            if(slots.applied)
                ApplyPacket(pkt);
            else
                slots.entitySpawnPackets.Add(pkt);
        }
        public static void SlotPacket(int tick, WSEntityTransformUpdatePkt pkt) {
            WCPacketSlots slots = GetPacketSlots(tick);
            if(slots.applied)
                ApplyPacket(pkt);
            else
                slots.entityTransformUpdatePackets.Add(pkt);
        }
        

        public static void ApplyTick(int tick) {
            receivedPackets.Remove(tick - WCommon.TICKS_PER_SECOND);

            if(!receivedPackets.TryGetValue(tick, out var packets)) {
                return;
            }

            foreach(var pkt in packets.entityKillPackets) {
                ApplyPacket(pkt);
            }

            foreach(var pkt in packets.entitySpawnPackets) {
                ApplyPacket(pkt);
            }

            foreach(var pkt in packets.entityTransformUpdatePackets) {
                ApplyPacket(pkt);
            }
        }
        private static void ApplyPacket(WSEntityKillPkt pkt) {
            WCEntityManager.KillEntity(pkt);
        }
        private static void ApplyPacket(WSEntitySpawnPkt pkt) {
            WCEntityManager.Spawn(pkt);
        }
        private static void ApplyPacket(WSEntityTransformUpdatePkt pkt) {
            WCEntityManager.UpdateEntityTransform(pkt);
        }
    }
}