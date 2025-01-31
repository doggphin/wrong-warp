using System.Collections.Generic;
using Networking.Shared;
using UnityEngine;

namespace Networking.Shared {
    public class PacketCacheManager : BaseSingleton<PacketCacheManager> {
        private class PacketCache {
            public bool applied = false;
            public List<BasePacket> applicablePackets = new();

            public void Reset() {
                applied = false;
                applicablePackets.Clear();
            }
        }

        private TimestampedCircularTickBuffer<PacketCache> caches = TimestampedCircularTickBufferClassInitializer<PacketCache>.GetInitialized(0);

        public static bool CachePacket(int tick, BasePacket packet) {
            int cachedTimestamp = Instance.caches.GetTimestamp(tick);
            var cache = Instance.caches[tick];

            // Toss out old packets unless the packet should run reliably
            if(cachedTimestamp > tick) {
                if(packet.ShouldRunEvenIfLate) {
                    packet.BroadcastApply(tick);
                    return true;
                } else {
                    return false;
                }
            }
            
            // First packet of new tick; clean out this tick
            if(tick > cachedTimestamp) {
                cache.Reset();
                Instance.caches.SetTimestamp(tick);
            }
                
            cache.applicablePackets.Add(packet);
            return true;
        }


        public static void ApplyTick(int tick) {
            // Don't run cache if it isn't up to date or has already been applied
            if(!Instance.caches.TryGetByTimestamp(tick, out PacketCache cache) || cache.applied)
                return;

            foreach(var packet in cache.applicablePackets)
                packet.BroadcastApply(tick);

            cache.applied = true;
        }
    }
}