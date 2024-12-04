using System.Data.Common;
using LiteNetLib.Utils;
using Networking.Shared;
using UnityEngine;

namespace Controllers.Shared {
    public class DefaultController : MonoBehaviour, IPlayer {
        const float groundedMaxSpeed = 7f;
        const float aerialMaxSpeed = 5f;
        const float groundedAcceleration = 55f;
        const float aerialAcceleration = 10f;
        const float gravity = 18f;
        const float groundedJumpForce = 5.5f;
        const float aerialJumpForce = 6.5f;

        [SerializeField] private Camera cam;
        WEntityBase entity;
        private CharacterController characterController;
        public void InitAsControllable() {
            boundedRotator ??= new();
            characterController = GetComponent<CharacterController>();
            entity = GetComponent<WEntityBase>();
        }

        private BoundedRotator boundedRotator;
        private Vector3 velocity = Vector3.zero;
        private bool canDoubleJump = false;
        private WInputsSerializable previousInputs = new();
        public void RollbackToTick(int tick)
        {
            Debug.Log($"Trying to roll back to {tick}!");
            var state = WCRollbackManager.defaultControllerStates[tick];

            velocity = state.velocity;
            canDoubleJump = state.canDoubleJump;
            previousInputs = state.previousInputs;
            boundedRotator.rotation = state.boundedRotatorRotation;
            entity.positionsBuffer[tick] = state.position;
        }
        public WSDefaultControllerStatePkt GetSerializableState() {
            return new WSDefaultControllerStatePkt() {
                velocity = velocity,
                canDoubleJump = canDoubleJump,
                previousInputs = previousInputs,
                boundedRotatorRotation = boundedRotator.rotation
            };
        }

        public void Control(WInputsSerializable inputs, int onTick) {
            if(entity == null)
                return;

            if(!entity.updateRotationsLocally && inputs.inputFlags.GetFlag(InputType.Look))
                boundedRotator.rotation = inputs.look.Value;

            //entity.transform.rotation = boundedRotator.BodyQuatRotation;
            //cam.transform.localRotation = boundedRotator.CameraQuatRotation;
            Quaternion rotation = boundedRotator.BodyQuatRotation;

            float forward = (inputs.inputFlags.GetFlag(InputType.Forward) ? 1f : 0f) - (inputs.inputFlags.GetFlag(InputType.Back) ? 1f : 0f);
            float right = (inputs.inputFlags.GetFlag(InputType.Right) ? 1f : 0f) - (inputs.inputFlags.GetFlag(InputType.Left) ? 1f : 0f);

            Vector3 wasdInput = new(right, 0, forward);
            if(forward != 0 && right != 0)
                wasdInput *= .7071f;

            // ===========

            bool isGrounded = Physics.Raycast(entity.positionsBuffer[onTick], Vector3.down, 1.1f);
            
            // Set constants dependent on being grounded or not
            float gravityAcceleration = isGrounded ? 0 : -gravity * WCommon.SECONDS_PER_TICK;
            float drag = isGrounded ? Mathf.Pow(0.00002f, WCommon.SECONDS_PER_TICK) : Mathf.Pow(0.5f, WCommon.SECONDS_PER_TICK);
            float movementAccelerationFactor = isGrounded ? groundedAcceleration : aerialAcceleration;
            float maxSpeed = isGrounded ? groundedMaxSpeed : aerialMaxSpeed;

            velocity *= drag;

            Vector2 xzVelocity = new Vector2(velocity.x, velocity.z);
            float xzSpeed = xzVelocity.magnitude;

            Vector3 movement = rotation * (wasdInput * movementAccelerationFactor * WCommon.SECONDS_PER_TICK);
            Vector2 xzMovement = new Vector2(movement.x, movement.z);
            Vector2 xzVelocityAfterMovement = xzVelocity + xzMovement;
            float xzSpeedAfterMovement = xzVelocityAfterMovement.magnitude;

            bool isMovingBelowMaxSpeed = xzSpeed <= maxSpeed;

            Vector2 xzVelocityToUse;
            if(isMovingBelowMaxSpeed) {
                // If trying to move faster than maxWalkingSpeed, cap it
                if(xzSpeedAfterMovement > maxSpeed) {
                    xzVelocityToUse = maxSpeed * 0.99f * (xzVelocityAfterMovement / xzSpeedAfterMovement);
                }
                // If walking within maxWalkingSpeed, allow it
                else {
                    xzVelocityToUse = xzVelocityAfterMovement;
                }
            } else {
                // Allow the player to influence movement, but not accelerate faster than their current speed
                xzVelocityToUse = xzSpeed * (xzVelocityAfterMovement / xzSpeedAfterMovement);
            }

            velocity = new Vector3(xzVelocityToUse.x, velocity.y + gravityAcceleration, xzVelocityToUse.y);

            if(isGrounded)
                canDoubleJump = true;

            if(inputs.inputFlags.GetFlag(InputType.Jump) && !previousInputs.inputFlags.GetFlag(InputType.Jump)) {
                if(isGrounded) {
                    velocity.y = groundedJumpForce;
                    isGrounded = false;
                } else if (canDoubleJump) {
                    velocity.y = Mathf.Max(aerialJumpForce, velocity.y + aerialJumpForce);
                    
                    // If moving at normal movespeed, or double jumping away from current speeding velocity direction, apply burst speed
                    if(isMovingBelowMaxSpeed || Vector3.Dot(movement, new Vector3(velocity.x, 0, velocity.z)) < 0) {
                        velocity += new Vector3(xzMovement.x, 0, xzMovement.y) * 6.5f;
                    }
                    canDoubleJump = false;
                }
            }

            // Calculate distance cc wants to move
            transform.position = entity.positionsBuffer[onTick];
            characterController.Move(velocity * WCommon.SECONDS_PER_TICK);
            Vector3 difference = transform.position - entity.positionsBuffer[onTick];

            entity.positionsBuffer[onTick] += difference;
            // This is for client-ended units - probably remove 12/02/24
            entity.positionsBuffer[onTick + 1] = entity.positionsBuffer[onTick];
            transform.position = entity.positionsBuffer[onTick];        // consider deleting this

            // Visual position will be set before next frame is shown, not important to set here
            previousInputs = inputs;

            if(WNetManager.IsClient) {
                var state = GetSerializableState();
                WCRollbackManager.defaultControllerStates[onTick] = state;
            }
                
        }


        public void AddRotationDelta(Vector2 delta) {
            boundedRotator.AddRotationDelta(new Vector2(delta.x, delta.y));

            if(!entity.updateRotationsLocally)
                return;
            
            entity.transform.rotation = boundedRotator.BodyQuatRotation;
            cam.transform.localRotation = boundedRotator.CameraQuatRotation;
        }


        public void EnablePlayer()
        {
            boundedRotator = new();
            cam.enabled = true;
            cam.gameObject.GetComponent<AudioListener>().enabled = true;
            entity ??= GetComponent<WEntityBase>();
            entity.updateRotationsLocally = true;
            entity.updatePositionsLocally = true;
        }


        public void DisablePlayer()
        {
            cam.enabled = false;
            cam.gameObject.GetComponent<AudioListener>().enabled = false;
            entity.updateRotationsLocally = false;
            entity.updatePositionsLocally = false;
        }


        public Vector2? PollLook()
        {
            return boundedRotator.PollLook();
        }
    }
}
