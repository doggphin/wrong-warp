using System.Collections.Generic;
using LiteNetLib;
using Networking.Shared;

namespace Networking.Server {
    public class SPlayerInputsSlotterManager : BaseSingleton<SPlayerInputsSlotterManager> {

        protected override void Awake() {
            SNetManager.PlayerJoined += AddPlayer;
            SNetManager.PlayerLeft += RemovePlayer;
            CPacket<CGroupedInputsPkt>.Apply += ReceiveGroupedInputsPkt;

            base.Awake();
        }

        protected override void OnDestroy()
        {
            SNetManager.PlayerJoined -= AddPlayer;
            SNetManager.PlayerLeft -= RemovePlayer;
            CPacket<CGroupedInputsPkt>.Apply -= ReceiveGroupedInputsPkt;

            base.OnDestroy();
        }

        private Dictionary<int, TimestampedCircularTickBuffer<InputsSerializable>> playerCycledInputs = new();

        private void AddPlayer(SPlayer player) {
            playerCycledInputs[player.Peer.Id] = new(-1);
        }

        public static void RemovePlayer(SPlayer player) {
            Instance.playerCycledInputs.Remove(player.Peer.Id);
        }

        public static bool TryGetInputsOfAPlayer(int tick, SPlayer player, out InputsSerializable outInputs) {
            var timestampedInputs = Instance.playerCycledInputs[player.Peer.Id];
            if(timestampedInputs.GetTimestamp(tick) == tick) {
                outInputs = timestampedInputs[tick];
                return true;
            } else {
                outInputs = null;
                return false; 
            } 
        }

        public static void SetInputsOfPlayer(int tick, int playerId, InputsSerializable inputsPkt) {
            var buffer = Instance.playerCycledInputs[playerId];
            buffer.SetValueAndTimestampIfMoreRecent(tick, inputsPkt);
        }

        // Sets multiple inputs at a time
        private void ReceiveGroupedInputsPkt(int tick, CGroupedInputsPkt pkt, NetPeer sender) =>
            SetGroupedInputsOfPlayer(tick, sender.Id, pkt);
        private void SetGroupedInputsOfPlayer(int tick, int peerId, CGroupedInputsPkt groupedInputsPkt) {
            for(int i=0; i<groupedInputsPkt.inputsSerialized.Length; i++) {
                SetInputsOfPlayer(tick + i, peerId, groupedInputsPkt.inputsSerialized[i]);
            }
        }
    }
}