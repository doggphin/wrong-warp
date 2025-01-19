using System.Collections.Generic;
using UnityEngine;
using LiteNetLib.Utils;
using Mono.Cecil;

public class TickedPacketCollection : INetSerializable {
    private Dictionary<int, List<INetPacketForClient>> tickedPacketCollections = new();
    public bool HasPackets => tickedPacketCollections.Count > 0;
    
    public void AddPacket(int tick, INetPacketForClient packet) {
        Debug.Log("Adding a packet!");
        if(!tickedPacketCollections.TryGetValue(tick, out var list)) {
            list = new(1);
        }

        list.Add(packet);
    }
    
    ///<summary> Does nothing if no packets exist </summary>
    public void SerializeAndReset(NetDataWriter writer) {
        Serialize(writer);
        tickedPacketCollections.Clear();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(tickedPacketCollections);
    }

    public void Deserialize(NetDataReader reader)
    {
        tickedPacketCollections = reader.GetTickedPacketCollection();
    }
}