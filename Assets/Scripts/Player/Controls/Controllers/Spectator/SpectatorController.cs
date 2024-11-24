using System;
using Networking.Shared;
using UnityEngine;

namespace Controllers.Shared {
    public class SpectatorController : NormalRotator, IControllable {
        [SerializeField] private Camera cam;
        private Vector3 velocity = Vector3.zero;
        Vector2 lastReceivedLook = Vector2.zero;
        float speed = 3;

        private WEntityBase entityToTranslate;

        public void Control(WCInputsPkt inputs)
        {
            if(entityToTranslate == null) {
                Debug.LogError("No entity to control!");
                return;
            }

            if(inputs.inputFlags.GetFlag(InputType.Look) && inputs.look.HasValue) {
                lastReceivedLook.x = inputs.look.Value.x;
                lastReceivedLook.y = inputs.look.Value.y;
            }

            float forward = (inputs.inputFlags.GetFlag(InputType.Forward) ? 1f : 0f) - (inputs.inputFlags.GetFlag(InputType.Back) ? 1f : 0f);
            float left = (inputs.inputFlags.GetFlag(InputType.Right) ? 1f : 0f) - (inputs.inputFlags.GetFlag(InputType.Left) ? 1f : 0f);

            Vector3 movementInput = new(left, 0, forward);

            Quaternion rotation = Quaternion.Euler(lastReceivedLook.y, lastReceivedLook.x, 0);

            Vector3 impulse = rotation * movementInput;

            velocity += impulse * speed;
            velocity *= (float)Math.Pow(0.4, Time.fixedDeltaTime);  // Drag
            
            if(entityToTranslate.renderPersonalPositionUpdates) {
                Debug.Log("Changing position directly!");
                transform.position += velocity * Time.fixedDeltaTime;
            } else {
                entityToTranslate.currentPosition += velocity * Time.fixedDeltaTime;
            }              
        }

        public void EnableController()
        {
            Debug.Log("Enabled spectator controller!");
            cam.enabled = true;
            cam.GetComponent<AudioListener>().enabled = true;
            entityToTranslate = GetComponent<WEntityBase>();
        }

        public void DisableController()
        {
            cam.enabled = false;
            cam.GetComponent<AudioListener>().enabled = false;
            entityToTranslate = null;
        }
    }
}
