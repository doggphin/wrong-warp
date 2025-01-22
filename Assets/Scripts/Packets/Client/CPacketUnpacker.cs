using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using Networking.Shared;

public class CPacketUnpacker : PacketUnpacker<CPacketUnpacker>
{
    private readonly Dictionary<PacketIdentifier, Func<NetDataReader, BasePacket>> packetDeserializers = new() {
        { PacketIdentifier.SJoinAccept, Deserialize<SJoinAcceptPkt> },
        { PacketIdentifier.SChunkDeltaSnapshot, Deserialize<SChunkDeltaSnapshotPkt> },
        { PacketIdentifier.SEntitiesLoadedDelta, Deserialize<WSEntitiesLoadedDeltaPkt> },
        { PacketIdentifier.SDefaultControllerState, Deserialize<SDefaultControllerStatePkt> },
        { PacketIdentifier.SChunkReliableUpdates, Deserialize<SChunkReliableUpdatesPkt> },
        { PacketIdentifier.SChatMessage, Deserialize<SChatMessagePkt> },
        { PacketIdentifier.SFullEntitiesSnapshot, Deserialize<WSFullEntitiesSnapshotPkt> },
        { PacketIdentifier.SEntitySpawn, Deserialize<WSEntitySpawnPkt> },
        { PacketIdentifier.SEntityKill, Deserialize<WSEntityKillPkt> },
        { PacketIdentifier.SSetPlayerEntity, Deserialize<WSSetPlayerEntityPkt> },
        { PacketIdentifier.SEntityTransformUpdate, Deserialize<WSEntityTransformUpdatePkt> },
        { PacketIdentifier.SGenericUpdatesCollection, Deserialize<TickedPacketCollection> }
    };

    protected override Dictionary<PacketIdentifier, Func<NetDataReader, BasePacket>> PacketDeserializers { get => packetDeserializers; }
}