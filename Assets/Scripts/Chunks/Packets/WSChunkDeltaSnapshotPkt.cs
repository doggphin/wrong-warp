using LiteNetLib.Utils;
using System.Collections.Generic;

namespace Networking.Shared {
    ///<summary> A collection of updates that have occured within a chunk across several ticks </summary>
    public class WSChunkDeltaSnapshotPkt : NetPacketForClient {
        public List<NetPacketForClient>[] generalUpdates;
        public Dictionary<int, List<NetPacketForClient>>[] entityUpdates;

        public override void Deserialize(NetDataReader reader) {
            generalUpdates = new List<NetPacketForClient>[WCommon.TICKS_PER_SNAPSHOT];
            entityUpdates = new Dictionary<int, List<NetPacketForClient>>[WCommon.TICKS_PER_SNAPSHOT];

            for(int i=0; i < WCommon.TICKS_PER_SNAPSHOT; i++) {
                generalUpdates[i] = new();
                entityUpdates[i] = new();
            }

            byte generalUpdatesExistFlags = reader.GetByte();

            for(int tick=0; tick < WCommon.TICKS_PER_SNAPSHOT; tick++) {
                // If bitflag for this tick is turned off, skip to the next one
                if((generalUpdatesExistFlags & (1 << tick)) == 0)
                    continue;
                    
                int numberOfGeneralUpdatesInTick = (int)reader.GetVarUInt();

                while(numberOfGeneralUpdatesInTick-- > 0) {
                    generalUpdates[tick].Add(WCPacketForClientUnpacker.DeserializeNextPacket(reader));
                }
            }

            byte entityUpdatesExistFlags = reader.GetByte();

            for (int tick = 0; tick < WCommon.TICKS_PER_SNAPSHOT; tick++) {
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
                        list.Add(WCPacketForClientUnpacker.DeserializeNextPacket(reader));
                    }
                }
            }
        }


        public override void Serialize(NetDataWriter writer) {
            // Put packet type
            writer.Put(WPacketIdentifier.SChunkDeltaSnapshot);

            // Get and store the total general packets in each tick
            // If there are any, the tick contains updates, so mark it in the flags
            int generalUpdatesExistInTickBitflags = 0;
            for(int i=0; i<WCommon.TICKS_PER_SNAPSHOT; i++) {
                if (generalUpdates[i].Count > 0)
                    generalUpdatesExistInTickBitflags |= 1 << i;
            }

            // Write the flags
            writer.Put((byte)generalUpdatesExistInTickBitflags);

            for(int i=0; i<WCommon.TICKS_PER_SNAPSHOT; i++) {

                // Serialize # of general updates only when there's more than 0; otherwise skip to next tick
                int generalUpdatesInTick = generalUpdates[i].Count;
                if (generalUpdatesInTick < 1)
                    continue;

                writer.PutVarUInt((uint)generalUpdatesInTick);

                // Write all the general updates
                foreach (INetSerializable update in generalUpdates[i]) {
                    update.Serialize(writer);
                }
            }

            // Get and store the total entities in each tick
            // If there are any, the tick contains updates, so mark it in the flags
            int tickContainsEntityUpdatesFlags = 0;
            int[] totalEntitiesInTicks = new int[WCommon.TICKS_PER_SNAPSHOT];
            for (int i=0; i<WCommon.TICKS_PER_SNAPSHOT; i++) {
                totalEntitiesInTicks[i] = entityUpdates[i].Keys.Count;
                if (totalEntitiesInTicks[i] > 0)
                    tickContainsEntityUpdatesFlags |= 1 << i;
            }

            // Write the flags
            writer.Put((byte)tickContainsEntityUpdatesFlags);

            // For each tick,
            for (int tick=0; tick<WCommon.TICKS_PER_SNAPSHOT; tick++) {

                // Serialize # of general updates only when there's more than 0; otherwise skip to next tick
                int entitiesInTick = totalEntitiesInTicks[tick];
                if (entitiesInTick < 1)
                    continue;

                writer.PutVarUInt((uint)entitiesInTick);

                // For each entity ID and list of updates
                foreach(KeyValuePair<int, List<NetPacketForClient>> entityIdAndUpdates in entityUpdates[tick]) {

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

        protected override void BroadcastApply(int tick)
        {
            for(int i=0; i<WCommon.TICKS_PER_SNAPSHOT; i++) {
                int offsetTick = tick - (WCommon.TICKS_PER_SNAPSHOT - i - 1);

                foreach(NetPacketForClient update in generalUpdates[i]) {
                    WCPacketForClientUnpacker.ConsumePacket(offsetTick, update);
                }

                foreach(var(entityId, updates) in entityUpdates[i]) {
                    
                    foreach(NetPacketForClient update in updates) {
                        var entityUpdate = (IEntityUpdate)update;
                        entityUpdate.CEntityId = entityId;
                        WCPacketForClientUnpacker.ConsumePacket(offsetTick, update);
                    }
                }
            }
        }
    }
}