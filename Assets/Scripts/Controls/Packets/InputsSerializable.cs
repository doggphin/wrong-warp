using LiteNetLib.Utils;
using UnityEngine;

namespace Networking.Shared {
    public enum InputType : byte {
        Forward,
        Right,
        Back,
        Left,
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

        FireDown,
        FireDownEvent,

        AltFireDown,
        AltFireDownEvent,
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


    public class InputsSerializable : INetSerializable
    {
        public InputFlags inputFlags;

        public float? fireDownSubtick;
        public Vector2? fireDownLookVector;
        public float? altFireDownSubtick;
        public Vector2? altFireDownLookVector;

        public Vector2? look;

        public void Deserialize(NetDataReader reader)
        {
            inputFlags.flags = reader.GetLong();

            if(inputFlags.GetFlag(InputType.Look))
                look = reader.GetVector2();

            if(inputFlags.GetFlag(InputType.FireDownEvent)) {
                fireDownSubtick = reader.GetCompressedUnsignedFloat(1);
                fireDownLookVector = reader.GetVector2();
            }

            if(inputFlags.GetFlag(InputType.AltFireDownEvent)) {
                altFireDownSubtick = reader.GetCompressedUnsignedFloat(1);
                altFireDownLookVector = reader.GetVector2();
            }
        }


        public void Serialize(NetDataWriter writer)
        {
            writer.Put(inputFlags.flags);

            if(inputFlags.GetFlag(InputType.Look))
                writer.Put(look.Value);

            if(inputFlags.GetFlag(InputType.FireDownEvent)) {
                writer.PutCompressedUnsignedFloat(Mathf.Clamp(fireDownSubtick.Value, 0, 1), 1);
                writer.Put(fireDownLookVector.Value);
            }

            if(inputFlags.GetFlag(InputType.AltFireDownEvent)) {
                writer.PutCompressedUnsignedFloat(Mathf.Clamp(altFireDownSubtick.Value, 0, 1), 1);
                writer.Put(altFireDownLookVector.Value);
            } 
        }
    }
}