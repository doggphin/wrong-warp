using System;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib.Utils;
using Networking.Shared;

public abstract class PacketUnpacker<T> : BaseSingleton<T> where T : PacketUnpacker<T>, new() {
    ///<summary> Given a class T that implements INetPacketForClient, generically deserialize it from the NetDataReader </summary>
    ///<returns> The deserialized packet </returns>
    protected static BasePacket Deserialize<TPacket>(NetDataReader reader) where TPacket : BasePacket, new() {
        TPacket ret = new();
        ret.Deserialize(reader);
        return ret;
    }

    protected abstract Dictionary<PacketIdentifier, Func<NetDataReader, BasePacket>> PacketDeserializers { get; }

    ///<summary> Reads out a packet type and then a packet that matches that type from a NetDataReader </summary>
    ///<returns> The deserialized packet </returns>
    public static BasePacket DeserializeNextPacket(NetDataReader reader) {
        ushort packetTypeUShort = reader.GetUShort();
        if(!Instance.PacketDeserializers.TryGetValue((PacketIdentifier)packetTypeUShort, out var function)) {
            throw new Exception($"No handler for {(PacketIdentifier)packetTypeUShort} (Code {packetTypeUShort})!");
        }
        return function(reader);
    }

    ///<summary> Either caches or immediatedly applies a packet based on its logic </summary>
    public static void ConsumePacket(int tick, BasePacket packet) {
        if(packet.ShouldCache) {
            PacketCacheManager.CachePacket(tick, packet);
        } else {
            packet.BroadcastApply(tick);
        }
    }

    ///<summary> Reads through next packet in NetDataReader, applying or caching it as per the packet's logic  </summary>
    public static void ConsumeNextPacket(int tick, NetDataReader reader) {
        BasePacket packet = DeserializeNextPacket(reader);
        ConsumePacket(tick, packet);
    }

    ///<summary> Reads through an entire NetDataReader, applying or caching each packet read </summary>
    public static void ConsumeAllPackets(int tick, NetDataReader reader) {
        while(reader.AvailableBytes >= sizeof(ushort)) {
            ConsumeNextPacket(tick, reader);
        }
    }
}