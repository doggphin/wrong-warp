using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using Networking.Shared;

public class SPacketUnpacker : PacketUnpacker<SPacketUnpacker>
{
    private readonly Dictionary<PacketIdentifier, Func<NetDataReader, BasePacket>> packetDeserializers = new() {
        { PacketIdentifier.CJoinRequest, Deserialize<CJoinRequestPkt> },
        { PacketIdentifier.CGroupedInputs, Deserialize<CGroupedInputsPkt> },
        { PacketIdentifier.CMoveSlotRequest, Deserialize<CMoveSlotRequest> },
        { PacketIdentifier.CDropSlotRequest, Deserialize<CDropSlotRequest> },
    };

    protected override Dictionary<PacketIdentifier, Func<NetDataReader, BasePacket>> PacketDeserializers { get => packetDeserializers; }
}