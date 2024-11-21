using System;
using System.Collections;
using System.IO.Compression;
using System.Xml.Linq;
using LiteNetLib;
using LiteNetLib.Utils;
using Unity.VisualScripting;
using UnityEngine;

public static class NetDataExtensions {
    public static byte CompressNormalizedFloat(float val) {
        return (byte)((val + 1) * 127.5);
    }
    public static float DecompressNormalizedFloat(byte val) {
        return (val / 127.5f) - 1f;
    }


    public static uint GetVarUInt(this NetDataReader reader)
    {
        uint ret = 0;

        for(int i=0; i<5; i++) {
            byte chunk = reader.GetByte();

            ret |= chunk;

            if((chunk & 0b10000000) == 0)
                ret <<= 7;
            else
                break;
        }

        return ret;
    }
    public static void PutVarUInt(this NetDataWriter writer, uint val)
    {
        while(val > 0) {
            writer.Put((byte)(val & 0b01111111));
            val >>= 7;
        }
    }


    public static void PutShitQuaternion(this NetDataWriter writer, Quaternion quat) {
        writer.Put(CompressNormalizedFloat(quat.x));
        writer.Put(CompressNormalizedFloat(quat.y));
        writer.Put(CompressNormalizedFloat(quat.z));
        writer.Put(CompressNormalizedFloat(quat.w));
    }
    public static Quaternion GetShitQuaternion(this NetDataReader reader) {
        Quaternion rotation = new() {
                x = DecompressNormalizedFloat(reader.GetByte()),
                y = DecompressNormalizedFloat(reader.GetByte()),
                z = DecompressNormalizedFloat(reader.GetByte()),
                w = DecompressNormalizedFloat(reader.GetByte())
            };
        rotation.Normalize();   // Would probably look cool if this got removed

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
}

namespace Networking.Shared {
    public enum WPacketType : ushort {
        Unimplemented,

        CJoinRequest,

        SJoinAccept,
        SJoinDenied,

        SChunkDeltaSnapshot,
        SEntityTransformUpdate,
        SEntityKill,
        SEntitySpawn,
        SFullEntitiesSnapshot
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
                rotation = reader.GetShitQuaternion();
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
                writer.PutShitQuaternion((Quaternion)rotation);
            }
            if (scale != null) {
                writer.Put((Vector3)scale);
            }
        }
    }
}