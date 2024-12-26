using Networking.Shared;
using UnityEngine.InputSystem;
using UnityEngine;
using Unity.VisualScripting;

namespace Controllers.Shared {
    public static class ControlsManager {
        private static PlayerInputActions inputActions = new();

        public static IPlayer player = null;

        public static TimestampedCircularTickBuffer<WInputsSerializable> inputs = new();
        private static InputFlags finalInputs = new();
        private static InputFlags heldInputs = new();
        
        public static void Init() {
            for(int i=0; i<inputs.buffer.Length; i++) {
                inputs.buffer[i] = new();
            }

            void BindInputActionToInputType(InputAction inputAction, InputType inputType) {
                inputAction.started += (InputAction.CallbackContext ctx) => HandleBufferedAction(ctx, inputType);
                inputAction.canceled += (InputAction.CallbackContext ctx) => HandleBufferedAction(ctx, inputType);
            }

            inputActions = new();
            inputActions.Gameplay.Enable();

            BindInputActionToInputType(inputActions.Gameplay.Forward, InputType.Forward);
            BindInputActionToInputType(inputActions.Gameplay.Left, InputType.Left);
            BindInputActionToInputType(inputActions.Gameplay.Back, InputType.Back);
            BindInputActionToInputType(inputActions.Gameplay.Right, InputType.Right);
            BindInputActionToInputType(inputActions.Gameplay.Jump, InputType.Jump);
            BindInputActionToInputType(inputActions.Gameplay.Crouch, InputType.Crouch);

            inputActions.Gameplay.Look.performed += (InputAction.CallbackContext ctx) => { 
                player?.AddRotationDelta(ctx.action.ReadValue<Vector2>());
            };
        }


        private static void HandleBufferedAction(InputAction.CallbackContext ctx, InputType inputType) {
            // If STARTING to press this button, set it to held and turn it on in final inputs
            if(ctx.phase == InputActionPhase.Started) {
                heldInputs.SetFlag(inputType, true);
                finalInputs.SetFlag(inputType, true);
            }

            // If RELEASING a button, unhold the button, but still keep it on in final inputs
            // This makes pressing for less than a tick still count as a keypress
            else if(ctx.phase == InputActionPhase.Canceled) {
                heldInputs.SetFlag(inputType, false);
                finalInputs.SetFlag(inputType, true);
            }
        }


        public static void PollAndControl(int onTick) {
            if(inputs.CheckTickIsMoreRecent(onTick)) {
                Vector2? rotation = player?.PollLook();
                finalInputs.SetFlag(InputType.Look, rotation.HasValue);
                finalInputs.flags |= heldInputs.flags;

                inputs[onTick].inputFlags.flags = finalInputs.flags;
                inputs[onTick].look = rotation;
                inputs.SetTimestamp(onTick);

                player?.Control(inputs[onTick], onTick);

                finalInputs.Reset();
            } else {
                player?.Control(inputs[onTick], onTick);
            }
        }
    }
}