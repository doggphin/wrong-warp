using Networking.Shared;
using UnityEngine.InputSystem;
using UnityEngine;
using System;
using UnityEngine.WSA;

namespace Controllers.Shared {
    public static class ControlsManager {
        private static PlayerInputActions inputActions;

        public static IPlayer player = null;

        public static TimestampedCircularTickBuffer<WInputsSerializable> inputs = new();
        private static InputFlags finalInputs = new();
        private static InputFlags heldInputs = new();

        public static Action ChatClicked;
        public static Action EscapeClicked;
        public static Action ConfirmClicked;


        private static bool inputsInitialized = false;
        public static void Init() {      
            // Always reinitialize inputs.
            // Otherwise, old inputs can sit here between resets
            for(int i=0; i<inputs.buffer.Length; i++) {
                inputs.buffer[i] = new();
            }

            if(inputsInitialized)
                return;

            void BindInputActionToInputType(InputAction inputAction, InputType inputType) {
                inputAction.started += (InputAction.CallbackContext ctx) => HandleBufferedAction(ctx, inputType);
                inputAction.canceled += (InputAction.CallbackContext ctx) => HandleBufferedAction(ctx, inputType);
            }

            inputActions = new();

            BindInputActionToInputType(inputActions.Gameplay.Forward, InputType.Forward);
            BindInputActionToInputType(inputActions.Gameplay.Left, InputType.Left);
            BindInputActionToInputType(inputActions.Gameplay.Back, InputType.Back);
            BindInputActionToInputType(inputActions.Gameplay.Right, InputType.Right);
            BindInputActionToInputType(inputActions.Gameplay.Jump, InputType.Jump);
            BindInputActionToInputType(inputActions.Gameplay.Crouch, InputType.Crouch);

            inputActions.Gameplay.Look.performed += (InputAction.CallbackContext ctx) => {
                player?.AddRotationDelta(ctx.action.ReadValue<Vector2>());
            };

            inputActions.Ui.Chat.started += (_) => ChatClicked?.Invoke();
            inputActions.Ui.Escape.started += (_) => EscapeClicked?.Invoke();
            inputActions.Ui.Confirm.started += (_) => ConfirmClicked?.Invoke();

            inputsInitialized = true;
        }

        public static void Activate() {
            Debug.Log("Enabling controls!");
            SetGameplayControlsEnabled(true);
            SetUiControlsEnabled(true);
        }

        public static void Deactivate() {
            Debug.Log("Disabling controls!");
            SetGameplayControlsEnabled(false);
            SetUiControlsEnabled(false);
        }


        // Gameplay and Ui don't share a base class, can't be made more efficient
        public static void SetGameplayControlsEnabled(bool value) {
            if(value) {
                inputActions.Gameplay.Enable();
            } else {
                inputActions.Gameplay.Disable();
            }
        }
        public static void SetUiControlsEnabled(bool value) {
            if(value) {
                inputActions.Ui.Enable();
            } else {
                inputActions.Ui.Disable();
            }
        }
        public static bool GameplayControlsEnabled {
            get => inputActions.Gameplay.enabled;
            private set {
                if(value) {
                    inputActions.Gameplay.Enable();
                } else {
                    inputActions.Gameplay.Disable();
                }
            }
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