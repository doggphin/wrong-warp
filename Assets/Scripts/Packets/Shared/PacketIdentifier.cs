namespace Networking.Shared {
    public enum PacketIdentifier : ushort {
        Unimplemented = 0,

        CJoinRequest = 1,
        SJoinAccept = 2,
        SJoinDenied = 3,

        CGroupedInputs = 5,
        SChunkDeltaSnapshot = 6,
        SEntityTransformUpdate = 7,
        SEntityKill = 8,
        SEntitySpawn = 9,
        SFullEntitiesSnapshot = 10,
        SEntitiesLoadedDelta = 11,
        SDefaultControllerState = 12,
        CChatMessage = 13,
        SChatMessage = 14,
        SChunkReliableUpdates = 15,
        SSetPlayerEntity = 17,


        SInventoryDeltaCollection = 16,
        SAddInventory = 18,
        SRemoveInventory = 19,
        SSetPersonalInventoryId = 20,
        CMoveSlotRequest = 21,
        CDropSlotRequest = 22,

        STickedPacketCollection = 23,
        STickedEntityUpdates = 24,
    }
}