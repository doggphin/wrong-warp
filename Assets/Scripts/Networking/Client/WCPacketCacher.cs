using System.Collections.Generic;
using UnityEngine;
using Networking.Shared;

namespace Networking.Client {
    public static class WCPacketCacher {
        public class PacketCache {
            public bool applied = false;
            public List<IClientApplicablePacket> applicablePackets = new();

            public void Reset() {
                applied = false;
                applicablePackets.Clear();
            }
        }

        private static TimestampedCircularTickBuffer<PacketCache> caches = new();

        public static void Init() {
            for(int i=0; i<WCommon.TICKS_PER_SECOND; i++)
                caches[i] = new();
        }


        public static bool CachePacket(int tick, IClientApplicablePacket packet) {
            int cachedTimestamp = caches.GetTimestamp(tick);
            var cache = caches[tick];

            // Old packet; toss it out
            if(cachedTimestamp > tick)
                return false;
            
            // First packet of new tick; clean out this tick
            if(tick > cachedTimestamp) {
                cache.Reset();
                caches.SetTimestamp(tick);
            }
                
            cache.applicablePackets.Add(packet);
            return true;
        }


        public static void ApplyTick(int tick) {
            // Don't run cache if it isn't up to date or has already been applied
            if(!caches.TryGetByTimestamp(tick, out PacketCache cache) || cache.applied)
                return;

            foreach(var packet in cache.applicablePackets)
                packet.ApplyOnClient(tick);

            cache.applied = true;
        }
    }
}