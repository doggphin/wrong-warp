using System.Collections.Generic;
using UnityEngine;
using LiteNetLib.Utils;
using Networking.Shared;

public class TickedPacketCollection : INetPacketForClient {
    private Dictionary<int, List<INetPacketForClient>> tickedPacketCollections = new();
    public bool HasPackets => tickedPacketCollections.Count > 0;

    public void AddPacket(int tick, INetPacketForClient packet) {
        if(!tickedPacketCollections.TryGetValue(tick, out var list)) {
            list = new(1);
            tickedPacketCollections[tick] = list;
        }

        list.Add(packet);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(WPacketIdentifier.SGenericUpdatesCollection);

        writer.Put(tickedPacketCollections);
    }

    ///<summary> Does nothing if no packets exist </summary>
    public void SerializeAndReset(NetDataWriter writer) {
        Serialize(writer);
        tickedPacketCollections.Clear();
    }

    public void Deserialize(NetDataReader reader)
    {
        tickedPacketCollections = reader.GetTickedPacketCollection();
    }

    public bool ShouldCache => false;
    public void ApplyOnClient(int tick)
    {
        foreach(var kvp in tickedPacketCollections) {
            foreach(INetPacketForClient packet in kvp.Value) {
                WCPacketForClientUnpacker.ConsumePacket(kvp.Key, packet);
            }  
        }
    }
}