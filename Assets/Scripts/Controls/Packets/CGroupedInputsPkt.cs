using System;
using LiteNetLib.Utils;
using UnityEngine;

namespace Networking.Shared {
    public class CGroupedInputsPkt : CPacket<CGroupedInputsPkt>
    {
        public const byte MAX_INPUTS_PER_GROUP = NetCommon.TICKS_PER_SNAPSHOT;

        public byte amountOfInputs;
        public InputsSerializable[] inputsSerialized;

        public override void Deserialize(NetDataReader reader)
        {
            amountOfInputs = Math.Min(reader.GetByte(), MAX_INPUTS_PER_GROUP);
            
            inputsSerialized = new InputsSerializable[amountOfInputs];
            for(int i=0; i<amountOfInputs; i++) {
                InputsSerializable inputs = new();
                inputs.Deserialize(reader);
                inputsSerialized[i] = inputs;
            }

        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(PacketIdentifier.CGroupedInputs);

            writer.Put((byte)inputsSerialized.Length);
            for(int i=0; i<inputsSerialized.Length; i++) {
                inputsSerialized[i].Serialize(writer);
            }
        }
    }
}