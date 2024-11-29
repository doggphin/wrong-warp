using System.Collections.Generic;
using UnityEngine;
using Networking.Shared;

namespace Networking.Server {
    public static class WsPlayerInputsSlotter {
        // Player ID -> Input packets, buffered by a second
        private static Dictionary<int, WInputsSerializable[]> playerCycledInputs = new();

        public static void AddPlayer(int id) {
            playerCycledInputs[id] = new WInputsSerializable[WCommon.TICKS_PER_SECOND];
        }


        public static void RemovePlayer(int id) {
            playerCycledInputs.Remove(id);
        }

        
        public static WInputsSerializable GetInputsOfPlayer(int tick, int playerId) {
            WInputsSerializable ret = playerCycledInputs[playerId][tick % WCommon.TICKS_PER_SECOND];
            playerCycledInputs[playerId][tick % WCommon.TICKS_PER_SECOND] = null;
            
            return ret;
        }


        // Returns if there was already an inputs packet for this tick
        public static void SetInputsOfPlayer(int tick, int playerId, WInputsSerializable inputsPkt) {
            playerCycledInputs[playerId][tick % WCommon.TICKS_PER_SECOND] = inputsPkt;
            //Debug.Log($"Inputs are being set to {inputsPkt.inputFlags.flags}");
        }


        public static void SetGroupedInputsOfPlayer(int tick, int playerId, WCGroupedInputsPkt groupedInputsPkt) {
            for(int i=0; i<groupedInputsPkt.inputsSerialized.Length; i++) {
                SetInputsOfPlayer(tick, playerId, groupedInputsPkt.inputsSerialized[i]);
            }
        }
    }
}