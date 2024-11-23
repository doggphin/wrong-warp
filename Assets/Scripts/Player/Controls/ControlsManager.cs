using Networking.Shared;
using UnityEngine.InputSystem;
using UnityEngine;
using System.Collections.Generic;

namespace Controllers.Client {
    public static class ControlsManager {
        private static PlayerInputActions inputActions = new();

        public static IRotatable mainRotatable = null;
        public static IControllable mainControllable = null;

        private static InputFlags finalInputs = new();
        private static InputFlags heldInputs = new();
        
        public static void Init() {
            inputActions = new();

            inputActions.Gameplay.Forward.performed += (InputAction.CallbackContext ctx) => HandleBufferedAction(ctx, InputType.Forward);
            inputActions.Gameplay.Left.started += (InputAction.CallbackContext ctx) => HandleBufferedAction(ctx, InputType.Left);
            inputActions.Gameplay.Back.started += (InputAction.CallbackContext ctx) => HandleBufferedAction(ctx, InputType.Back);
            inputActions.Gameplay.Right.started += (InputAction.CallbackContext ctx) => HandleBufferedAction(ctx, InputType.Right);

            inputActions.Gameplay.Look.performed += (InputAction.CallbackContext ctx) => { 
                mainRotatable.AddRotationDelta(ctx.action.ReadValue<Vector2>()); 
            };
        }


        private static void HandleBufferedAction(InputAction.CallbackContext ctx, InputType inputType) {
            // If STARTING to press this button, set it to held and turn it on in final inputs
            if(ctx.phase == InputActionPhase.Started) {
                heldInputs.SetFlag(inputType, true);
                finalInputs.SetFlag(inputType, true);

            // If RELEASING a button, unhold the button, but still keep it on in final inputs
            // This makes pressing for less than a tick still count as a keypress
            } else if(ctx.phase == InputActionPhase.Canceled && heldInputs.GetFlag(inputType)) {
                heldInputs.SetFlag(inputType, false);
                finalInputs.SetFlag(inputType, true);
            }
        }


        public static WCInputsPkt Poll() {
            if(mainControllable == null && mainRotatable == null)
                return null;

            Vector2? rotation = mainRotatable.PollRotation();
            finalInputs.SetFlag(InputType.Look, rotation.HasValue);

            WCInputsPkt ret = new() {
                inputFlags = finalInputs,
                look = mainRotatable.PollRotation()
            };

            return ret;
        }
    }
}