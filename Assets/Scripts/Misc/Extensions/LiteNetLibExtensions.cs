using LiteNetLib.Utils;
using Networking.Shared;
using UnityEngine;

public static class LiteNetLibExtensions {
    public static void PutCompressedUnsignedFloat(this NetDataWriter writer, float value, float maxValue) {
        writer.Put(CompressionHelpers.CompressUnsignedFloat(value, maxValue));
    }
    public static float GetCompressedUnsignedFloat(this NetDataReader reader, float maxValue) {
        byte compressedValue = reader.GetByte();
        return CompressionHelpers.DecompressUnsignedFloat(compressedValue, maxValue);
    }

    public static uint GetVarUInt(this NetDataReader reader)
    {
        uint ret = 0;

        for(int i=0; i<5; i++) {
            byte chunk = reader.GetByte();
            
            if((chunk & 0b10000000) != 0) {
                // If there's a leading 1, then remove it
                chunk &= 0b01111111;

                ret <<= 7;
                ret |= chunk;
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


    public static void PutLossyQuaternion(this NetDataWriter writer, Quaternion quat) {
        writer.Put(CompressionHelpers.CompressNormalizedFloat(quat.x));
        writer.Put(CompressionHelpers.CompressNormalizedFloat(quat.y));
        writer.Put(CompressionHelpers.CompressNormalizedFloat(quat.z));
        writer.Put(CompressionHelpers.CompressNormalizedFloat(quat.w));
    }
    public static Quaternion GetLossyQuaternion(this NetDataReader reader) {
        Quaternion rotation = new() {
            x = CompressionHelpers.DecompressNormalizedFloat(reader.GetByte()),
            y = CompressionHelpers.DecompressNormalizedFloat(reader.GetByte()),
            z = CompressionHelpers.DecompressNormalizedFloat(reader.GetByte()),
            w = CompressionHelpers.DecompressNormalizedFloat(reader.GetByte())
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


    public static void Put(this NetDataWriter writer, NetPrefabType prefabId) {
        writer.Put((ushort)prefabId);
    }
    public static NetPrefabType GetPrefabId(this NetDataReader reader) {
        return (NetPrefabType)reader.GetUShort();
    }
}