using System;
using Networking.Shared;
using UnityEngine;

namespace Controllers.Shared {
    public class SpectatorController : MonoBehaviour, IPlayer {
        [SerializeField] private Camera cam;
        private WEntityBase entity;

        private BoundedRotator boundedRotator;
        private Vector3 velocity = Vector3.zero;
        float speed = 10;
        

        public void RollbackToTick(int tick)
        {
            Debug.LogError("Not implemented");
        }

        public void Control(WInputsSerializable inputs, int onTick)
        {
            if(entity == null) {
                Debug.LogError("No entity to control!");
                return;
            }

            float forward = (inputs.inputFlags.GetFlag(InputType.Forward) ? 1f : 0f) - (inputs.inputFlags.GetFlag(InputType.Back) ? 1f : 0f);
            float right = (inputs.inputFlags.GetFlag(InputType.Right) ? 1f : 0f) - (inputs.inputFlags.GetFlag(InputType.Left) ? 1f : 0f);
            float up = (inputs.inputFlags.GetFlag(InputType.Jump) ? 1f : 0f) - (inputs.inputFlags.GetFlag(InputType.Crouch) ? 1f : 0f);
            Vector3 movementInput = new(right, up, forward);
            
            int axes = (forward != 0 ? 1 : 0) + (right != 0 ? 1 : 0) + (up != 0 ? 1 : 0);
            if(axes == 2)
                movementInput *= 0.707f;
            else if (axes == 3)
                movementInput *= 0.577f;
            
            // =====

            Quaternion rotation = boundedRotator.FullQuatRotation;

            velocity *= (float)Math.Pow(0.5f, WCommon.SECONDS_PER_TICK);

            Vector3 acceleration = rotation * movementInput * speed;
            velocity += acceleration * WCommon.SECONDS_PER_TICK;
            
            entity.positionsBuffer[onTick] += velocity * WCommon.SECONDS_PER_TICK;
        }


        public void ServerInit() {
            entity = GetComponent<WEntityBase>();
        }

        public void EnablePlayer()
        {
            Debug.Log("Enabled spectator controller!");
            cam.enabled = true;
            cam.GetComponent<AudioListener>().enabled = true;
            entity.updateRotationsLocally = true;
            boundedRotator = new();
        }


        public void DisablePlayer()
        {
            cam.enabled = false;
            cam.GetComponent<AudioListener>().enabled = false;
        }


        public void AddRotationDelta(Vector2 delta)
        {
            boundedRotator.AddRotationDelta(delta);

            if(!entity.updateRotationsLocally)
                return;
            
            entity.transform.rotation = boundedRotator.BodyQuatRotation;
            cam.transform.localRotation = boundedRotator.CameraQuatRotation;

            //entity.rotationsBuffer[onTick] = rotator.QuatRotation;
        }


        public Vector2? PollLook()
        {
            return boundedRotator.PollLook();
        }
    }
}
