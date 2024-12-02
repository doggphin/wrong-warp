using System.Diagnostics;

namespace Networking.Shared {
    public class WWatch {
        private Stopwatch sw;
        private long msTicked;

        public void Start() {
            sw = new();
            sw.Start();
            msTicked = 0;
        }

        
        public float GetPercentageThroughTick() {
            return (sw.ElapsedMilliseconds - msTicked) * WCommon.TICKS_PER_MS;
        }


        public void AdvanceTick() {
            msTicked += WCommon.MS_PER_TICK;
        }
    }
}