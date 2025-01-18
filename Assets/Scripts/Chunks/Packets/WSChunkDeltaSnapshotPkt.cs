using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

using Networking.Server;
using Networking.Client;

namespace Networking.Shared {
    public class WSChunkDeltaSnapshotPkt : INetSerializable {
        public List<INetSerializable>[] generalUpdates;
        public Dictionary<int, List<INetSerializable>>[] entityUpdates;

        // This is set on the client end
        public int c_startTick;

        public void Deserialize(NetDataReader reader) {
            generalUpdates = new List<INetSerializable>[WCommon.TICKS_PER_SNAPSHOT];
            entityUpdates = new Dictionary<int, List<INetSerializable>>[WCommon.TICKS_PER_SNAPSHOT];

            for(int i=0; i < WCommon.TICKS_PER_SNAPSHOT; i++) {
                generalUpdates[i] = new();
                entityUpdates[i] = new();
            }

            byte generalUpdatesExistFlags = reader.GetByte();

            for(int tick=0; tick < WCommon.TICKS_PER_SNAPSHOT; tick++) {
                // If bitflag for this tick is turned off, skip to the next one
                if((generalUpdatesExistFlags & (1 << tick)) == 0) {
                    continue;
                }
                    
                int numberOfGeneralUpdatesInTick = reader.GetUShort();

                while(numberOfGeneralUpdatesInTick-- > 0) {
                    WPacketType packetType = reader.GetPacketType();
                    WCNetClient.Instance.ConsumeGeneralUpdate(
                        c_startTick + tick,
                        packetType,
                        reader);
                }
            }

            byte entityUpdatesExistFlags = reader.GetByte();

            for (int tick = 0; tick < WCommon.TICKS_PER_SNAPSHOT; tick++) {
                // If bitflag for this tick is turned off, skip to the next one
                if ((entityUpdatesExistFlags & (1 << tick)) == 0)
                    continue;
                    
                int numberOfEntitiesInTick = reader.GetUShort();

                while (numberOfEntitiesInTick-- > 0) {
                    int entityId = reader.GetInt();
                    int amountOfUpdatesForEntity = reader.GetUShort();

                    while(amountOfUpdatesForEntity-- > 0) {
                        WPacketType packetType = reader.GetPacketType();

                        WCNetClient.Instance.ConsumeEntityUpdate(
                            c_startTick + tick,
                            entityId,
                            packetType,
                            reader);
                    }
                }
            }
        }


        public void Serialize(NetDataWriter writer) {
            // Put packet type
            writer.Put(WPacketType.SChunkDeltaSnapshot);

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
                if (generalUpdatesInTick > 0)
                    writer.Put((ushort)generalUpdatesInTick);
                else
                    continue;

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
                if (entitiesInTick > 0)
                    writer.Put((ushort)entitiesInTick);
                else
                    continue;

                // For each entity ID and list of updates
                foreach(KeyValuePair<int, List<INetSerializable>> entityIdAndUpdates in entityUpdates[tick]) {

                    // Put entity ID
                    int entityId = entityIdAndUpdates.Key;
                    writer.Put(entityId);

                    // Put # of updates
                    int amountOfUpdates = entityIdAndUpdates.Value.Count;
                    writer.Put((ushort)amountOfUpdates);

                    // Then put all of its updates
                    foreach(var entityUpdatePacket in entityIdAndUpdates.Value) {
                        // what the fuck is this little line of code? consider deleting 12/26/24
                        //Vector3 position = ((WSEntityTransformUpdatePkt)entityUpdatePacket).transform.position.GetValueOrDefault(Vector3.zero);
                        
                        entityUpdatePacket.Serialize(writer);
                    }
                }
            }
        }
    }
}