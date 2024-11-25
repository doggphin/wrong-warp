using Networking.Shared;
using Unity.VisualScripting;
using UnityEngine;

namespace Controllers.Shared {
    public class BoundedRotator
    {
        private Vector2 oldRotation = Vector3.zero;
        public Vector2 rotation = Vector3.zero;

        public Quaternion QuatRotation => Quaternion.Euler(new Vector3(rotation.y, rotation.x, 0));


        public void AddRotationDelta(Vector2 delta)
        {
            rotation += new Vector2(delta.x, -delta.y);

            rotation.x = ((rotation.x % 360) + 360) % 360;
            rotation.y = Mathf.Clamp(rotation.y, -90, 90);
        }


        public Vector2? PollLook()
        {
            bool hasRotated = oldRotation != rotation;

            if(!hasRotated)
                return null;

            oldRotation = rotation;

            return rotation;
        }
    }
}