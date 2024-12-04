using UnityEngine;

namespace Networking.Client {
    public class WCPlayerEntity : WCEntity {
        private void Update() {
            float percentageThroughTick = WCNetClient.PercentageThroughTick;

            Debug.Log("bruh");
        }
    }
}