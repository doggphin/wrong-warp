using UnityEngine;

using Networking.Shared;

namespace Controllers.Client {
    public interface IControllable {
        public void ApplyControls(long inputFlags);
    }

    public interface IRotatable {
        public void AddRotationDelta(Vector2 delta);
        public Vector2? PollRotation();
    }
}