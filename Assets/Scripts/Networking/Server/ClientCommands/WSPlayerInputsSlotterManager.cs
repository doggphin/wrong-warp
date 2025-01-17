using System.Collections.Generic;
using LiteNetLib;
using Networking.Shared;
using UnityEngine;

namespace Networking.Server {
    public class WSPlayerInputsSlotterManager : BaseSingleton<WSPlayerInputsSlotterManager> {
        class ReceivedInputs {
            public WInputsSerializable inputs;
            public int receivedTick;

            public ReceivedInputs(WInputsSerializable inputs, int tick) {
                this.inputs = inputs;
                receivedTick = tick;
            }

            public ReceivedInputs() {
                inputs = null;
                receivedTick = -1;
            }
        }


        protected override void Awake() {
            base.Awake();

            WSNetServer.PlayerJoined += AddPlayer;
            WSNetServer.PlayerLeft += RemovePlayer;
        }

        protected override void OnDestroy()
        {
            WSNetServer.PlayerJoined -= AddPlayer;
            WSNetServer.PlayerLeft -= RemovePlayer;

            base.OnDestroy();
        }

        private void AddPlayer(WSPlayer player) => AddPlayer(player.Peer.Id);
        private void RemovePlayer(WSPlayer player) => RemovePlayer(player.Peer.Id);

        // Player ID -> Input packets, buffered by a second
        private Dictionary<int, CircularTickBuffer<ReceivedInputs>> playerCycledInputs = new();

        public void AddPlayer(int peerId) {
            var buffer = new CircularTickBuffer<ReceivedInputs>();
            playerCycledInputs[peerId] = buffer;

            for(int i=0; i<WCommon.TICKS_PER_SECOND; i++) {
                buffer.buffer[i] = new();
            }
        }

        // Removes a player from playerCycledInputs
        public void RemovePlayer(int peerId) {
            playerCycledInputs.Remove(peerId);
        }


        // Gets the inputs of a player with no strings attached
        private ReceivedInputs GetInputsOfAPlayerUnsafe(int tick, int peerId) {
            return playerCycledInputs[peerId][tick];
        }


        // Gets the inputs of a player for this tick as long as they're up to date
        // TODO: this should use older ticks if possible
        public WInputsSerializable GetInputsOfAPlayer(int tick, int peerId) {
            ReceivedInputs receivedInputs = GetInputsOfAPlayerUnsafe(tick, peerId);
            
            return receivedInputs.receivedTick == tick ? receivedInputs.inputs : null;
        }


        // Sets the inputs of a player for this tick as long as they didn't send inputs for this tick already
        public void SetInputsOfPlayer(int tick, int playerId, WInputsSerializable inputsPkt) {
            ReceivedInputs receivedInputs = GetInputsOfAPlayerUnsafe(tick, playerId);
            
            // Don't allow player to send inputs on a tick they already sent inputs for
            if(tick > receivedInputs.receivedTick)
                receivedInputs.inputs = inputsPkt;
                receivedInputs.receivedTick = tick;
        }


        // Sets multiple inputs at a time
        public void SetGroupedInputsOfPlayer(int tick, int peerId, WCGroupedInputsPkt groupedInputsPkt) {
            for(int i=0; i<groupedInputsPkt.inputsSerialized.Length; i++) {
                SetInputsOfPlayer(tick + i, peerId, groupedInputsPkt.inputsSerialized[i]);
            }
        }
    }
}