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
            return (float)(sw.ElapsedMilliseconds - msTicked) / WCommon.MS_IN_TICK;
        }


        public void AdvanceTick() {
            msTicked += WCommon.MS_IN_TICK;
        }
    }
}