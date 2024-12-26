using UnityEngine;
using Controllers.Shared;
using Mono.Cecil.Cil;
using Networking.Shared;
using System;

public static class WCRollbackManager {
    public static TimestampedCircularTickBuffer<WSDefaultControllerStatePkt> defaultControllerStates = new();

    // Rolls the player back from currentTick to rollbackTo.
    public static void RollbackDefaultController(int currentTick, int rollbackToTick, WSDefaultControllerStatePkt confirmedPacket) {
        Debug.Log($"Rolling back from {currentTick} to {rollbackToTick}");

        // Null out all controller states after the confirmed state since they're all invalid
        // How necessary is this?
        for(int i=currentTick; i > rollbackToTick; i--) {
            defaultControllerStates[i] = null;
        }

        // Replace the predicted controller state with the confirmed one
        defaultControllerStates[rollbackToTick] = confirmedPacket;

        // Roll the player back
        ControlsManager.player?.RollbackToTick(rollbackToTick);
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
            predictedState.velocity == confirmedState.velocity &&
            predictedState.position == confirmedState.position;

        if(isSameDefaultControllerState) {
            Debug.Log("Same controller state!");
        } else {
            Debug.Log("Different controller state!");
            //Debug.Log($"Predicted rotation for tick {tick} was {predictedState.boundedRotatorRotation}, received {confirmedState.boundedRotatorRotation}");
            //Debug.Log($"Predicted look for tick {tick} was {predictedState.previousInputs.look}, received {confirmedState.previousInputs.look}");
            Debug.Log($"== Predicted position for tick {tick} was {predictedState.position}, received {confirmedState.position}");
            Debug.Log($"-1 Predicted position for tick {tick - 1} was {defaultControllerStates[tick - 1].position}");
            Debug.Log($"-2 Predicted position for tick {tick - 2} was {defaultControllerStates[tick - 2].position}");
        }
        return isSameDefaultControllerState;
    }
}