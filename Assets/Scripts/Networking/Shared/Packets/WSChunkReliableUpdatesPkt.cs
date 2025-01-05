using System.Collections.Generic;
using UnityEngine;
using LiteNetLib.Utils;
using Networking.Client;

namespace Networking.Shared {
    public class WSChunkReliableUpdatesPkt : INetSerializable {
        public void SetStartTick(int currentServerTick) => startTick = currentServerTick - WCommon.TICKS_PER_SNAPSHOT;
        public int startTick;
        public List<INetSerializable>[] updates;

        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketType.SChunkReliableUpdates);

            writer.Put(startTick);
            
            // Save position to write bitflags to return to later. Put a dummy value here to overwrite later.
            int bitflagsWriterPosition = writer.Length;
            int updatesExistBitflags = 0;
            writer.Put((byte)0);
            
            for(int i=0; i<WCommon.TICKS_PER_SNAPSHOT; i++) {
                if(updates[i] != null && updates[i].Count > 0) {
                    // Save updates exist to bitflags to write later
                    updatesExistBitflags |= 1 << i;
                    // Put length of updates
                    writer.PutVarUInt((uint)updates[i].Count);
                    Debug.Log($"Putting {updates[i].Count} updates!");
                    // Put updates
                    foreach(INetSerializable update in updates[i]) {
                        update.Serialize(writer);
                    }
                }
            }

            // Write bitflags, then return to final position
            int finalWriterPosition = writer.Length;
            writer.SetPosition(bitflagsWriterPosition);
            writer.Put((byte)updatesExistBitflags);
            writer.SetPosition(finalWriterPosition);
        }

    
        public void Deserialize(NetDataReader reader) {
            startTick = reader.GetInt();

            int updatesExistBitflags = reader.GetByte();

            for(int i=0, bitMask = 1; i<WCommon.TICKS_PER_SNAPSHOT; i++, bitMask <<= 1) {
                if((updatesExistBitflags & bitMask) == 0)
                    continue;

                int amountOfUpdates = (int)reader.GetVarUInt();
                Debug.Log($"There are {amountOfUpdates} updates in here!");
                
                for(int j=0; j<amountOfUpdates; j++) {
                    WPacketType packetType = reader.GetPacketType();
                    WCNetClient.Instance.ProcessPacketFromReader(null, reader, startTick + i, packetType);
                }
            }
        }
    }
}