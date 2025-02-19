using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using Networking.Shared;

public class CPacketUnpacker : BasePacketUnpacker<CPacketUnpacker>
{
    private readonly Dictionary<PacketIdentifier, Func<NetDataReader, BasePacket>> packetDeserializers = new() {
        { PacketIdentifier.SJoinAccept, Deserialize<SJoinAcceptPkt> },
        //{ PacketIdentifier.SChunkDeltaSnapshot, Deserialize<SChunkDeltaSnapshotPkt> },
        //{ PacketIdentifier.SEntitiesLoadedDelta, Deserialize<SEntitiesLoadedDeltaPkt> },
        { PacketIdentifier.SDefaultControllerState, Deserialize<SDefaultControllerStatePkt> },
        //{ PacketIdentifier.SChunkReliableUpdates, Deserialize<SChunkReliableUpdatesPkt> },
        { PacketIdentifier.SChatMessage, Deserialize<SChatMessagePkt> },
        { PacketIdentifier.SFullEntitiesSnapshot, Deserialize<SFullEntitiesSnapshotPkt> },
        { PacketIdentifier.SEntitySpawn, Deserialize<SEntitySpawnPkt> },
        { PacketIdentifier.SEntityKill, Deserialize<SEntityKillPkt> },
        { PacketIdentifier.SSetPlayerEntity, Deserialize<SSetPlayerEntityPkt> },
        { PacketIdentifier.SEntityTransformUpdate, Deserialize<SEntityTransformUpdatePkt> },
        { PacketIdentifier.STickedPacketCollection, Deserialize<TickedPacketCollection> },
        { PacketIdentifier.STickedEntityUpdates, Deserialize<TickedEntitiesUpdates> },
        { PacketIdentifier.SAddInventory, Deserialize<SAddInventoryPkt> },
        { PacketIdentifier.SInventoryDeltas, Deserialize<SInventoryDeltasPkt> }
    };

    protected override Dictionary<PacketIdentifier, Func<NetDataReader, BasePacket>> PacketDeserializers { get => packetDeserializers; }
}