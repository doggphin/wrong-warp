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
        }


        public override void DisablePlayer() {
            cam.enabled = false;
            cam.gameObject.tag = "Untagged";
            cam.gameObject.GetComponent<AudioListener>().enabled = false;
        }


        public override void AddRotationDelta(Vector2 delta) {
            boundedRotator.AddRotationDelta(delta);
            entity.transform.rotation = boundedRotator.BodyQuatRotation;
            cam.transform.localRotation = boundedRotator.CameraQuatRotation;
        }

        protected virtual float InteractRange => 5.0f;
        public override bool PollForInteractable(out BaseInteractable outInteractable) =>
            PollForInteractable(cam.transform, InteractRange, out outInteractable);
        public override void TryInteract() {
            if(PollForInteractable(out var interactable) && TryGetComponent(out BaseEntity baseEntity)) {
                interactable.InteractStart(baseEntity);
            }
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