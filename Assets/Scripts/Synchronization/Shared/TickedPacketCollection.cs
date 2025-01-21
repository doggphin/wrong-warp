using System.Collections.Generic;
using UnityEngine;
using LiteNetLib.Utils;
using Networking.Shared;

public class TickedPacketCollection : NetPacketForClient {
    private Dictionary<int, List<NetPacketForClient>> tickedPacketCollections = new();
    public bool HasPackets => tickedPacketCollections.Count > 0;

    public void AddPacket(int tick, NetPacketForClient packet) {
        if(!tickedPacketCollections.TryGetValue(tick, out var list)) {
            list = new(1);
            tickedPacketCollections[tick] = list;
        }

        list.Add(packet);
    }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(WPacketIdentifier.SGenericUpdatesCollection);

        writer.Put(tickedPacketCollections);
    }

    ///<summary> Does nothing if no packets exist </summary>
    public void SerializeAndReset(NetDataWriter writer) {
        Serialize(writer);
        tickedPacketCollections.Clear();
    }

    public override void Deserialize(NetDataReader reader)
    {
        tickedPacketCollections = reader.GetTickedPacketCollection();
    }

    public override bool ShouldCache => false;
    protected override void BroadcastApply(int tick)
    {
        foreach(var kvp in tickedPacketCollections) {
            foreach(NetPacketForClient packet in kvp.Value) {
                WCPacketForClientUnpacker.ConsumePacket(kvp.Key, packet);
            }  
        }
    }
}