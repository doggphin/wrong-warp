using Networking.Shared;
using UnityEngine;

namespace Controllers.Shared {
    public class BaseController : MonoBehaviour {
        [SerializeField] protected Camera cam;
        protected WEntityBase entity;
        protected BoundedRotator boundedRotator;

        public void SetRotation(Vector2 look)
        {
            boundedRotator.rotation = look;
        }


        public Vector2 GetRotation() => boundedRotator.rotation;


        public virtual void EnablePlayer() {
            boundedRotator = new();
            cam.enabled = true;
            cam.GetComponent<AudioListener>().enabled = true;

            entity = GetComponent<WEntityBase>();

            entity.updateRotationsLocally = true;
            entity.updatePositionsLocally = true;
        }


        public virtual void DisablePlayer() {
            cam.enabled = false;
            cam.gameObject.GetComponent<AudioListener>().enabled = false;

            entity.updateRotationsLocally = false;
            entity.updatePositionsLocally = false;
        }


        public virtual void AddRotationDelta(Vector2 delta) {
            boundedRotator.AddRotationDelta(delta);
            entity.transform.rotation = boundedRotator.BodyQuatRotation;
            cam.transform.localRotation = boundedRotator.CameraQuatRotation;
        }


        public Vector2? PollLook()
        {
            return boundedRotator.PollLook();
        }
    }
}