using Networking;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace Networking.Shared
{
    public static class WCommon {
        public static float GetPercentageTimeThroughCurrentTick() => (Time.time % SECONDS_PER_TICK) * TICKS_PER_SECOND;

        public const int TICKS_PER_SNAPSHOT = 2;
        public const int TICKS_PER_SECOND = 20;
        public const int MAX_PING_MS = 300;
        public const float SECONDS_PER_TICK = 1f / TICKS_PER_SECOND;
        public const int MS_IN_TICK = (int)((1f / TICKS_PER_SECOND) * 1000f);
        public const ushort WRONGWARP_PORT = 1972;

        public static int GetModuloSnapshotLength(int tick) => tick % TICKS_PER_SNAPSHOT;
        public static int GetModuloTPS(int tick) => tick % TICKS_PER_SECOND;
    }
}
