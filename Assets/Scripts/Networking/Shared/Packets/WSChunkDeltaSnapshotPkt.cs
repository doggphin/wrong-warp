using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

using Networking.Server;

namespace Networking.Shared {
    public class WSChunkDeltaSnapshotPkt : INetSerializable {
        public List<INetSerializable>[] s_generalUpdates;
        public Dictionary<int, List<INetSerializable>>[] s_entityUpdates;

        public int startTick;
        // Tick, Packet Type, Reader
        public Func<int, WPacketType, NetDataReader, bool> c_generalHandler;
        // Tick, Entity ID, Packet Type, Reader
        public Func<int, int, WPacketType, NetDataReader, bool> c_entityHandler;

        public void Deserialize(NetDataReader reader) {
            startTick = startTick - WCommon.TICKS_PER_SNAPSHOT;

            s_generalUpdates = new List<INetSerializable>[WCommon.TICKS_PER_SNAPSHOT];
            s_entityUpdates = new Dictionary<int, List<INetSerializable>>[WCommon.TICKS_PER_SNAPSHOT];

            for(int i=0; i < WCommon.TICKS_PER_SNAPSHOT; i++) {
                s_generalUpdates[i] = new();
                s_entityUpdates[i] = new();
            }

            byte generalUpdatesExistFlags = reader.GetByte();

            for(int tick=0; tick < WCommon.TICKS_PER_SNAPSHOT; tick++) {
                // If bitflag for this tick is turned off, skip to the next one
                if((generalUpdatesExistFlags & (1 << tick)) == 0) {
                    continue;
                }
                    
                int numberOfGeneralUpdatesInTick = reader.GetUShort();

                while(numberOfGeneralUpdatesInTick-- > 0) {
                    WPacketType packetType = (WPacketType)reader.GetUShort();
                    c_generalHandler(
                        startTick + tick,
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
                        WPacketType packetType = (WPacketType)reader.GetUShort();

                        c_entityHandler(
                            startTick + tick,
                            entityId,
                            packetType,
                            reader);
                    }
                }
            }
        }


        public void Serialize(NetDataWriter writer) {
            // Put packet type
            writer.Put((ushort)WPacketType.SChunkDeltaSnapshot);

            // Get and store the total general packets in each tick
            // If there are any, the tick contains updates, so mark it in the flags
            int generalUpdatesExistInTickBitflags = 0;
            for(int i=0; i<WCommon.TICKS_PER_SNAPSHOT; i++) {
                if (s_generalUpdates[i].Count > 0)
                    generalUpdatesExistInTickBitflags |= 1 << i;
            }

            // Write the flags
            writer.Put((byte)generalUpdatesExistInTickBitflags);

            for(int i=0; i<WCommon.TICKS_PER_SNAPSHOT; i++) {

                // Serialize # of general updates only when there's more than 0; otherwise skip to next tick
                int generalUpdatesInTick = s_generalUpdates[i].Count;
                if (generalUpdatesInTick > 0)
                    writer.Put((ushort)generalUpdatesInTick);
                else
                    continue;

                // Write all the general updates
                foreach (INetSerializable update in s_generalUpdates[i]) {
                    update.Serialize(writer);
                }
            }

            // Get and store the total entities in each tick
            // If there are any, the tick contains updates, so mark it in the flags
            int tickContainsEntityUpdatesFlags = 0;
            int[] totalEntitiesInTicks = new int[WCommon.TICKS_PER_SNAPSHOT];
            for (int i=0; i<WCommon.TICKS_PER_SNAPSHOT; i++) {
                totalEntitiesInTicks[i] = s_entityUpdates[i].Keys.Count;

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
                foreach(KeyValuePair<int, List<INetSerializable>> entityIdAndUpdates in s_entityUpdates[tick]) {

                    // Put entity ID
                    int entityId = entityIdAndUpdates.Key;
                    writer.Put(entityId);

                    // Put # of updates
                    int amountOfUpdates = entityIdAndUpdates.Value.Count;
                    writer.Put((ushort)amountOfUpdates);

                    // Then put all of its updates
                    foreach(var entityUpdatePacket in entityIdAndUpdates.Value) {
                        Vector3 position = ((WSEntityTransformUpdatePkt)entityUpdatePacket).transform.position.GetValueOrDefault(Vector3.zero);
                        
                        entityUpdatePacket.Serialize(writer);
                    }
                }
            }
        }
    }
}