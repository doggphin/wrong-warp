using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using TMPro.EditorUtilities;
using Unity.VisualScripting;
using UnityEngine;

namespace Networking.Shared {
    public class WSChunkSnapshotPkt : INetSerializable {
        public List<INetSerializable>[] s_generalUpdates;
        public Dictionary<int, List<INetSerializable>>[] s_entityUpdates;

        public int c_headerTick;
        // Tick, Packet Type, Reader
        public Func<int, WPacketType, NetDataReader, bool> c_generalHandler;
        // Tick, Entity ID, Packet Type, Reader
        public Func<int, int, WPacketType, NetDataReader, bool> c_entityHandler;

        public void Deserialize(NetDataReader reader) {
            s_generalUpdates = new List<INetSerializable>[WNetCommon.TICKS_PER_SNAPSHOT];
            s_entityUpdates = new Dictionary<int, List<INetSerializable>>[WNetCommon.TICKS_PER_SNAPSHOT];

            for(int i=0; i < WNetCommon.TICKS_PER_SNAPSHOT; i++) {
                s_generalUpdates[i] = new();
                s_entityUpdates[i] = new();
            }

            byte generalUpdatesExistFlags = reader.GetByte();

            for(int i=0; i < WNetCommon.TICKS_PER_SNAPSHOT; i++) {
                // If bitflag for this tick is turned off, skip to the next one
                if((generalUpdatesExistFlags & (1 << i)) == 0) {
                    continue;
                }
                    
                int numberOfGeneralUpdatesInTick = reader.GetUShort();

                //Debug.Log($"In tick {c_headerTick - WNetCommon.TICKS_PER_SNAPSHOT + i}, there are {numberOfGeneralUpdatesInTick} general updates.");

                while(numberOfGeneralUpdatesInTick-- > 0) {
                    WPacketType packetType = (WPacketType)reader.GetUShort();
                    c_generalHandler(
                        c_headerTick - WNetCommon.TICKS_PER_SNAPSHOT + i,
                        packetType,
                        reader);
                }
            }

            byte entityUpdatesExistFlags = reader.GetByte();

            for (int i = 0; i < WNetCommon.TICKS_PER_SNAPSHOT; i++) {
                // If bitflag for this tick is turned off, skip to the next one
                if ((entityUpdatesExistFlags & (1 << i)) == 0)
                    continue;
                    
                int numberOfEntitiesInTick = reader.GetUShort();
                Debug.Log(numberOfEntitiesInTick);

                //Debug.Log($"In tick {c_headerTick - WNetCommon.TICKS_PER_SNAPSHOT + i}, there are {numberOfEntityUpdatesInTick} entity updates.");
                while (numberOfEntitiesInTick-- > 0) {
                    int entityId = reader.GetInt();
                    int amountOfUpdatesForEntity = reader.GetUShort();

                    while(amountOfUpdatesForEntity-- > 0) {
                        WPacketType packetType = (WPacketType)reader.GetUShort();

                        c_entityHandler(
                            c_headerTick - WNetCommon.TICKS_PER_SNAPSHOT + i,
                            entityId,
                            packetType,
                            reader);
                    }
                }
            }
        }


        public void Serialize(NetDataWriter writer) {
            // Put packet type
            writer.Put((ushort)WPacketType.SChunkSnapshot);

            // Get and store the total general packets in each tick
            // If there are any, the tick contains updates, so mark it in the flags
            int generalUpdatesExistInTickBitflags = 0;
            for(int i=0; i<WNetCommon.TICKS_PER_SNAPSHOT; i++) {
                if (s_generalUpdates[i].Count > 0)
                    generalUpdatesExistInTickBitflags |= 1 << i;
            }

            // Write the flags
            writer.Put((byte)generalUpdatesExistInTickBitflags);

            for(int i=0; i<WNetCommon.TICKS_PER_SNAPSHOT; i++) {

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
            int[] totalEntitiesInTicks = new int[WNetCommon.TICKS_PER_SNAPSHOT];
            for (int i=0; i<WNetCommon.TICKS_PER_SNAPSHOT; i++) {
                totalEntitiesInTicks[i] = s_entityUpdates[i].Keys.Count;

                if (totalEntitiesInTicks[i] > 0)
                    tickContainsEntityUpdatesFlags |= 1 << i;
            }

            // Write the flags
            writer.Put((byte)tickContainsEntityUpdatesFlags);

            for (int i=0; i<WNetCommon.TICKS_PER_SNAPSHOT; i++) {

                // Serialize # of general updates only when there's more than 0; otherwise skip to next tick
                int entitiesInTick = totalEntitiesInTicks[i];
                if (entitiesInTick > 0)
                    writer.Put((ushort)entitiesInTick);
                else
                    continue;

                // For each entity and their updates,
                foreach(KeyValuePair<int, List<INetSerializable>> entityUpdates in s_entityUpdates[i]) {

                    // Put entity ID
                    int entityId = entityUpdates.Key;
                    writer.Put(entityId);

                    // Put # of packets
                    int amountOfUpdates = entityUpdates.Value.Count;
                    writer.Put((ushort)amountOfUpdates);

                    // Then put all of its updates
                    foreach(var pkt in entityUpdates.Value) {
                        pkt.Serialize(writer);
                    }
                }
            }
        }
    }
}