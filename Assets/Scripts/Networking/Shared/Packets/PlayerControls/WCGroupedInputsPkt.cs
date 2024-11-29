using System;
using LiteNetLib.Utils;
using UnityEngine;

namespace Networking.Shared {
    public class WCGroupedInputsPkt : INetSerializable
    {
        public const byte MAX_INPUTS_PER_GROUP = WCommon.TICKS_PER_SNAPSHOT;

        public byte amountOfInputs;
        public WInputsSerializable[] inputsSerialized;

        public void Deserialize(NetDataReader reader)
        {
            amountOfInputs = Math.Min(reader.GetByte(), MAX_INPUTS_PER_GROUP);
            
            inputsSerialized = new WInputsSerializable[amountOfInputs];
            for(int i=0; i<amountOfInputs; i++) {
                WInputsSerializable inputs = new();
                inputs.Deserialize(reader);
                inputsSerialized[i] = inputs;
            }

        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(WPacketType.CGroupedInputs);

            writer.Put((byte)inputsSerialized.Length);
            for(int i=0; i<inputsSerialized.Length; i++) {
                inputsSerialized[i].Serialize(writer);
            }
        }
    }
}