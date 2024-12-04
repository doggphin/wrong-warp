using UnityEngine;
using Controllers.Shared;
using Mono.Cecil.Cil;
using Networking.Shared;

public static class WCRollbackManager {
    public static CircularTickBuffer<WSDefaultControllerStatePkt> defaultControllerStates = new();

    // Rolls the player back from currentTick to rollbackTo.
    public static void Rollback(int currentTick, int rollbackToThisTick, WSDefaultControllerStatePkt confirmedPacket) {
        Debug.Log($"Rolling back from {currentTick} to {rollbackToThisTick}");
        // Replace the predicted controller state with the confirmed one
        defaultControllerStates[rollbackToThisTick] = confirmedPacket;

        // Null out all controller states after the confirmed state since they're all invalid
        for(int i=currentTick; i > rollbackToThisTick; i--) {
            defaultControllerStates[i] = null;
        }

        // Roll the player back to this tick
        ControlsManager.player?.RollbackToTick(rollbackToThisTick);
    }


    public static void SetDefaultControllerState(int tick, WSDefaultControllerStatePkt controllerState) {
        defaultControllerStates[tick] = controllerState;
    }


    public static bool ReceiveDefaultControllerStateConfirmation(
        int tick,
        WSDefaultControllerStatePkt confirmedState
    ) {
        var predictedState = defaultControllerStates[tick];

        if(predictedState == null)
            return true;

        bool isSameDefaultControllerState =
            predictedState.boundedRotatorRotation == confirmedState.boundedRotatorRotation &&
            predictedState.canDoubleJump == confirmedState.canDoubleJump &&
            predictedState.previousInputs.inputFlags.flags == confirmedState.previousInputs.inputFlags.flags &&
            predictedState.previousInputs.look == confirmedState.previousInputs.look &&
            predictedState.velocity == confirmedState.previousInputs.look &&
            predictedState.position == confirmedState.position;

        return isSameDefaultControllerState;
    }
}