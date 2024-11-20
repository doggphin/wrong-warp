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
                if ((entityUpdatesExistFlags & (1 << i)) == 0) {
                    continue;
                }
                    
                int numberOfEntityUpdatesInTick = reader.GetUShort();

                //Debug.Log($"In tick {c_headerTick - WNetCommon.TICKS_PER_SNAPSHOT + i}, there are {numberOfEntityUpdatesInTick} entity updates.");

                while (numberOfEntityUpdatesInTick-- > 0) {
                    int entityId = reader.GetInt();
                    int amountOfUpdates = reader.GetUShort();

                    while(amountOfUpdates-- > 0) {
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
            writer.Put((ushort)WPacketType.SChunkSnapshot);

            // -- Bitflags that represent which ticks have general updates
            // For every tick in a snapshot:
            // -- Amount of general updates in this tick (ushort)
            //      -- General update
            //
            // For every tick in a snapshot:
            // -- Total amount of updates in this tick (ushort)
            //      -- Entity ID (int)-
            //      -- Amount of updates
            //          -- Entity update'
            int generalUpdatesExistInTickBitflags = 0;
            for(int i=0; i<WNetCommon.TICKS_PER_SNAPSHOT; i++) {
                if (s_generalUpdates[i].Count > 0)
                    generalUpdatesExistInTickBitflags |= 1 << i;
            }

            writer.Put((byte)generalUpdatesExistInTickBitflags);

            for(int i=0; i<WNetCommon.TICKS_PER_SNAPSHOT; i++) {
                // Serialize # of general updates only when there's more than 0; otherwise skip to next tick
                int generalUpdatesInTick = s_generalUpdates[i].Count;
                if (generalUpdatesInTick == 0)
                    continue;
                writer.Put((ushort)generalUpdatesInTick);

                foreach (INetSerializable update in s_generalUpdates[i]) {
                    update.Serialize(writer);
                }
            }

            int tickContainsEntityUpdatesFlags = 0;
            int[] totalNumberOfEntityUpdatesInTick = new int[WNetCommon.TICKS_PER_SNAPSHOT];
            for(int i=0; i<WNetCommon.TICKS_PER_SNAPSHOT; i++) {
                foreach (var updates in s_entityUpdates[i].Values) {
                    totalNumberOfEntityUpdatesInTick[i] += updates.Count;
                    tickContainsEntityUpdatesFlags |= 1 << i;
                }
            }

            writer.Put((byte)tickContainsEntityUpdatesFlags);

            for (int i=0; i<WNetCommon.TICKS_PER_SNAPSHOT; i++) {
                // Serialize # of general updates only when there's more than 0; otherwise skip to next tick
                int entityUpdatesInTick = totalNumberOfEntityUpdatesInTick[i];
                if (entityUpdatesInTick == 0)
                    continue;
                writer.Put((ushort)entityUpdatesInTick);

                foreach(KeyValuePair<int, List<INetSerializable>> entityUpdates in s_entityUpdates[i]) {
                    int entityId = entityUpdates.Key;
                    int amountOfUpdates = entityUpdates.Value.Count;
                    // Put entity ID
                    writer.Put(entityId);
                    // Put # of packets
                    writer.Put((ushort)amountOfUpdates);
                    // Put all updates
                    foreach(INetSerializable update in entityUpdates.Value) {
                        update.Serialize(writer);
                    }
                }
            }
        }
    }
}