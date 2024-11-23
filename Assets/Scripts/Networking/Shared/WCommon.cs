using Networking;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace Networking.Shared
{
    public static class WCommon {
        public static float GetPercentageTimeThroughCurrentTick() {
            return (Time.time % Time.fixedDeltaTime) / Time.fixedDeltaTime;
        }

        public const int TICKS_PER_SNAPSHOT = 5;
        public const ushort WRONGWARP_PORT = 1972;
    }
}
