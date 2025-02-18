using Networking.Shared;
using UnityEngine.InputSystem;
using UnityEngine;
using System;
using Audio.Shared;
using Networking.Server;
using Inventories;
using Unity.VisualScripting;
using System.IO;


namespace Controllers.Shared {
    public class ControlsManager : BaseSingleton<ControlsManager> {
        private PlayerInputActions inputActions;

        private AbstractPlayer player = null;

        public static bool TryGetPlayer(out AbstractPlayer player) {
            player = Instance.player;
            return player != null;
        }

        public static void SetPlayer(AbstractPlayer player) {
            Instance.player = player;
        }

        public static bool HasPlayer => Instance.player != null;

        public TimestampedCircularTickBuffer<InputsSerializable> inputs = new();

        private InputFlags finalInputs = new();
        private InputFlags heldInputs = new();

        private float fireDownSubtickFraction;
        private Vector2 fireDownLookVector;
        private float altFireDownSubtickFraction;
        private Vector2 altFireDownLookVector;

        public static Action ChatClicked;
        public static Action EscapeClicked;
        public static Action ConfirmClicked;
        public static Action InventoryClicked;
        public static Action InteractStarted;
        public static Action InteractCanceled;

        protected override void Awake() {    
            base.Awake();
              
            inputs = TimestampedCircularTickBufferClassInitializer<InputsSerializable>.Initialize();

            void BindInputActionToInputType(InputAction inputAction, InputType inputType) {
                inputAction.started += ctx => HandleBufferedAction(ctx, inputType);
                inputAction.canceled += ctx => HandleBufferedAction(ctx, inputType);
            }

            inputActions = new();

            BindInputActionToInputType(inputActions.KeyboardControls.Forward, InputType.Forward);
            BindInputActionToInputType(inputActions.KeyboardControls.Left, InputType.Left);
            BindInputActionToInputType(inputActions.KeyboardControls.Back, InputType.Back);
            BindInputActionToInputType(inputActions.KeyboardControls.Right, InputType.Right);
            BindInputActionToInputType(inputActions.KeyboardControls.Jump, InputType.Jump);
            BindInputActionToInputType(inputActions.KeyboardControls.Interact, InputType.Interact);

            inputActions.MouseControls.Fire.started += (InputAction.CallbackContext ctx) => HandleOneOffAction(ctx, InputType.FireDownEvent);
            BindInputActionToInputType(inputActions.MouseControls.Fire, InputType.FireDown);
            

            inputActions.MouseControls.Look.performed += (InputAction.CallbackContext ctx) => {
                if(player != null)
                    player.AddRotationDelta(ctx.action.ReadValue<Vector2>());
            };

            inputActions.Ui.Chat.started += (_) => ChatClicked?.Invoke();
            inputActions.Ui.Escape.started += (_) => EscapeClicked?.Invoke();
            inputActions.Ui.Confirm.started += (_) => ConfirmClicked?.Invoke();
            inputActions.Ui.Inventory.started += (_) => InventoryClicked?.Invoke();
        }


        public static void ActivateControls() {
            SetKeyboardControlsEnabled(true);
            SetUiControlsEnabled(true);
            SetMouseControlsEnabled(true);
        }

        public static void DeactivateControls() {
            SetKeyboardControlsEnabled(false);
            SetUiControlsEnabled(false);
            SetMouseControlsEnabled(false);
        }


        // Gameplay and Ui don't share a base class, can't be made more efficient
        public static void SetKeyboardControlsEnabled(bool value) {
            if(value) {
                Instance.inputActions.KeyboardControls.Enable();
            } else {
                Instance.inputActions.KeyboardControls.Disable();
            }
        }
        public static void SetUiControlsEnabled(bool value) {
            if(value) {
                Instance.inputActions.Ui.Enable();
            } else {
                Instance.inputActions.Ui.Disable();
            }
        }
        public static void SetMouseControlsEnabled(bool value) {
            if(value) {
                Instance.inputActions.MouseControls.Enable();
            } else {
                Instance.inputActions.MouseControls.Disable();
            }
        }


        private void HandleBufferedAction(InputAction.CallbackContext ctx, InputType inputType) {
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


        private void HandleOneOffAction(InputAction.CallbackContext ctx, InputType inputType) {
            // Side effects of one-off actions should only be calculated once a tick
            // So if one-off action has already been done this tick, return early
            if(player == null || ctx.phase != InputActionPhase.Started || finalInputs.GetFlag(inputType))
                return;

            finalInputs.SetFlag(inputType, true);
            switch(inputType) {
                case InputType.FireDownEvent:
                    fireDownSubtickFraction = WWNetManager.GetPercentageThroughTickCurrentFrame();
                    Debug.Log(fireDownSubtickFraction);
                    fireDownLookVector = player.GetLook();
                    // TODO: remove this
                    AudioManager.PlayPositionedSoundEffect("Spells/Burst", player.transform);
                    SEntity entity = SEntityManager.Instance.SpawnEntity(EntityPrefabId.DroppedItem, null, null, null);
                    entity.GetComponent<InteractableTakeable>().item = new SlottedItem(ItemType.TestingPotion, 5);
                    break;
                case InputType.AltFireDownEvent:
                    altFireDownSubtickFraction = WWNetManager.GetPercentageThroughTickCurrentFrame();
                    altFireDownLookVector = player.GetLook();
                    break;
            }
        }


        public void PollAndControl(int onTick) {
            if(player == null)
                return;
            
            InputsSerializable serializedInputs = inputs[onTick];

            if(inputs.IsInputTickNewer(onTick)) {
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