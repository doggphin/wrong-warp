using UnityEngine;

using Networking.Shared;

namespace Controllers.Shared {
    public interface IControllable {
        public void Control(WCInputsPkt inputs);
        public void EnableController();
        public void DisableController();
    }
}