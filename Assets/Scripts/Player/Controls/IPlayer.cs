using Networking.Shared;
using UnityEngine;

namespace Controllers.Shared {
    public interface IPlayer
    {
        public void EnablePlayer();
        public void DisablePlayer();

        public void Control(WCInputsPkt inputs);
        public void AddRotationDelta(Vector2 delta);
        public Vector2? PollLook();
    }
}
