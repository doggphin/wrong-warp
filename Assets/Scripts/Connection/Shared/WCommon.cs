using UnityEngine;

namespace Networking.Shared
{
    public static class WCommon {
        public static float GetPercentageTimeThroughCurrentTick() => (Time.time % SECONDS_PER_TICK) * TICKS_PER_SECOND;

        public const string CONNECTION_KEY = "WW 0.01";
        public const int TIMEOUT_MS = 5000;
        public const int TICKS_PER_SNAPSHOT = 2;
        public const int TICKS_PER_SECOND = 20;
        public const int MAX_PING_MS = 300;
        public const float SECONDS_PER_TICK = 1f / TICKS_PER_SECOND;
        public const int MS_PER_TICK = (int)(1000 * (1f / TICKS_PER_SECOND));
        public const float TICKS_PER_MS = 1f / MS_PER_TICK;
        public const ushort WRONGWARP_PORT = 1972;

        public static int GetModuloSnapshotLength(int tick) => tick % TICKS_PER_SNAPSHOT;
        public static int GetModuloTPS(int tick) => tick % TICKS_PER_SECOND;

        // I have no idea if this -1 is necessary
        public static bool IsTickOld(int tick, int pastTick) => tick - pastTick > WCommon.TICKS_PER_SECOND - 1;
    }

    public struct WDisconnectInfo {
        public string reason;
        public bool wasExpected;
    }


    public class CircularTickBuffer<T> {
        public T[] buffer = new T[WCommon.TICKS_PER_SECOND];

        public T this[int tick] {
            get {
                return Get(tick);
            }
            set {
                Set(tick, value);
            }
        }

        public void Set(int tick, T value) {
            buffer[WCommon.GetModuloTPS(tick)] = value;
        }


        public T Get(int tick) {
            return buffer[WCommon.GetModuloTPS(tick)];
        }
    }


    public class TimestampedCircularTickBuffer<T> {
        private struct TimestampedCircularTickBufferItem {
            public T item;
            public int timestamp;
        }

        private TimestampedCircularTickBufferItem[] timestampedItems = new TimestampedCircularTickBufferItem[WCommon.TICKS_PER_SECOND];
        // public T[] buffer = new T[WCommon.TICKS_PER_SECOND];
        // public int[] timestamps = new int[WCommon.TICKS_PER_SECOND];

        public T this[int tick] {
            get {
                return timestampedItems[WCommon.GetModuloTPS(tick)].item;
            }
            set {
                timestampedItems[WCommon.GetModuloTPS(tick)].item = value;
            }
        }


        public bool CheckTickIsMoreRecent(int tick, bool returnTrueIfEquals = false) {
            return returnTrueIfEquals ? 
                tick >= timestampedItems[WCommon.GetModuloTPS(tick)].timestamp :
                tick > timestampedItems[WCommon.GetModuloTPS(tick)].timestamp;
        }


        public void SetValueAndTimestamp(T value, int tickTimestamp) {
            timestampedItems[WCommon.GetModuloTPS(tickTimestamp)] = new() {
                item = value,
                timestamp = tickTimestamp
            };
        }


        public void SetTimestamp(int tick) {
            timestampedItems[WCommon.GetModuloTPS(tick)].timestamp = tick;
        }


        public int GetTimestamp(int tick) {
            return timestampedItems[WCommon.GetModuloTPS(tick)].timestamp;
        }
        

        public bool TryGetByTimestamp(int timestamp, out T value) {
            if(timestampedItems[WCommon.GetModuloTPS(timestamp)].timestamp == timestamp) {
                value = timestampedItems[WCommon.GetModuloTPS(timestamp)].item;
                return true;
            } else {
                value = default;
                return false;
            }
        }
        
        public TimestampedCircularTickBuffer(int initialTimestamp) {
            for(int i=0; i<WCommon.TICKS_PER_SECOND; i++) {
                timestampedItems[i].timestamp = initialTimestamp;
            }
        }
        public TimestampedCircularTickBuffer() {}
    }

    ///<summary> Used to generate initialized TimestampedCircularTickBuffers, where T is a class </summary>
    public static class TimestampedCircularTickBufferClassInitializer<T> where T : class, new() {
        public static TimestampedCircularTickBuffer<T> GetInitialized(int initialTick = -1) {
            TimestampedCircularTickBuffer<T> ret = new(initialTick);
            for(int i=0; i<WCommon.TICKS_PER_SECOND; i++) {
                ret[i] = new();
            }
            return ret;
        }
    }
}
