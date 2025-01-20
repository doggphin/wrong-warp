using System.Collections.Generic;
using Networking.Shared;

namespace Networking.Client {
    public class WCPacketCacheManager : BaseSingleton<WCPacketCacheManager> {
        private class PacketCache {
            public bool applied = false;
            public List<INetPacketForClient> applicablePackets = new();

            public void Reset() {
                applied = false;
                applicablePackets.Clear();
            }
        }

        private TimestampedCircularTickBuffer<PacketCache> caches = new();

        protected override void Awake() {
            for(int i=0; i<WCommon.TICKS_PER_SECOND; i++)
                caches[i] = new();
                
            base.Awake();
        }


        public static bool CachePacket(int tick, INetPacketForClient packet) {
            int cachedTimestamp = Instance.caches.GetTimestamp(tick);
            var cache = Instance.caches[tick];

            // Old packet; toss it out
            if(cachedTimestamp > tick)
                return false;
            
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
                packet.ApplyOnClient(tick);

            cache.applied = true;
        }
    }
}