using Networking;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using UnityEngine;

namespace Networking.Shared
{
    public static class WCommon {
        public static float GetPercentageTimeThroughCurrentTick() => (Time.time % SECONDS_PER_TICK) * TICKS_PER_SECOND;

        public const int TICKS_PER_SNAPSHOT = 2;
        public const int TICKS_PER_SECOND = 15;
        public const int MAX_PING_MS = 300;
        public const float SECONDS_PER_TICK = 1f / TICKS_PER_SECOND;
        public const int MS_PER_TICK = (int)(1000 * (1f / TICKS_PER_SECOND));
        public const float TICKS_PER_MS = 1f / MS_PER_TICK;
        public const ushort WRONGWARP_PORT = 1972;

        public static int GetModuloSnapshotLength(int tick) => tick % TICKS_PER_SNAPSHOT;
        public static int GetModuloTPS(int tick) => tick % TICKS_PER_SECOND;
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


        public void SetValueAndTimestamp(T value, int tick) {
            buffer[WCommon.GetModuloTPS(tick)] = value;
            timestamps[WCommon.GetModuloTPS(tick)] = tick;
        }

        public void SetTimestamp(int tick) {
            timestamps[WCommon.GetModuloTPS(tick)] = tick;
        }
    }
}
