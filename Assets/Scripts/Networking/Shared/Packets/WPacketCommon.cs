using LiteNetLib.Utils;
using Mono.Cecil;
using Networking;
using Networking.Shared;
using UnityEngine;


namespace Networking.Shared {
    public enum WPacketType : ushort {
        Unimplemented,

        CJoinRequest,
        SJoinAccept,
        SJoinDenied,
        CInputs,
        CGroupedInputs,
        SChunkDeltaSnapshot,
        SEntityTransformUpdate,
        SEntityKill,
        SEntitySpawn,
        SFullEntitiesSnapshot,
        SEntitiesLoadedDelta,
        SDefaultControllerState,
        CChatMessage,
        SChatMessage,
        SChunkReliableUpdates,
        SInventoryDeltaCollection,
        SSetPlayerEntity,
        SAddInventory,
        SRemoveInventory,
        SSetPersonalInventoryId,
    }
}