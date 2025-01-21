using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using Networking.Shared;
using Networking.Client;

public static class WCPacketForClientUnpacker {
    ///<summary> Given a class T that implements INetPacketForClient, generically deserialize it from the NetDataReader </summary>
    ///<returns> The deserialized packet </returns>
    private static INetPacketForClient Deserialize<T>(NetDataReader reader) where T : class, INetPacketForClient, new() {
        T ret = new();
        ret.Deserialize(reader);
        return ret;
    }

    private static readonly Dictionary<WPacketIdentifier, Func<NetDataReader, INetPacketForClient>> packetDeserializers = new() {
        { WPacketIdentifier.SJoinAccept, Deserialize<WSJoinAcceptPkt> },
        { WPacketIdentifier.SChunkDeltaSnapshot, Deserialize<WSChunkDeltaSnapshotPkt> },
        { WPacketIdentifier.SEntitiesLoadedDelta, Deserialize<WSEntitiesLoadedDeltaPkt> },
        { WPacketIdentifier.SDefaultControllerState, Deserialize<WSDefaultControllerStatePkt> },
        { WPacketIdentifier.SChunkReliableUpdates, Deserialize<WSChunkReliableUpdatesPkt> },
        { WPacketIdentifier.SChatMessage, Deserialize<WSChatMessagePkt> },
        { WPacketIdentifier.SFullEntitiesSnapshot, Deserialize<WSFullEntitiesSnapshotPkt> },
        { WPacketIdentifier.SEntitySpawn, Deserialize<WSEntitySpawnPkt> },
        { WPacketIdentifier.SEntityKill, Deserialize<WSEntityKillPkt> },
        { WPacketIdentifier.SSetPlayerEntity, Deserialize<WSSetPlayerEntityPkt> },
        { WPacketIdentifier.SEntityTransformUpdate, Deserialize<WSEntityTransformUpdatePkt> },
        { WPacketIdentifier.SGenericUpdatesCollection, Deserialize<TickedPacketCollection> }
    };

    ///<summary> Reads out a packet type and then a packet that matches that type from a NetDataReader </summary>
    ///<returns> The deserialized packet </returns>
    public static INetPacketForClient DeserializeNextPacket(NetDataReader reader) {
        ushort packetTypeUShort = reader.GetUShort();
        if(!packetDeserializers.TryGetValue((WPacketIdentifier)packetTypeUShort, out var function)) {
            throw new Exception($"No handler for {(WPacketIdentifier)packetTypeUShort} (Code {packetTypeUShort})!");
        }
        return function(reader);
    }

    ///<summary> Either caches or immediatedly applies a packet based on its logic </summary>
    public static void ConsumePacket(int tick, INetPacketForClient packet) {
        if(packet.ShouldCache) {
            WCPacketCacheManager.CachePacket(tick, packet);
        } else {
            packet.ApplyOnClient(tick);
        }
    }

    ///<summary> Reads through next packet in NetDataReader, applying or caching it as per the packet's logic  </summary>
    public static void ConsumeNextPacket(int tick, NetDataReader reader) {
        INetPacketForClient packet = DeserializeNextPacket(reader);
        ConsumePacket(tick, packet);
    }

    ///<summary> Reads through an entire NetDataReader, applying or caching each packet read </summary>
    public static void ConsumeAllPackets(int tick, NetDataReader reader) {
        while(reader.AvailableBytes >= sizeof(ushort)) {
            ConsumeNextPacket(tick, reader);
        }
    }
}