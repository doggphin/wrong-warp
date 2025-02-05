using UnityEngine;

namespace Networking.Shared
{
    public static class NetCommon {
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
        public static bool IsTickOld(int tick, int pastTick) => tick - pastTick > NetCommon.TICKS_PER_SECOND - 1;
    }

    public struct WDisconnectInfo {
        public string reason;
        public bool wasExpected;
    }


    public class CircularTickBuffer<T> {
        public T[] buffer = new T[NetCommon.TICKS_PER_SECOND];

        public T this[int tick] {
            get {
                return Get(tick);
            }
            set {
                Set(tick, value);
            }
        }

        public void Set(int tick, T value) {
            buffer[NetCommon.GetModuloTPS(tick)] = value;
        }


        public T Get(int tick) {
            return buffer[NetCommon.GetModuloTPS(tick)];
        }
    }


    public class TimestampedCircularTickBuffer<T> {
        private struct TimestampedCircularTickBufferItem {
            public T item;
            public int timestamp;
        }

        private TimestampedCircularTickBufferItem[] timestampedItems = new TimestampedCircularTickBufferItem[NetCommon.TICKS_PER_SECOND];
        // public T[] buffer = new T[WCommon.TICKS_PER_SECOND];
        // public int[] timestamps = new int[WCommon.TICKS_PER_SECOND];

        public T this[int tick] {
            get {
                return timestampedItems[NetCommon.GetModuloTPS(tick)].item;
            }
            set {
                timestampedItems[NetCommon.GetModuloTPS(tick)].item = value;
            }
        }

        public bool SetValueAndTimestampIfMoreRecent(int tick, T value, bool overwriteIfSame = false) {
            int currentTick = GetTimestamp(tick);
            if(currentTick < tick || (overwriteIfSame && currentTick == tick)) {
                SetValueAndTimestamp(value, tick);
                return true;
            }
            return false;
        }

        public bool IsInputTickNewer(int tick, bool returnTrueIfEquals = false) {
            return returnTrueIfEquals ? 
                tick >= timestampedItems[NetCommon.GetModuloTPS(tick)].timestamp :
                tick > timestampedItems[NetCommon.GetModuloTPS(tick)].timestamp;
        }


        public void SetValueAndTimestamp(T value, int tickTimestamp) {
            timestampedItems[NetCommon.GetModuloTPS(tickTimestamp)] = new() {
                item = value,
                timestamp = tickTimestamp
            };
        }


        public void SetTimestamp(int tick) {
            timestampedItems[NetCommon.GetModuloTPS(tick)].timestamp = tick;
        }


        public int GetTimestamp(int tick) {
            return timestampedItems[NetCommon.GetModuloTPS(tick)].timestamp;
        }
        

        public bool TryGetByTimestamp(int timestamp, out T value) {
            if(timestampedItems[NetCommon.GetModuloTPS(timestamp)].timestamp == timestamp) {
                value = timestampedItems[NetCommon.GetModuloTPS(timestamp)].item;
                return true;
            } else {
                value = default;
                return false;
            }
        }
        
        public TimestampedCircularTickBuffer() {
            for(int i=0; i<NetCommon.TICKS_PER_SECOND; i++) {
                timestampedItems[i].timestamp = -1;
            }
        }
    }

    ///<summary> Used to generate initialized TimestampedCircularTickBuffers, where T is a class </summary>
    public static class TimestampedCircularTickBufferClassInitializer<T> where T : class, new() {
        public static TimestampedCircularTickBuffer<T> Initialize() {
            TimestampedCircularTickBuffer<T> ret = new();
            for(int i=0; i<NetCommon.TICKS_PER_SECOND; i++) {
                ret[i] = new();
            }
            return ret;
        }
    }
}
