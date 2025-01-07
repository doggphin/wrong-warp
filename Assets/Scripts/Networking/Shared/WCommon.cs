using UnityEngine;

namespace Networking.Shared
{
    public static class WCommon {
        public static float GetPercentageTimeThroughCurrentTick() => (Time.time % SECONDS_PER_TICK) * TICKS_PER_SECOND;

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
        public T[] buffer = new T[WCommon.TICKS_PER_SECOND];
        public int[] timestamps = new int[WCommon.TICKS_PER_SECOND];

        public T this[int tick] {
            get {
                return buffer[WCommon.GetModuloTPS(tick)];
            }
            set {
                buffer[WCommon.GetModuloTPS(tick)] = value;
            }
        }


        public bool CheckTickIsMoreRecent(int tick, bool returnTrueIfEquals = false) {
            return returnTrueIfEquals ? 
                tick >= timestamps[WCommon.GetModuloTPS(tick)] :
                tick > timestamps[WCommon.GetModuloTPS(tick)];
        }


        public void SetValueAndTimestamp(T value, int tickTimestamp) {
            buffer[WCommon.GetModuloTPS(tickTimestamp)] = value;
            timestamps[WCommon.GetModuloTPS(tickTimestamp)] = tickTimestamp;
        }


        public void SetTimestamp(int tick) {
            timestamps[WCommon.GetModuloTPS(tick)] = tick;
        }


        public int GetTimestamp(int tick) {
            return timestamps[WCommon.GetModuloTPS(tick)];
        }
        

        public bool TryGetByTimestamp(int timestamp, out T value) {
            if(timestamps[WCommon.GetModuloTPS(timestamp)] == timestamp) {
                value = buffer[WCommon.GetModuloTPS(timestamp)];
                return true;
            } else {
                value = default;
                return false;
            }
        }

        public TimestampedCircularTickBuffer(T defaultValue, int initialTimestamp) {
            for(int i=0; i<WCommon.TICKS_PER_SECOND; i++) {
                buffer[i] = defaultValue;
                timestamps[i] = initialTimestamp;
            }
        }
        public TimestampedCircularTickBuffer() {}
    }
}
