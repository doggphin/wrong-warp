using System.Collections.Generic;
using LiteNetLib.Utils;
using Networking.Client;

namespace Networking.Shared {
    public class WSChunkGeneralUpdatesPkt : INetSerializable {
        public int startTick;
        public List<INetSerializable>[] updates;

        public void Serialize(NetDataWriter writer) {        
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
                    // Put updates
                    foreach(INetSerializable update in updates[i]) {
                        update.Serialize(writer);
                    }
                }
            }
            writer.Put((byte)updatesExistBitflags);

            // Write bitflags, then return to final position
            int finalWriterPosition = writer.Length;
            writer.SetPosition(bitflagsWriterPosition);
            writer.Put(updatesExistBitflags);
            writer.SetPosition(finalWriterPosition);
        }

    
        public void Deserialize(NetDataReader reader) {
            startTick = reader.GetInt();

            int updatesExistBitflags = reader.GetByte();

            for(int i=0, bitMask = 1; i<WCommon.TICKS_PER_SNAPSHOT; i++, bitMask <<= 1) {
                if((updatesExistBitflags & bitMask) == 0)
                    continue;

                int amountOfUpdates = (int)reader.GetVarUInt();
                
                for(int j=0; i<amountOfUpdates; j++) {
                    WPacketType packetType = reader.GetPacketType();
                    WCNetClient.Instance.ProcessPacketFromReader(null, reader, startTick + i, packetType);
                }
            }
        }
    }
}