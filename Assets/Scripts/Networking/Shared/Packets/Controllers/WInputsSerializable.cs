
using System;
using LiteNetLib.Utils;
using Mono.Cecil;
using UnityEngine;

namespace Networking.Shared {
    public enum InputType : byte {
        Forward,
        Right,
        Back,
        Left,
        Fire,
        AltFire,
        Dash,
        Jump,
        Crouch,
        Interact,
        Melee,
        Look,
        Item1,
        Item2,
        Item3,
        Item4,
    }


    public struct InputFlags {
        public long flags;

        public bool GetFlag(InputType inputType) {
            return (flags & (1L << (byte)inputType)) != 0;
        }

        public void SetFlag(InputType inputType, bool setActive) {
            long mask = 1L << (byte)inputType;

            if(setActive)
                flags |= mask;
            else
                flags &= ~mask;
        }

        public void Reset() {
            flags = 0;
        }
    }


    public class WInputsSerializable : INetSerializable
    {
        public InputFlags inputFlags;
        public Vector2? look = null;

        public void Deserialize(NetDataReader reader)
        {
            inputFlags.flags = reader.GetLong();

            if(inputFlags.GetFlag(InputType.Look))
                look = reader.GetVector2();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(inputFlags.flags);

            if(inputFlags.GetFlag(InputType.Look))
                writer.Put(look.Value);
        }
    }
}