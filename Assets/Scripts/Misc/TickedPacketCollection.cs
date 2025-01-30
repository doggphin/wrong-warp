using System.Collections.Generic;
using UnityEngine;
using LiteNetLib.Utils;
using Networking.Shared;

public class TickedPacketCollection : BasePacket, ITickedContainer {
    private Dictionary<int, List<BasePacket>> tickedPacketCollections = new();
    public bool HasData { get => tickedPacketCollections.Count > 0; }

    public void AddPacket(int tick, BasePacket packet) {
        if(!tickedPacketCollections.TryGetValue(tick, out var list)) {
            list = new(1);
            tickedPacketCollections[tick] = list;
        }

        list.Add(packet);
    }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(PacketIdentifier.STickedPacketCollection);

        writer.Put(tickedPacketCollections);
    }

    public void Reset() {
        tickedPacketCollections.Clear();
    }

    ///<returns> Whether any data was serialized </returns>
    public bool SerializeAndReset(NetDataWriter writer, bool serializeIfNoData) {
        if(!serializeIfNoData && !HasData) {
            return false;
        }

        Serialize(writer);
        Reset();
        return true;
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