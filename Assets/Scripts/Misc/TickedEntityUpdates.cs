using System.Collections.Generic;
using LiteNetLib.Utils;
using Networking.Shared;


public class TickedEntitiesUpdates : BasePacket, ITickedContainer {
    private Dictionary<int, Dictionary<int, List<BasePacket>>> ticks = new();
    public bool HasData { get => ticks.Count > 0; }

    public void Add(int tick, int entityId, BasePacket update) {
        if(!ticks.TryGetValue(tick, out Dictionary<int, List<BasePacket>> entities)) {
            entities = new(1);
            ticks[tick] = entities;
        }

        if(!entities.TryGetValue(entityId, out List<BasePacket> updates)) {
            updates = new(1);
            entities[entityId] = updates;
        }

        updates.Add(update);
    }


    public void Reset() {  
        ticks.Clear();
    }


    public override void Deserialize(NetDataReader reader) {
        int ticksCount = (int)reader.GetVarUInt();
        ticks = new(ticksCount);

        for(int i=0; i<ticksCount; i++) {
            int tick = reader.GetInt();
            int entitiesCount = (int)reader.GetVarUInt();
            Dictionary<int, List<BasePacket>> entities = new(entitiesCount);
            ticks[tick] = entities;
            for(int j=0; j<entitiesCount; j++) {
                int entityId = reader.GetInt();
                int updatesCount = (int)reader.GetVarUInt();
                List<BasePacket> updates = new(updatesCount);
                entities[entityId] = updates;
                for(int k=0; k<updatesCount; k++) {
                    BasePacket update = CPacketUnpacker.DeserializeNextPacket(reader);
                    updates.Add(update);
                }}}}


    public override void Serialize(NetDataWriter writer) {
        writer.Put(PacketIdentifier.STickedEntityUpdates);

        writer.PutVarUInt(ticks.Count);
        foreach(var(tick, entityUpdates) in ticks) {
            writer.Put(tick);
            writer.PutVarUInt(entityUpdates.Count);
            foreach(var(entityId, updates) in entityUpdates) {
                writer.Put(entityId);
                writer.PutVarUInt(updates.Count);
                foreach(var update in updates) {
                    update.Serialize(writer);
                }}}}


    public override bool ShouldCache => false;
    protected override void OverridableBroadcastApply(int tick)
    {
        foreach(var(onTick, entityUpdates) in ticks) {
            foreach(var(entityId, updates) in entityUpdates) {
                foreach(var update in updates) {
                    if(update is IEntityUpdate iEntityUpdate) {
                        iEntityUpdate.CEntityId = entityId;
                    }
                    CPacketUnpacker.ConsumePacket(onTick, update);
                }
            }
        }
    }
}