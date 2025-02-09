using LiteNetLib.Utils;
using Networking.Shared;
using UnityEngine;

namespace Controllers.Shared {
    public abstract class AbstractPlayer : MonoBehaviour
    {
        public abstract void EnablePlayer();
        public abstract void DisablePlayer();

        public abstract void Control(InputsSerializable inputs, int onTick);

        public abstract void AddRotationDelta(Vector2 delta);
        public abstract Vector2? PollLook();
        public abstract Vector2 GetLook();

        public abstract void SetRotation(Vector2 look);
        public abstract Vector2 GetRotation();

        public abstract void RollbackToTick(int tick);
        public abstract bool PollForInteractable(out BaseInteractable outInteractable);
        public abstract void TryInteract();

        protected bool PollForInteractable(Transform camTransform, float range, out BaseInteractable baseInteractable) {
            if(Physics.Raycast(camTransform.position, camTransform.rotation * Vector3.forward, out var hit, range)) {
                return hit.collider.gameObject.TryGetComponent(out baseInteractable);
            }
            baseInteractable = null;
            return false;
        }
    }
}
