using System;
using Networking.Shared;
using UnityEngine;

namespace Controllers.Shared {
    public class SpectatorController : BaseController {

        private Vector3 velocity = Vector3.zero;
        float speed = 10;
        
        
        void Awake() {
            entity = GetComponent<BaseEntity>();
        }

        public override void RollbackToTick(int tick)
        {
            Debug.LogError("Not implemented");
        }

        public override void Control(InputsSerializable inputs, int onTick)
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

            velocity *= (float)Math.Pow(0.5f, NetCommon.SECONDS_PER_TICK);

            Vector3 acceleration = rotation * movementInput * speed;
            velocity += acceleration * NetCommon.SECONDS_PER_TICK;
            
            entity.positionsBuffer[onTick] += velocity * NetCommon.SECONDS_PER_TICK;
        }
    }
}
