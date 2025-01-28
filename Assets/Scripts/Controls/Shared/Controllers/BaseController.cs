using Networking.Shared;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace Controllers.Shared {
    public abstract class BaseController : AbstractPlayer {
        private static int layerMask;

        [SerializeField] protected Camera cam;
        protected BaseEntity entity;
        
        protected BoundedRotator boundedRotator;

        void Awake() {
            layerMask = LayerMask.GetMask("World", "Interactable");
        }

        public override void SetRotation(Vector2 look)
        {
            boundedRotator.rotation = look;
        }

        public override Vector2 GetRotation() => boundedRotator.rotation;


        public override void EnablePlayer() {
            boundedRotator = new();
            cam.enabled = true;
            cam.gameObject.tag = "MainCamera";
            cam.GetComponent<AudioListener>().enabled = true;

            entity = GetComponent<BaseEntity>();

            //entity.setVisualRotationAutomatically = false;
            //entity.setVisualPositionAutomatically = false;
        }


        public override void DisablePlayer() {
            cam.enabled = false;
            cam.gameObject.tag = "Untagged";
            cam.gameObject.GetComponent<AudioListener>().enabled = false;

            //entity.setVisualRotationAutomatically = true;
            //entity.setVisualPositionAutomatically = true;
        }


        public override void AddRotationDelta(Vector2 delta) {
            boundedRotator.AddRotationDelta(delta);
            entity.transform.rotation = boundedRotator.BodyQuatRotation;
            cam.transform.localRotation = boundedRotator.CameraQuatRotation;
        }

        protected virtual float InteractRange => 5.0f;
        public override bool PollForInteractable(out BaseInteractable outInteractable) {
            outInteractable = null;
            return false;
            /*Debug.Log("Polling!");
            Debug.DrawLine(cam.transform.position, cam.transform.position + cam.transform.rotation * Vector3.forward * InteractRange, Color.red);
            Debug.Log(InteractRange);
            if(!Physics.Raycast(cam.transform.position, cam.transform.rotation * Vector3.forward, out var hit, InteractRange)) {
                Debug.Log("Didn't find shit!");
                outInteractable = null;
                return false;
            }

            outInteractable*/

            //return hit.collider.TryGetComponent(out outInteractable);
        }

        public override Vector2? PollLook() => boundedRotator.PollLook();


        public override Vector2 GetLook() => boundedRotator.GetLook();

        // These need to be implemented by a higher level class
        public override void Control(InputsSerializable inputs, int onTick)
        {
            throw new System.NotImplementedException();
        }
        public override void RollbackToTick(int tick)
        {
            throw new System.NotImplementedException();
        }
    }
}