using System.Data.Common;
using Networking.Shared;
using UnityEngine;

namespace Controllers.Shared {
public class DefaultController : MonoBehaviour, IPlayer
    {
        [SerializeField] private Camera cam;

        private Vector3 velocity = Vector3.zero;
        Vector2 lastReceivedLook = Vector2.zero;

        private CharacterController characterController;
        private BoundedRotator boundedRotator;

        WEntityBase entity;

        const float groundedMaxSpeed = 7f;
        const float aerialMaxSpeed = 5f;
        const float groundedAcceleration = 55f;
        const float aerialAcceleration = 10f;
        const float gravity = 18f;
        const float groundedJumpForce = 5.5f;
        const float aerialJumpForce = 6.5f;

        float yLook = 0;
        bool canDoubleJump = false;

        InputFlags lastFrameInputs = new();

        public void ServerInit() {
            characterController = GetComponent<CharacterController>();
            entity = GetComponent<WEntityBase>();
        }

        public void EnablePlayer()
        {
            boundedRotator = new();
            cam.enabled = true;
            cam.gameObject.GetComponent<AudioListener>().enabled = true;
            entity ??= GetComponent<WEntityBase>();
            entity.renderPersonalRotationUpdates = true;
        }


        public void DisablePlayer()
        {
            cam.enabled = false;
            cam.gameObject.GetComponent<AudioListener>().enabled = false;
            entity.renderPersonalRotationUpdates = false;
        }


        public void Control(WInputsSerializable inputs) {
            if(entity == null)
                return;

            if(inputs.inputFlags.GetFlag(InputType.Look) && inputs.look.HasValue) {
                lastReceivedLook.x = inputs.look.Value.x;
                lastReceivedLook.y = inputs.look.Value.y;
            }

            Quaternion rotation = Quaternion.Euler(0, lastReceivedLook.x, 0);

            float forward = (inputs.inputFlags.GetFlag(InputType.Forward) ? 1f : 0f) - (inputs.inputFlags.GetFlag(InputType.Back) ? 1f : 0f);
            float left = (inputs.inputFlags.GetFlag(InputType.Right) ? 1f : 0f) - (inputs.inputFlags.GetFlag(InputType.Left) ? 1f : 0f);

            Vector3 wasdInput = new(left, 0, forward);
            if(forward != 0 && left != 0)
                wasdInput *= .7071f;

            // ===========

            bool isGrounded = Physics.Raycast(entity.currentPosition, Vector3.down, 1.1f);
            
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
                    xzVelocityToUse = (xzVelocityAfterMovement / xzSpeedAfterMovement) * maxSpeed * 0.99f;
                }
                // If walking within maxWalkingSpeed, allow it
                else {
                    xzVelocityToUse = xzVelocityAfterMovement;
                }
            } else {
                // Allow the player to influence movement, but not accelerate faster than their current speed
                xzVelocityToUse = (xzVelocityAfterMovement / xzSpeedAfterMovement) * xzSpeed;
            }

            velocity = new Vector3(xzVelocityToUse.x, velocity.y + gravityAcceleration, xzVelocityToUse.y);

            if(isGrounded)
                canDoubleJump = true;

            if(inputs.inputFlags.GetFlag(InputType.Jump) && !lastFrameInputs.GetFlag(InputType.Jump)) {
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

            Vector3 originalPosition = transform.position;

            // Calculate distance cc wants to move
            transform.position = entity.currentPosition;
            characterController.Move(velocity * WCommon.SECONDS_PER_TICK);
            Vector3 difference = transform.position - entity.currentPosition;

            entity.currentPosition += difference;

            // Visual position will be set before next frame is shown, not important to set here
            transform.position = originalPosition;  // Setting it anyway
            lastFrameInputs = inputs.inputFlags;
        }


        public void AddRotationDelta(Vector2 delta) {
            boundedRotator.AddRotationDelta(new Vector2(delta.x, 0));

            if(!entity.renderPersonalRotationUpdates)
                return;
            
            yLook -= delta.y;
            yLook = Mathf.Clamp(yLook, -90f, 90f);
            entity.transform.rotation = boundedRotator.QuatRotation;

            Vector3 cameraEulerAngles = cam.transform.eulerAngles;
            cameraEulerAngles.x = yLook;
            cam.transform.eulerAngles = cameraEulerAngles;
        }


        public Vector2? PollLook()
        {
            return boundedRotator.PollLook();
        }
    }
}
