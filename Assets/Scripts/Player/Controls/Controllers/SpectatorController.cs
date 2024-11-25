using System;
using Networking.Shared;
using UnityEngine;

namespace Controllers.Shared {
    public class SpectatorController : MonoBehaviour, IPlayer {
        [SerializeField] private Camera cam;
        private Vector3 velocity = Vector3.zero;
        Vector2 lastReceivedLook = Vector2.zero;
        float speed = 3;

        private BoundedRotator rotator;
        private WEntityBase entity;

        public void Control(WCInputsPkt inputs)
        {
            if(entity == null) {
                Debug.LogError("No entity to control!");
                return;
            }

            if(inputs.inputFlags.GetFlag(InputType.Look) && inputs.look.HasValue) {
                lastReceivedLook.x = inputs.look.Value.x;
                lastReceivedLook.y = inputs.look.Value.y;
            }

            float forward = (inputs.inputFlags.GetFlag(InputType.Forward) ? 1f : 0f) - (inputs.inputFlags.GetFlag(InputType.Back) ? 1f : 0f);
            float left = (inputs.inputFlags.GetFlag(InputType.Right) ? 1f : 0f) - (inputs.inputFlags.GetFlag(InputType.Left) ? 1f : 0f);
            float up = (inputs.inputFlags.GetFlag(InputType.Jump) ? 1f : 0f) - (inputs.inputFlags.GetFlag(InputType.Crouch) ? 1f : 0f);
            Vector3 movementInput = new(left, up, forward);
            
            int axes = (forward != 0 ? 1 : 0) + (left != 0 ? 1 : 0) + (up != 0 ? 1 : 0);
            if(axes == 2)
                movementInput *= 0.707f;
            else if (axes == 3)
                movementInput *= 0.577f;
            
            // =====

            Quaternion rotation = Quaternion.Euler(lastReceivedLook.y, lastReceivedLook.x, 0);

            Vector3 acceleration = rotation * movementInput * speed;
            velocity += acceleration * Time.deltaTime;
            velocity *= (float)Math.Pow(0.01, Time.fixedDeltaTime);
            
            entity.currentPosition += velocity * Time.fixedDeltaTime;
        }


        public void EnablePlayer()
        {
            Debug.Log("Enabled spectator controller!");
            cam.enabled = true;
            cam.GetComponent<AudioListener>().enabled = true;
            entity = GetComponent<WEntityBase>();
            entity.renderPersonalRotationUpdates = true;
            rotator = new();
        }


        public void DisablePlayer()
        {
            cam.enabled = false;
            cam.GetComponent<AudioListener>().enabled = false;
            entity = null;
        }


        public void AddRotationDelta(Vector2 delta)
        {
            rotator.AddRotationDelta(delta);

            if(entity.renderPersonalRotationUpdates) {
                Debug.Log("Changing rotation directly!");
                transform.rotation = rotator.QuatRotation;
            } 
        }


        public Vector2? PollLook()
        {
            return rotator.PollLook();
        }
    }
}
