using LiteNetLib.Utils;
using Mono.Cecil;
using Networking;
using Networking.Shared;
using UnityEngine;


namespace Networking.Shared {
    public enum WPacketType : ushort {
        Unimplemented,

        CJoinRequest,
        CInputs,
        CGroupedInputs,

        SJoinAccept,
        SJoinDenied,

        SChunkDeltaSnapshot,
        SEntityTransformUpdate,
        SEntityKill,
        SEntitySpawn,
        SFullEntitiesSnapshot,
        SEntitiesLoadedDelta,
    }


    public struct WTransformSerializable : INetSerializable
    {
        public Vector3? position;
        public Quaternion? rotation;
        public Vector3? scale;
        
        public void Deserialize(NetDataReader reader)
        {
            byte flags = reader.GetByte();

            if ((flags & 1) != 0) {
                position = reader.GetVector3();
            }
            if ((flags & 2) != 0) {
                rotation = reader.GetShiternion();
            }
            if ((flags & 4) != 0) {
                scale = reader.GetVector3();
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            byte flags = 0;
            if (position != null) {
                flags |= 1;
            }
            if (rotation != null) {
                flags |= 2;
            }
            if (scale != null) {
                flags |= 4;
            }

            writer.Put(flags);

            if (position != null) {
                writer.Put((Vector3)position);
            }
            if (rotation != null) {
                writer.PutShiternion((Quaternion)rotation);
            }
            if (scale != null) {
                writer.Put((Vector3)scale);
            }
        }
    }
}


public static class WExtensions {
    public static byte CompressNormalizedFloat(float val) {
        return (byte)((val + 1) * 127.5);
    }
    public static float DecompressNormalizedFloat(byte val) {
        return (val / 127.5f) - 1f;
    }

    public static byte MegaCompressNormalizedFloat(float val) {
        return (byte)((val + 1f) * 16);
    }
    public static float MegaDecompressNormalizedFloat(byte val) {
        return (val / 16f) - 1f;
    }


    public static uint GetVarUInt(this NetDataReader reader)
    {
        uint ret = 0;

        for(int i=0; i<5; i++) {
            byte chunk = reader.GetByte();
            
            if((chunk & 0b10000000) != 0) {
                // If there's a leading 1, then remove it
                chunk &= 0b01111111;

                ret |= chunk;
                ret <<= 7;
            } else {
                ret |= chunk;

                // If there's no leading 1, stop reading
                return ret;
            }
        }

        return ret;
    }
    public static void PutVarUInt(this NetDataWriter writer, uint val)
    {
        if(val == 0) {
            writer.Put((byte)0);
            return;
        }
            
        while(val > 0) {
            byte chunk = (byte)(val & 0b01111111);
            val >>= 7;

            if(val != 0) {
                chunk |= 0b10000000;
            }

            writer.Put(chunk);
        }
    }


    public static void PutShiternion(this NetDataWriter writer, Quaternion quat) {
        writer.Put(CompressNormalizedFloat(quat.x));
        writer.Put(CompressNormalizedFloat(quat.y));
        writer.Put(CompressNormalizedFloat(quat.z));
        writer.Put(CompressNormalizedFloat(quat.w));
    }
    public static Quaternion GetShiternion(this NetDataReader reader) {
        Quaternion rotation = new() {
            x = DecompressNormalizedFloat(reader.GetByte()),
            y = DecompressNormalizedFloat(reader.GetByte()),
            z = DecompressNormalizedFloat(reader.GetByte()),
            w = DecompressNormalizedFloat(reader.GetByte())
        };
        // Not really necessary as far as I can tell
        // rotation.Normalize();

        return rotation;
    }


    public static void Put(this NetDataWriter writer, Vector3 vector) {
        writer.Put(vector.x);
        writer.Put(vector.y);
        writer.Put(vector.z);
    }
    public static Vector3 GetVector3(this NetDataReader reader) {
        return new Vector3(
            reader.GetFloat(),
            reader.GetFloat(),
            reader.GetFloat());
    }


    public static void Put(this NetDataWriter writer, Vector2 vector) {
        writer.Put(vector.x);
        writer.Put(vector.y);
    }
    public static Vector3 GetVector2(this NetDataReader reader) {
        return new Vector2(reader.GetFloat(), reader.GetFloat());
    }


    public static void Put(this NetDataWriter writer, Quaternion quat) {
        writer.Put(quat.x);
        writer.Put(quat.y);
        writer.Put(quat.z);
        writer.Put(quat.w);
    }
    public static Quaternion GetQuaternion(this NetDataReader reader) {
        return new Quaternion(
            reader.GetFloat(),
            reader.GetFloat(),
            reader.GetFloat(),
            reader.GetFloat());
    }

    public static void Put(this NetDataWriter writer, WPacketType packetType) {
        writer.Put((ushort)packetType);
    }
    public static WPacketType GetPacketType(this NetDataReader reader) {
        return (WPacketType)reader.GetUShort();
    }


    public static void Put(this NetDataWriter writer, WPrefabId prefabId) {
        writer.Put((ushort)prefabId);
    }
    public static WPrefabId GetPrefabId(this NetDataReader reader) {
        return (WPrefabId)reader.GetUShort();
    }
}