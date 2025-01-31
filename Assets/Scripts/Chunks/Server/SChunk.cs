using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib.Utils;
using Mono.Cecil;
using Networking.Server;
using Networking.Shared;
using Unity.VisualScripting;
using UnityEngine;

public class SChunk {
    public readonly Vector2Int Coords;

    private HashSet<SEntity> entities = new();
    public IEnumerable<SEntity> GetEntities() => entities;

    private HashSet<SPlayer> playerObservers = new();
    public static Action<SChunk> StartExpiring;
    public static Action<SChunk> StopExpiring;
    
    // For all updates, naming convention is as follows --
    // U = Unreliable
    // R = Reliable
    // E = Entity
    // G = General

    // Local updates will only be shared with players in this chunk
    // Shared updates will be shared with players in all chunks within viewing radius of this chunk
    TickedEntitiesUpdates rLocalEUpdates = new();
    TickedEntitiesUpdates rSharedEUpdates = new();
    TickedEntitiesUpdates uSharedEUpdates = new();
    private NetDataWriter rLocalEUpdatesWriter = new();
    private NetDataWriter rSharedEUpdatesWriter = new();
    private NetDataWriter uSharedEUpdatesWriter = new();
    private bool isRSharedEUpdatesWritten, isUSharedEUpdatesWritten, isRLocalEUpdatesWritten;

    private NetDataWriter rSharedGUpdatesWriter = new();
    TickedPacketCollection rSharedGUpdates = new();
    private bool isRSharedGUpdatesWritten;
    
    private readonly ITickedContainer[] tickedContainers;

    private NetDataWriter u3x3UpdatesWriter = new();
    private NetDataWriter r3x3UpdatesWriter = new();
    private bool isU3x3UpdatesWritten, isR3x3UpdatesWritten;

    private readonly NetDataWriter[] writers;

    private bool hasAlreadyBroadcastHasAnUpdate;
    public static Action<SChunk> HasAnUpdate;

    public static Action<SEntity> UnloadEntity;
    
    public SChunk(Vector2Int coords) {
        Coords = coords;
        tickedContainers = new ITickedContainer[]{ rLocalEUpdates, uSharedEUpdates, rSharedEUpdates, rSharedGUpdates };
        writers = new NetDataWriter[]{ rLocalEUpdatesWriter, uSharedEUpdatesWriter, rSharedEUpdatesWriter, r3x3UpdatesWriter, u3x3UpdatesWriter };
    }
    

    /// <summary> Appends a TickedEntitiesUpdates to a writer if there's any data </summary>
    /// <param name="appendTo"> The NetDataWriter to append to </param>
    /// <param name="tickedEntitiesUpdates"> The TickedEntitiesUpdates to append </param>
    /// <param name="cachedWriter"> A NetDataWriter that may already have the serialized version of this TickedEntitiesUpdates </param>
    /// <param name="isAlreadyWritten"> A reference to whether a check representing if the TickedEntitiesUpdates has already been serialized </param>
    /// <returns> Whether any data was actually added </returns>
    private bool TryAppendEUpdates<T>(NetDataWriter appendTo, T tickedEntitiesUpdates, NetDataWriter cachedWriter, ref bool isAlreadyWritten) where T : INetSerializable, ITickedContainer {
        if(!tickedEntitiesUpdates.HasData)
            return false;

        if(!isAlreadyWritten) {
            tickedEntitiesUpdates.Serialize(cachedWriter);
        }

        appendTo.Append(cachedWriter);
        return true;
    }

    private bool TryAppendRSharedEUpdates(NetDataWriter appendTo) =>
        TryAppendEUpdates(appendTo, rSharedEUpdates, rSharedEUpdatesWriter, ref isRSharedEUpdatesWritten);
    private bool TryAppendRSharedGUpdates(NetDataWriter appendTo) =>
        TryAppendEUpdates(appendTo, rSharedGUpdates, rSharedGUpdatesWriter, ref isRSharedGUpdatesWritten);
    private bool TryAppendUSharedEUpdates(NetDataWriter appendTo) =>
        TryAppendEUpdates(appendTo, uSharedEUpdates, uSharedEUpdatesWriter, ref isUSharedEUpdatesWritten);

    private bool TryAppendRLocalEUpdates(NetDataWriter appendTo) =>
        TryAppendEUpdates(appendTo, rLocalEUpdates, rLocalEUpdatesWriter, ref isRLocalEUpdatesWritten);

    /// <summary>
    /// Collects updates from nearby chunks, and along with local updates, merges them all into a writer for sending to players inhabiting this chunk
    /// </summary>
    /// <param name="surroundingChunks"> All chunks surrounding this chunk, including self </param>
    /// <param name="outWriter"> The writer that will contain all updates </param>
    /// <param name="writer3x3"> The (possibly) cached writer that may already contain all 3x3 updates already in this chunk </param>
    /// <param name="isAlreadyWritten"> Whether the 3x3 updates of this update type have already been written </param>
    /// <param name="tryAppendSharedEUpdatesFunc"> The function used to get shared entity updates from other chunks </param>
    /// <param name="tryAppendSharedEUpdatesFunc"> The function used to get shared general updates from other chunks. Can be null (FOR NOW) </param>
    /// <param name="tryAppendLocalEUpdatesFunc"> The function used to get local updates from this chunk. Can be null (FOR NOW) </param>
    /// <returns> Whether any data was actually retrieved </returns>
    private bool TryWrite3x3Updates(HashSet<SChunk> surroundingChunks, out NetDataWriter outWriter, NetDataWriter writer3x3, ref bool isAlreadyWritten,
    Func<SChunk, NetDataWriter, bool> tryAppendSharedEUpdatesFunc,
    Func<SChunk, NetDataWriter, bool> tryAppendSharedGUpdatesFunc,
    Func<NetDataWriter, bool> tryAppendLocalEUpdatesFunc) {
        bool anyDataWritten = false;
    
        // If 3x3 has already been written, 
        if(isAlreadyWritten) {
            Debug.Log("Was already written!");
            anyDataWritten = writer3x3.Length > 0;
        } else {
            // Add updates from all chunks in view
            foreach(SChunk surroundingChunk in surroundingChunks) {
                anyDataWritten |= tryAppendSharedEUpdatesFunc(surroundingChunk, writer3x3);
                if(tryAppendSharedGUpdatesFunc != null) {
                    anyDataWritten |= tryAppendSharedGUpdatesFunc(surroundingChunk, writer3x3);
                }      
            }

            // Add local updates
            if(tryAppendLocalEUpdatesFunc != null) {
                anyDataWritten |= tryAppendLocalEUpdatesFunc(writer3x3);
            }

            if(anyDataWritten) {
                BroadcastHasAnUpdate();
            }

            isAlreadyWritten = true;
        }

        outWriter = anyDataWritten ? writer3x3 : null;
        return anyDataWritten;
    }

    public bool TryGet3x3ReliableUpdates(HashSet<SChunk> surroundingChunks, out NetDataWriter writer) =>
        TryWrite3x3Updates(surroundingChunks, out writer, r3x3UpdatesWriter, ref isR3x3UpdatesWritten,
            (chunk, writer) => chunk.TryAppendRSharedEUpdates(writer),
            (chunk, writer) => chunk.TryAppendRSharedGUpdates(writer),
            TryAppendRLocalEUpdates);
    public bool TryGet3x3UnreliableUpdates(HashSet<SChunk> surroundingChunks, out NetDataWriter writer) =>
        TryWrite3x3Updates(surroundingChunks, out writer, u3x3UpdatesWriter, ref isU3x3UpdatesWritten,
            (chunk, writer) => chunk.TryAppendUSharedEUpdates(writer),
            null,
            null);


    public void ResetUpdates() {
        Debug.Log("Resetting!");
        foreach(var writer in writers)
            writer.Reset();

        foreach(var container in tickedContainers)
            container.Reset();

        isRSharedEUpdatesWritten = isUSharedEUpdatesWritten = isRLocalEUpdatesWritten = 
        isRSharedGUpdatesWritten =
        isU3x3UpdatesWritten = isR3x3UpdatesWritten =
        hasAlreadyBroadcastHasAnUpdate = false;
    }


    public SFullEntitiesSnapshotPkt GetFullEntitiesSnapshot(int tick) {
        return new() {
            isFullReset = true,
            entities = entities.Select(entity => entity.GetSerializedEntity(tick)).ToArray()
        };
    }
    

    public void AddEntity(SEntity entity) {
        if(!entities.Add(entity))
            return;

        entity.PushUnreliableUpdate += AddUnreliableEntityUpdate;
    }


    public void RemoveEntity(SEntity entity) {
        if(!entities.Remove(entity))
            return;

        entity.PushUnreliableUpdate -= AddUnreliableEntityUpdate;
    }


    bool hasAddedFirstPlayer = false;
    public void AddPlayer(SPlayer player) {
        if(playerObservers.Count == 0) {
            if(hasAddedFirstPlayer) {
                StopExpiring?.Invoke(this);
            } else {
                // Don't bother unmarking for unloading on the first player being added
                hasAddedFirstPlayer = true;
            }  
        }

        playerObservers.Add(player);
    }


    public void RemovePlayer(SPlayer player) {
        playerObservers.Remove(player);

        if(playerObservers.Count == 0) {
            StartExpiring?.Invoke(this);
        }
    }

    
    private void AddEntityUpdate(SEntity entity, TickedEntitiesUpdates tickedEntitiesUpdates, BasePacket packet) {
        if(playerObservers.Count > 0) {
            tickedEntitiesUpdates.Add(SNetManager.Tick, entity.Id, packet);
            BroadcastHasAnUpdate();
        }
    }

    ///<summary> Notifies all players in this chunk that an entity has entered their render distance </summary>
    public void AddEntityIntoRenderDistance(SEntity entity) {
        Debug.Log($"Notifying players in {Coords} to load {entity}!");
        AddEntityUpdate(
            entity,
            rLocalEUpdates, 
            new SEntitySpawnPkt() {
                entity = entity.GetSerializedEntity(SNetManager.Tick),
                reason = WEntitySpawnReason.Load 
            });
    }

    ///<summary> Notifies all players in this chunk that an entity has left their render distance </summary>
    public void RemoveEntityFromRenderDistance(SEntity entity) {
        Debug.Log($"Notifying players in {Coords} to unload {entity}!");
        AddEntityUpdate(
            entity,
            rLocalEUpdates, 
            new SEntityKillPkt() {
                entityId = entity.Id,
                reason = WEntityKillReason.Unload 
            });
    }


    private void AddUnreliableEntityUpdate(SEntity entity, BasePacket packet) {
        AddEntityUpdate(entity, uSharedEUpdates, packet);
        Debug.Log($"Adding a {packet} for {entity}!");
    }


    public void AddReliableGeneralUpdate(BasePacket packet) {
        rSharedGUpdates.AddPacket(SNetManager.Tick, packet);
        BroadcastHasAnUpdate();
    }


    private void BroadcastHasAnUpdate() {
        if(!hasAlreadyBroadcastHasAnUpdate) {
            hasAlreadyBroadcastHasAnUpdate = true;
            HasAnUpdate?.Invoke(this);
        }
    }


    public void Unload() {
        foreach(var entity in entities.ToList()) {
            UnloadEntity?.Invoke(entity);
        }
    }
}