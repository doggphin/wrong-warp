using Networking.Shared;
using UnityEngine.InputSystem;
using UnityEngine;

namespace Controllers.Shared {
    public static class ControlsManager {
        private static PlayerInputActions inputActions = new();

        public static IRotatable mainRotatable = null;
        public static IControllable mainControllable = null;

        private static InputFlags finalInputs = new();
        private static InputFlags heldInputs = new();
        
        public static void Init() {
            void InitInputAction(InputAction inputAction, InputType inputType) {
                inputAction.started += (InputAction.CallbackContext ctx) => HandleBufferedAction(ctx, inputType);
                inputAction.canceled += (InputAction.CallbackContext ctx) => HandleBufferedAction(ctx, inputType);
            }

            inputActions = new();
            inputActions.Gameplay.Enable();

            InitInputAction(inputActions.Gameplay.Forward, InputType.Forward);
            InitInputAction(inputActions.Gameplay.Left, InputType.Left);
            InitInputAction(inputActions.Gameplay.Back, InputType.Back);
            InitInputAction(inputActions.Gameplay.Right, InputType.Right);

            inputActions.Gameplay.Look.performed += (InputAction.CallbackContext ctx) => { 
                mainRotatable?.AddRotationDelta(ctx.action.ReadValue<Vector2>()); 
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


        public static void Poll(WCInputsPkt writeTo) {
            Vector2? rotation = mainRotatable?.PollRotation();
            finalInputs.SetFlag(InputType.Look, rotation.HasValue);

            finalInputs.flags |= heldInputs.flags;
            
            writeTo ??= new();
            writeTo.inputFlags.flags = finalInputs.flags;
            writeTo.look = rotation;

            mainControllable?.Control(writeTo);

            finalInputs.Reset();
        }
    }
}