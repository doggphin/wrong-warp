
using UnityEngine;

namespace Networking.Shared {
    public abstract class WEntityBase : MonoBehaviour {
        public int Id { get; protected set; }
        public WPrefabId PrefabId { get; protected set; }
        protected bool isDead;

        public bool renderPersonalPositionUpdates, renderPersonalRotationUpdates, renderPersonalScaleUpdates;

        protected Vector3 previousPosition = Vector3.zero;
        protected Quaternion previousRotation = Quaternion.identity;
        protected Vector3 previousScale = Vector3.one;

        [HideInInspector] public Vector3 currentPosition = Vector3.zero;
        [HideInInspector] public Quaternion currentRotation = Quaternion.identity;
        [HideInInspector] public Vector3 currentScale = Vector3.one;

        public bool HasMoved => currentPosition != previousPosition;
        public bool HasRotated => currentRotation != previousRotation;
        public bool HasScaled => currentRotation != previousRotation;

        public abstract void Kill(WEntityKillReason reason);
    }
}