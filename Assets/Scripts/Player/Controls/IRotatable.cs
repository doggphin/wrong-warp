using UnityEngine;

namespace Controllers.Shared {
    public interface IRotatable {
        public void AddRotationDelta(Vector2 delta);
        public Vector2? PollRotation();
        public void EnableRotator();
        public void DisableRotator();
    }
}