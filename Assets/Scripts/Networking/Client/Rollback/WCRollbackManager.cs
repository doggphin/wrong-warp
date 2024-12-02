using System.Runtime.InteropServices.WindowsRuntime;
using Controllers.Shared;
using LiteNetLib.Utils;
using Mono.Cecil.Cil;
using Networking.Client;
using Networking.Shared;

public static class WCRollbackManager {
    public static CircularTickBuffer<DefaultControllerStateSerializable> defaultControllerStates = new();
    public static void Rollback(int currentTick, int rollbackTo) {
        for(int tick=currentTick; tick > rollbackTo; tick--) {
            defaultControllerStates[tick] = null;
        }

        ControlsManager.player?.RollbackToTick(currentTick);
    }





    public static bool ReceiveDefaultControllerStateConfirmation(
        int tick,
        WTransformSerializable confirmedTransform,
        DefaultControllerStateSerializable confirmedState
    ) {
        WCEntity playerEntity = WCNetClient.PlayerEntity;
        
        bool isSameTransform = !confirmedTransform.position.HasValue || playerEntity.positionsBuffer[WCommon.GetModuloTPS(tick)] == confirmedTransform.position.Value;

        DefaultControllerStateSerializable predictedState = defaultControllerStates[tick];
        bool isSameDefaultControllerState =
            predictedState.boundedRotatorRotation == confirmedState.boundedRotatorRotation &&
            predictedState.canDoubleJump == confirmedState.canDoubleJump &&
            predictedState.previousInputs.inputFlags.flags == confirmedState.previousInputs.inputFlags.flags && predictedState.previousInputs.look == confirmedState.previousInputs.look &&
            predictedState.velocity == confirmedState.previousInputs.look;

        return isSameTransform && isSameDefaultControllerState;
    }
}