namespace Networking.Shared {
    public enum PacketIdentifier : ushort {
        Unimplemented = 0,

        // Connections
        CJoinRequest = 1,
        SJoinAccept = 2,
        SJoinDenied = 3,

        // Inputs
        CGroupedInputs = 5,

        // Entities and chunks
        SChunkDeltaSnapshot = 6,
        SEntityTransformUpdate = 7,
        SEntityKill = 8,
        SEntitySpawn = 9,
        SFullEntitiesSnapshot = 10,
        SEntitiesLoadedDelta = 11,
        SDefaultControllerState = 12,
        SChunkReliableUpdates = 15,
        SSetPlayerEntity = 17,
        STickedEntityUpdates = 24,

        // Chat
        CChatMessage = 13,
        SChatMessage = 14,


        // Inventory stuff
        SInventoryDeltas = 16,
        SAddInventory = 18,
        SRemoveInventory = 19,
        SSetPersonalInventoryId = 20,
        CMoveSlotRequest = 21,
        CDropSlotRequest = 22,

        // Generic packet collection
        STickedPacketCollection = 23,

        // Interactables
        STakeableStackSizeUpdate = 25,
    }
}