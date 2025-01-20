using UnityEngine;
using Controllers.Shared;
using Networking.Shared;

public class WCRollbackManager : BaseSingleton<WCRollbackManager> {
    private TimestampedCircularTickBuffer<WSDefaultControllerStatePkt> defaultControllerStates;

    protected override void Awake()
    {
        base.Awake();
        defaultControllerStates = TimestampedCircularTickBufferClassInitializer<WSDefaultControllerStatePkt>.GetInitialized(-1);
    }

    // Rolls the player back from currentTick to rollbackTo.
    public static void RollbackDefaultController(int currentTick, int rollbackToTick, WSDefaultControllerStatePkt confirmedPacket) {
        Debug.Log($"Rolling back from {currentTick} to {rollbackToTick}");

        // Null out all controller states after the confirmed state since they're all invalid
        // How necessary is this?
        for(int i=currentTick; i > rollbackToTick; i--) {
            Instance.defaultControllerStates[i] = null;
        }

        // Replace the predicted controller state with the confirmed one
        Instance.defaultControllerStates[rollbackToTick] = confirmedPacket;

        // Roll the player back
        if(ControlsManager.player != null) {
            ControlsManager.player.RollbackToTick(rollbackToTick);
        }
    }

    public static WSDefaultControllerStatePkt GetDefaultControllerState(int tick) {
        return Instance.defaultControllerStates[tick];
    }

    public static void SetDefaultControllerState(int tick, WSDefaultControllerStatePkt defaultControllerState) {
        Instance.defaultControllerStates.SetValueAndTimestamp(defaultControllerState, tick);
    }


    public static bool ReceiveDefaultControllerStateConfirmation(int tick, WSDefaultControllerStatePkt confirmedState) {
        var predictedState = Instance.defaultControllerStates[tick];

        if(predictedState == null)
            return true;

        bool isSameDefaultControllerState =
            predictedState.boundedRotatorRotation == confirmedState.boundedRotatorRotation &&
            predictedState.canDoubleJump == confirmedState.canDoubleJump &&
            predictedState.previousInputs.inputFlags.flags == confirmedState.previousInputs.inputFlags.flags &&
            predictedState.previousInputs.look == confirmedState.previousInputs.look &&
            predictedState.velocity == confirmedState.velocity &&
            predictedState.position == confirmedState.position;

        if(!isSameDefaultControllerState) {
            Debug.Log("Desync detected!");
        }
        
        return isSameDefaultControllerState;
    }
}