using Networking.Shared;
using UnityEngine;

namespace Controllers.Shared {
    [RequireComponent(typeof(CharacterController))]
    public class DefaultController : BaseController {
        const float groundedMaxSpeed = 7f;
        const float aerialMaxSpeed = 5f;
        const float groundedAcceleration = 55f;
        const float aerialAcceleration = 10f;
        const float gravity = 18f;
        const float groundedJumpForce = 5.5f;
        const float aerialJumpForce = 6.5f;

        private CharacterController characterController;

        private Vector3 velocity = Vector3.zero;
        private bool canDoubleJump = false;
        private InputsSerializable previousInputs = new();

        void Awake() {
            boundedRotator = new();
            characterController = GetComponent<CharacterController>();
        }

        void Start() {
            entity = GetComponent<BaseEntity>();
        }

        public override void RollbackToTick(int tick)
        {
            var state = CRollbackManager.GetDefaultControllerState(tick);

            velocity = state.velocity;
            canDoubleJump = state.canDoubleJump;
            previousInputs = state.previousInputs;
            boundedRotator.rotation = state.boundedRotatorRotation;

            // TODO: remove if working
            //entity.positionsBuffer[tick] = state.position;
            //entity.positionsBuffer[tick + 1] = state.position;
            entity.SetPosition(state.position, true, tick);
        }
        public SDefaultControllerStatePkt GetSerializableState(int tick) {
            return new SDefaultControllerStatePkt() {
                velocity = velocity,
                canDoubleJump = canDoubleJump,
                previousInputs = previousInputs,
                boundedRotatorRotation = boundedRotator.rotation,
                // This throws an exception because "entity" field won't be initialized before this is called, can't be put in Awake since won't exist yet
                position = entity.positionsBuffer[tick]
            };
        }

        public override void Control(InputsSerializable inputs, int onTick) {
            if(entity == null)
                return;

            if(inputs.inputFlags.GetFlag(InputType.Look))
                boundedRotator.rotation = inputs.look.Value;

            Quaternion rotation = boundedRotator.BodyQuatRotation;

            float forward = (inputs.inputFlags.GetFlag(InputType.Forward) ? 1f : 0f) - (inputs.inputFlags.GetFlag(InputType.Back) ? 1f : 0f);
            float right = (inputs.inputFlags.GetFlag(InputType.Right) ? 1f : 0f) - (inputs.inputFlags.GetFlag(InputType.Left) ? 1f : 0f);

            Vector3 wasdInput = new(right, 0, forward);
            if(forward != 0 && right != 0)
                wasdInput *= .7071f;

            // ===========

            bool isGrounded = Physics.Raycast(entity.positionsBuffer[onTick], Vector3.down, 1.1f, LayerMask.GetMask("World"));
            
            // Set constants dependent on being grounded or not
            float gravityAcceleration = isGrounded ? 0 : -gravity * NetCommon.SECONDS_PER_TICK;
            float drag = isGrounded ? Mathf.Pow(0.00002f, NetCommon.SECONDS_PER_TICK) : Mathf.Pow(0.5f, NetCommon.SECONDS_PER_TICK);
            float movementAccelerationFactor = isGrounded ? groundedAcceleration : aerialAcceleration;
            float maxSpeed = isGrounded ? groundedMaxSpeed : aerialMaxSpeed;

            velocity *= drag;

            Vector2 xzVelocity = new(velocity.x, velocity.z);
            float xzSpeed = xzVelocity.magnitude;

            Vector3 movement = rotation * (movementAccelerationFactor * NetCommon.SECONDS_PER_TICK * wasdInput);
            Vector2 xzMovement = new(movement.x, movement.z);
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
            characterController.enabled = false;
            transform.position = entity.positionsBuffer[onTick];
            characterController.enabled = true;

            characterController.Move(velocity * NetCommon.SECONDS_PER_TICK);

            Vector3 difference = transform.position - entity.positionsBuffer[onTick];
            
            //TODO: remove if working
            //entity.positionsBuffer[onTick] += difference;
            //entity.positionsBuffer[onTick + 1] = entity.positionsBuffer[onTick];
            //entity.rotationsBuffer[onTick] = boundedRotator.BodyQuatRotation;
            entity.SetPosition(entity.GetPosition(onTick) + difference, true, onTick);
            entity.SetRotation(boundedRotator.BodyQuatRotation, false, onTick);
            
            characterController.enabled = false;
            transform.position = entity.GetPosition(onTick);        // consider deleting this
            characterController.enabled = true;

            // Visual position will be set before next frame is shown, not important to set here
            previousInputs = inputs;

            if(WWNetManager.IsClient) {
                var state = GetSerializableState(onTick);
                CRollbackManager.SetDefaultControllerState(onTick, state);
            }         
        } 
    }
}
