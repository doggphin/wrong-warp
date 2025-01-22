using System.Diagnostics;

namespace Networking.Shared {
    public class WWatch {
        private Stopwatch sw;
        private long msTicked;

        public WWatch() {
            sw = new();
            sw.Start();
            msTicked = 0;
        }

        
        public float GetPercentageThroughTick() {
            return (sw.ElapsedMilliseconds - msTicked) * NetCommon.TICKS_PER_MS;
        }


        public void AdvanceTick() {
            msTicked += NetCommon.MS_PER_TICK;
        }
    }
}