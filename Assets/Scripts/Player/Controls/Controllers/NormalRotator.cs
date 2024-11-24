using Networking.Shared;
using Unity.VisualScripting;
using UnityEngine;

namespace Controllers.Shared {
    public class NormalRotator : MonoBehaviour, IRotatable
    {
        private Vector2 oldRotation = Vector3.zero;
        private Vector2 rotation = Vector3.zero;

        protected WEntityBase entityToRotate;

        public void AddRotationDelta(Vector2 delta)
        {
            rotation += new Vector2(delta.x, -delta.y);

            rotation.x = ((rotation.x % 360) + 360) % 360;
            rotation.y = Mathf.Clamp(rotation.y, -90, 90);

            Quaternion quat = Quaternion.Euler(new Vector3(rotation.y, rotation.x, 0));

            if(entityToRotate == null)
                return;
            
            if(entityToRotate.renderPersonalRotationUpdates)
                transform.rotation = quat;
            
            if(WNetManager.IsServer)
                entityToRotate.currentRotation = quat;
        }


        public Vector2? PollRotation()
        {
            bool hasRotated = oldRotation != rotation;

            if(!hasRotated)
                return null;

            oldRotation = rotation;

            return rotation;
        }

        public void EnableRotator() {
            Debug.Log("Enabled normal rotator!");
            entityToRotate = GetComponent<WEntityBase>();
        }

        public void DisableRotator() {
            entityToRotate = null;
        }
    }
}