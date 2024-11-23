
using System;
using LiteNetLib.Utils;
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


    public class WCInputsPkt : INetSerializable
    {
        public InputFlags inputFlags;
        public Vector2? look;

        public void Deserialize(NetDataReader reader)
        {
            inputFlags.flags = reader.GetLong();

            look = inputFlags.GetFlag(InputType.Look) ?
                reader.GetVector2()
                : null;
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(WPacketType.CInputs);

            writer.Put(inputFlags);

            if(inputFlags.GetFlag(InputType.Look))
                writer.Put(look.Value);
        }
    }
}