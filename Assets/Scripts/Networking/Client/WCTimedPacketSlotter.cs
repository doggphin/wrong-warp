

using System.Collections.Generic;
using UnityEngine;
using Networking.Shared;

namespace Networking.Client {
    public class WCPacketSlots {
        public int tick = -1;
        public bool applied = false;
        public List<WSEntityKillPkt> entityKillPackets = new();
        public List<WSEntitySpawnPkt> entitySpawnPackets = new();
        public List<WSEntityTransformUpdatePkt> entityTransformUpdatePackets = new();

        public void Reset(int tick) {
            this.tick = tick;
            applied = false;
            entityKillPackets.Clear();
            entitySpawnPackets.Clear();
            entityTransformUpdatePackets.Clear();
        }
    }

    public static class WCTimedPacketSlotter {
        private static CircularTickBuffer<WCPacketSlots> slots = new();
        public static void Init() {
            for(int i=0; i<WCommon.TICKS_PER_SECOND; i++) {
                slots[i] = new();
            }
        }
        private static bool CheckSlotsForSlotPacket(int tick) {
            WCPacketSlots slotsToCheck = slots[tick];
            
            if(slotsToCheck.tick > tick || slotsToCheck.applied)
                return false;

            if(tick > slotsToCheck.tick)
                slotsToCheck.Reset(tick);
            
            return true;
        }
        public static void SlotPacket(int tick, WSEntityKillPkt pkt) {
            if(CheckSlotsForSlotPacket(tick))
                slots[tick].entityKillPackets.Add(pkt);
        }
        public static void SlotPacket(int tick, WSEntitySpawnPkt pkt) {
            if(CheckSlotsForSlotPacket(tick))
                slots[tick].entitySpawnPackets.Add(pkt);
        }
        public static void SlotPacket(int tick, WSEntityTransformUpdatePkt pkt) {
            if(CheckSlotsForSlotPacket(tick))
                slots[tick].entityTransformUpdatePackets.Add(pkt);
        }


        public static void ApplySlottedPacketsFromTick(int tick) {
            WCPacketSlots slotsToApply = slots[tick];

            if(slotsToApply.tick != tick) {
                return;
            }

            slotsToApply.entityKillPackets.ForEach((pkt) => ApplyPacket(pkt));
            slotsToApply.entitySpawnPackets.ForEach((pkt) => ApplyPacket(pkt));
            slotsToApply.entityTransformUpdatePackets.ForEach((pkt) => ApplyPacket(tick, pkt));
        }
        private static void ApplyPacket(WSEntityKillPkt pkt) {
            WCEntityManager.KillEntity(pkt);
        }
        private static void ApplyPacket(WSEntitySpawnPkt pkt) {
            WCEntityManager.Spawn(pkt);
        }
        private static void ApplyPacket(int tick, WSEntityTransformUpdatePkt pkt) {
            WCEntityManager.SetEntityTransformForTick(tick, pkt);
        }
    }
}