using Networking.Shared;
using UnityEngine.InputSystem;
using UnityEngine;
using System;
using UnityEngine.WSA;
using Networking.Client;
using System.Text.RegularExpressions;
using Audio.Shared;

namespace Controllers.Shared {
    public static class ControlsManager {
        private static PlayerInputActions inputActions;

        public static AbstractPlayer player = null;

        public static TimestampedCircularTickBuffer<WInputsSerializable> inputs = new();

        private static InputFlags finalInputs = new();
        private static InputFlags heldInputs = new();

        private static float fireDownSubtickFraction;
        private static Vector2 fireDownLookVector;
        private static float altFireDownSubtickFraction;
        private static Vector2 altFireDownLookVector;

        public static Action ChatClicked;
        public static Action EscapeClicked;
        public static Action ConfirmClicked;
        public static Action InventoryClicked;

        private static bool inputsInitialized = false;
        public static void Init() {      
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

            inputActions.Gameplay.Fire.started += (InputAction.CallbackContext ctx) => HandleOneOffAction(ctx, InputType.FireDownEvent);
            BindInputActionToInputType(inputActions.Gameplay.Fire, InputType.FireDown);

            inputActions.Gameplay.Fire.started += (InputAction.CallbackContext ctx) => HandleOneOffAction(ctx, InputType.FireDownEvent);
            BindInputActionToInputType(inputActions.Gameplay.Fire, InputType.FireDown);
            

            inputActions.Gameplay.Look.performed += (InputAction.CallbackContext ctx) => {
                player?.AddRotationDelta(ctx.action.ReadValue<Vector2>());
            };

            inputActions.Ui.Chat.started += (_) => ChatClicked?.Invoke();
            inputActions.Ui.Escape.started += (_) => EscapeClicked?.Invoke();
            inputActions.Ui.Confirm.started += (_) => ConfirmClicked?.Invoke();
            inputActions.Ui.Inventory.started += (_) => InventoryClicked?.Invoke();

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
            if(player == null)
                return;
            
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


        private static void HandleOneOffAction(InputAction.CallbackContext ctx, InputType inputType) {
            // One-off actions should only be done once a tick. If it's already done, skip it
            if(player == null || ctx.phase != InputActionPhase.Started || finalInputs.GetFlag(inputType))
                return;

            finalInputs.SetFlag(inputType, true);
            switch(inputType) {
                case InputType.FireDownEvent:
                    fireDownSubtickFraction = WNetManager.GetPercentageThroughTickCurrentFrame();
                    Debug.Log(fireDownSubtickFraction);
                    fireDownLookVector = player.GetLook();
                    AudioManager.PlayPositionedSoundEffect(new PositionedSoundEffectSettings { audioEffect = AudioEffect.SpellBurst, position = Vector3.zero });
                    break;
                case InputType.AltFireDownEvent:
                    altFireDownSubtickFraction = WNetManager.GetPercentageThroughTickCurrentFrame();
                    altFireDownLookVector = player.GetLook();
                    break;
            }
        }


        public static void PollAndControl(int onTick) {
            if(player == null)
                return;
            
            WInputsSerializable serializedInputs = inputs[onTick];

            if(inputs.CheckTickIsMoreRecent(onTick)) {
                Vector2? rotation = player?.PollLook();
                finalInputs.SetFlag(InputType.Look, rotation.HasValue);
                finalInputs.flags |= heldInputs.flags;

                if(finalInputs.GetFlag(InputType.FireDownEvent)) {
                    serializedInputs.fireDownSubtick = fireDownSubtickFraction;
                    serializedInputs.fireDownLookVector = fireDownLookVector;
                }
                
                serializedInputs.inputFlags.flags = finalInputs.flags;
                serializedInputs.look = rotation;
                inputs.SetTimestamp(onTick);

                finalInputs.Reset();
            }

            player.Control(serializedInputs, onTick);
        }
    }
}