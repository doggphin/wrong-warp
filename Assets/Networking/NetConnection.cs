using Networking;
using UnityEngine;

public class NetConnection {
    public NetObject playerObject { get; private set; }
    public long playerId { get; private set; }

    public NetConnection(NetObject playerObject, long playerId) {
        this.playerObject = playerObject;
        this.playerId = playerId;
    }
}
