using LiteNetLib.Utils;
using System.Collections.Generic;
using System.Diagnostics;

namespace Networking.Shared {
    ///<summary> A collection of updates that have occured within a chunk across several ticks </summary>
    public class SChunkDeltaSnapshotPkt : BasePacket {
        public List<BasePacket>[] generalUpdates;
        public Dictionary<int, List<BasePacket>>[] entityUpdates;

        public override void Deserialize(NetDataReader reader) {
            generalUpdates = new List<BasePacket>[NetCommon.TICKS_PER_SNAPSHOT];
            entityUpdates = new Dictionary<int, List<BasePacket>>[NetCommon.TICKS_PER_SNAPSHOT];

            for(int i=0; i < NetCommon.TICKS_PER_SNAPSHOT; i++) {
                generalUpdates[i] = new();
                entityUpdates[i] = new();
            }

            byte generalUpdatesExistFlags = reader.GetByte();

            for(int tick=0; tick < NetCommon.TICKS_PER_SNAPSHOT; tick++) {
                // If bitflag for this tick is turned off, skip to the next one
                if((generalUpdatesExistFlags & (1 << tick)) == 0)
                    continue;
                    
                int numberOfGeneralUpdatesInTick = (int)reader.GetVarUInt();

                while(numberOfGeneralUpdatesInTick-- > 0) {
                    generalUpdates[tick].Add(CPacketUnpacker.DeserializeNextPacket(reader));
                }
            }

            byte entityUpdatesExistFlags = reader.GetByte();

            for (int tick = 0; tick < NetCommon.TICKS_PER_SNAPSHOT; tick++) {
                // If bitflag for this tick is turned off, skip to the next one
                if ((entityUpdatesExistFlags & (1 << tick)) == 0)
                    continue;
                    
                int numberOfEntitiesInTick = (int)reader.GetVarUInt();
                while (numberOfEntitiesInTick-- > 0) {
                    int entityId = reader.GetInt();
                    int amountOfUpdatesForEntity = (int)reader.GetVarUInt();

                    while(amountOfUpdatesForEntity-- > 0) {
                        if(!entityUpdates[tick].TryGetValue(entityId, out var list)) {
                            list = new();
                            entityUpdates[tick][entityId] = list;
                        }
                        list.Add(CPacketUnpacker.DeserializeNextPacket(reader));
                    }
                }
            }
        }


        public override void Serialize(NetDataWriter writer) {
            // Put packet type
            writer.Put(PacketIdentifier.SChunkDeltaSnapshot);

            // Get and store the total general packets in each tick
            // If there are any, the tick contains updates, so mark it in the flags
            int generalUpdatesExistInTickBitflags = 0;
            for(int i=0; i<NetCommon.TICKS_PER_SNAPSHOT; i++) {
                if (generalUpdates[i].Count > 0)
                    generalUpdatesExistInTickBitflags |= 1 << i;
            }

            // Write the flags
            writer.Put((byte)generalUpdatesExistInTickBitflags);

            for(int i=0; i<NetCommon.TICKS_PER_SNAPSHOT; i++) {

                // Serialize # of general updates only when there's more than 0; otherwise skip to next tick
                int generalUpdatesInTick = generalUpdates[i].Count;
                if (generalUpdatesInTick < 1)
                    continue;

                writer.PutVarUInt((uint)generalUpdatesInTick);

                // Write all the general updates
                foreach (BasePacket update in generalUpdates[i]) {
                    update.Serialize(writer);
                }
            }

            // Get and store the total entities in each tick
            // If there are any, the tick contains updates, so mark it in the flags
            int tickContainsEntityUpdatesFlags = 0;
            int[] totalEntitiesInTicks = new int[NetCommon.TICKS_PER_SNAPSHOT];
            for (int i=0; i<NetCommon.TICKS_PER_SNAPSHOT; i++) {
                totalEntitiesInTicks[i] = entityUpdates[i].Keys.Count;
                if (totalEntitiesInTicks[i] > 0)
                    tickContainsEntityUpdatesFlags |= 1 << i;
            }

            // Write the flags
            writer.Put((byte)tickContainsEntityUpdatesFlags);

            // For each tick,
            for (int tick=0; tick<NetCommon.TICKS_PER_SNAPSHOT; tick++) {

                // Serialize # of general updates only when there's more than 0; otherwise skip to next tick
                int entitiesInTick = totalEntitiesInTicks[tick];
                if (entitiesInTick < 1)
                    continue;

                writer.PutVarUInt((uint)entitiesInTick);

                // For each entity ID and list of updates
                foreach(KeyValuePair<int, List<BasePacket>> entityIdAndUpdates in entityUpdates[tick]) {

                    // Put entity ID
                    int entityId = entityIdAndUpdates.Key;
                    writer.Put(entityId);

                    // Put # of updates
                    int amountOfUpdates = entityIdAndUpdates.Value.Count;
                    writer.PutVarUInt((uint)amountOfUpdates);

                    // Then put all of its updates
                    foreach(var entityUpdatePacket in entityIdAndUpdates.Value) {
                        // what the fuck is this little line of code? consider deleting 12/26/24
                        //Vector3 position = ((WSEntityTransformUpdatePkt)entityUpdatePacket).transform.position.GetValueOrDefault(Vector3.zero);
                        
                        entityUpdatePacket.Serialize(writer);
                    }
                }
            }
        }

        public override bool ShouldCache => false;

        protected override void OverridableBroadcastApply(int tick)
        {
            for(int i=0; i<NetCommon.TICKS_PER_SNAPSHOT; i++) {
                int offsetTick = tick - (NetCommon.TICKS_PER_SNAPSHOT - i - 1);

                foreach(BasePacket update in generalUpdates[i]) {
                    CPacketUnpacker.ConsumePacket(offsetTick, update);
                }

                foreach(var(entityId, updates) in entityUpdates[i]) {
                    
                    foreach(BasePacket update in updates) {
                        // only set entityId of base packet if it inherits from IEntityUpdate
                        // this is smelly as fuck
                        UnityEngine.Debug.Log($"Received an {update.GetType().BaseType} packet with entity ID {entityId}");
                        if(update is IEntityUpdate iEntityUpdate) {
                            UnityEngine.Debug.Log("It is an entity update!");
                            iEntityUpdate.CEntityId = entityId;
                        } else {
                            UnityEngine.Debug.Log("It is not an entity update!");
                        }
                        CPacketUnpacker.ConsumePacket(offsetTick, update);
                    }
                }
            }
        }
    }
}