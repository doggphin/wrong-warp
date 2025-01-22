using System.Collections.Generic;
using UnityEngine;
using LiteNetLib.Utils;
using Networking.Shared;

public class TickedPacketCollection : BasePacket {
    private Dictionary<int, List<BasePacket>> tickedPacketCollections = new();
    public bool HasPackets => tickedPacketCollections.Count > 0;

    public void AddPacket(int tick, BasePacket packet) {
        if(!tickedPacketCollections.TryGetValue(tick, out var list)) {
            list = new(1);
            tickedPacketCollections[tick] = list;
        }

        list.Add(packet);
    }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(PacketIdentifier.SGenericUpdatesCollection);

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
    protected override void OverridableBroadcastApply(int tick)
    {
        foreach(var kvp in tickedPacketCollections) {
            foreach(BasePacket packet in kvp.Value) {
                CPacketUnpacker.ConsumePacket(kvp.Key, packet);
            }  
        }
    }
}