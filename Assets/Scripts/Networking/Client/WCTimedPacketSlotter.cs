

using System.Collections.Generic;
using UnityEngine;
using Networking.Shared;

namespace Networking.Client {
    public class WCPacketSlots {
        public List<WSEntityKillPkt> entityKillPackets = new();
        public List<WSEntitySpawnPkt> entitySpawnPackets = new();
        public List<WSEntityTransformUpdatePkt> entityTransformUpdatePackets = new();
    }

    public static class WCTimedPacketSlotter {
        private static int tick = 0;
        private static Dictionary<int, WCPacketSlots> receivedPackets = new();

        public static WCPacketSlots GetPacketSlots(int tick) {
            if(receivedPackets.TryGetValue(tick, out var packetSlots))
                return packetSlots;

            WCPacketSlots newPacketSlots = new();
            receivedPackets[tick] = newPacketSlots;

            return newPacketSlots;
        }

        public static void Init(int tick) {
            WCTimedPacketSlotter.tick = tick;
        }

        public static void AdvanceTick() {
            receivedPackets.Remove(tick - 20);
            tick++;

            if(!receivedPackets.TryGetValue(tick, out var packets)) {
                return;
            }

            foreach(var entityKillPacket in packets.entityKillPackets) {
                WCEntityManager.KillEntity(entityKillPacket);
            }

            foreach(var entitySpawnPacket in packets.entitySpawnPackets) {
                WCEntityManager.Spawn(entitySpawnPacket);
            }

            foreach(var entityTransformUpdatePacket in packets.entityTransformUpdatePackets) {
                WCEntityManager.UpdateEntityTransform(entityTransformUpdatePacket);
                Debug.Log("Sending a transform packet!");
            }

        }
    }
}