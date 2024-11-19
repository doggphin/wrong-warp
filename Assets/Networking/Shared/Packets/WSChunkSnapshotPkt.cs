using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;

namespace Networking.Shared {
    public class WSChunkSnapshotPkt : INetSerializable {
        public List<INetSerializable>[] s_generalUpdates;
        public Dictionary<int, List<INetSerializable>>[] s_entityUpdates;

        public int c_headerTick;
        // Tick, Packet Type, Reader
        public Action<int, WPacketType, NetDataReader> c_generalHandler;
        // Tick, Entity ID, Packet Type, Reader
        public Action<int, int, WPacketType, NetDataReader> c_entityHandler;

        public void Deserialize(NetDataReader reader) {
            s_generalUpdates = new List<INetSerializable>[WNetCommon.TICKS_PER_SNAPSHOT];
            s_entityUpdates = new Dictionary<int, List<INetSerializable>>[WNetCommon.TICKS_PER_SNAPSHOT];

            for(int i=0; i < WNetCommon.TICKS_PER_SNAPSHOT; i++) {
                s_generalUpdates[i] = new();
                s_entityUpdates[i] = new();
            }

            for(int i=0; i < WNetCommon.TICKS_PER_SNAPSHOT; i++) {
                int numberOfGeneralUpdatesInTick = reader.GetUShort();

                while(numberOfGeneralUpdatesInTick-- > 0) {
                    WPacketType packetType = (WPacketType)reader.GetUShort();
                    c_generalHandler(
                        c_headerTick - WNetCommon.TICKS_PER_SNAPSHOT + i,
                        packetType,
                        reader);
                }
            }

            for(int i = 0; i < WNetCommon.TICKS_PER_SNAPSHOT; i++) {
                int numberOfEntityUpdatesInTick = reader.GetUShort();

                while(numberOfEntityUpdatesInTick-- > 0) {
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

            // First write general updates
            //
            // For every tick in a snapshot:
            // -- Amount of general updates in this tick (ushort)
            //      -- General update
            //
            // For every tick in a snapshot:
            // -- Total amount of updates in this tick (ushort)
            //      -- Entity ID (int)
            //      -- Amount of updates
            //          -- Entity update
            for(int i=0; i<WNetCommon.TICKS_PER_SNAPSHOT; i++) {
                // Put # of updates
                writer.Put((ushort)s_generalUpdates[i].Count);

                // Serialize # general updates
                foreach(INetSerializable update in s_generalUpdates[i]) {
                    update.Serialize(writer);
                }
            }

            for(int i=0; i<WNetCommon.TICKS_PER_SNAPSHOT; i++) {
                // Calculate # of updates
                int numberOfUpdates = 0;
                foreach(var updates in s_entityUpdates[i].Values) {
                    numberOfUpdates += updates.Count;
                }

                // Put # of updates
                writer.Put(numberOfUpdates);

                foreach(KeyValuePair<int, List<INetSerializable>> entityUpdates in s_entityUpdates[i]) {
                    // Put entity ID
                    writer.Put(entityUpdates.Key);
                    // Put # of packets
                    writer.Put((ushort)entityUpdates.Value.Count);
                    // Put all updates
                    foreach(INetSerializable update in entityUpdates.Value) {
                        update.Serialize(writer);
                    }
                }
            }
        }
    }
}