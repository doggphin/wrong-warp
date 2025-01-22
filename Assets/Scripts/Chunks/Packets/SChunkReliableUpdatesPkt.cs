using System.Collections.Generic;
using UnityEngine;
using LiteNetLib.Utils;
using Networking.Client;

namespace Networking.Shared {
    ///<summary> A bundle of reliable updates </summary>
    public class SChunkReliableUpdatesPkt : BasePacket {
        public void SetStartTick(int currentServerTick) => startTick = currentServerTick - NetCommon.TICKS_PER_SNAPSHOT;
        public int startTick;
        public List<BasePacket>[] updates;

        public override void Serialize(NetDataWriter writer) {
            writer.Put(PacketIdentifier.SChunkReliableUpdates);

            writer.Put(startTick);
            
            // Save position to write bitflags to return to later. Put a dummy value here to overwrite later.
            int bitflagsWriterPosition = writer.Length;
            int updatesExistBitflags = 0;
            writer.Put((byte)0);
            
            for(int i=0; i<NetCommon.TICKS_PER_SNAPSHOT; i++) {
                if(updates[i] != null && updates[i].Count > 0) {
                    // Save updates exist to bitflags to write later
                    updatesExistBitflags |= 1 << i;
                    // Put length of updates
                    writer.PutVarUInt((uint)updates[i].Count);
                    Debug.Log($"Putting {updates[i].Count} updates!");
                    // Put updates
                    foreach(BasePacket update in updates[i]) {
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

    
        public override void Deserialize(NetDataReader reader) {
            startTick = reader.GetInt();

            int updatesExistBitflags = reader.GetByte();

            for(int i=0, bitMask = 1; i<NetCommon.TICKS_PER_SNAPSHOT; i++, bitMask <<= 1) {
                if((updatesExistBitflags & bitMask) == 0) {
                    updates[i] = new();
                    continue;
                }
                
                int amountOfUpdates = (int)reader.GetVarUInt();
                updates[i] = new(amountOfUpdates);
                
                for(int j=0; j<amountOfUpdates; j++) {
                    updates[i].Add(CPacketUnpacker.DeserializeNextPacket(reader));
                }
            }
        }

        public override bool ShouldCache => false;
        protected override void OverridableBroadcastApply(int tick) {
            for(int i=0; i<updates.Length; i++)
                foreach(var packet in updates[i])
                    CPacketUnpacker.ConsumePacket(tick - (updates.Length - 1) + i, packet);
        }
    }
}