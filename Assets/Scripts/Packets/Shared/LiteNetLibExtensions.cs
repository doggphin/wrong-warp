using System.Collections.Generic;
using LiteNetLib.Utils;
using Mono.Cecil;
using Networking.Shared;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;

public static class LiteNetLibExtensions {
    public static void Append(this NetDataWriter targetWriter, NetDataWriter sourceWriter)
    {
        // Append the raw data from the source writer to the target writer
        targetWriter.Put(sourceWriter.Data, 0, sourceWriter.Length);
    }
    
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


    public static void Put(this NetDataWriter writer, PacketIdentifier packetType) {
        writer.Put((ushort)packetType);
    }
    public static PacketIdentifier GetPacketType(this NetDataReader reader) {
        return (PacketIdentifier)reader.GetUShort();
    }


    public static void Put(this NetDataWriter writer, EntityPrefabId prefabId) {
        writer.Put((ushort)prefabId);
    }
    public static EntityPrefabId GetPrefabId(this NetDataReader reader) {
        return (EntityPrefabId)reader.GetUShort();
    }


    public static void Put(this NetDataWriter writer, List<BasePacket> serverPacketList) {
        writer.PutVarUInt((uint)serverPacketList.Count);
        foreach(var serializable in serverPacketList) {
            serializable.Serialize(writer);
        }
    }
    public static List<BasePacket> GetNetPacketForClientList(this NetDataReader reader) {
        int count = (int)reader.GetVarUInt();
        List<BasePacket> ret = new(count);
        for(int i=0; i<count; i++) {
            ret.Add(CPacketUnpacker.DeserializeNextPacket(reader));
        }
        return ret;
    }

    public static void Put(this NetDataWriter writer, Dictionary<int, List<BasePacket>> serverTickedPacketCollection) {
        // Put amount of KVPs
        uint count = (uint)serverTickedPacketCollection.Count;
        writer.PutVarUInt((uint)serverTickedPacketCollection.Count);

        foreach(var kvp in serverTickedPacketCollection) {
            // Put tick, then list of packets
            writer.Put(kvp.Key);
            writer.Put(kvp.Value);
        }
    }
    public static Dictionary<int, List<BasePacket>> GetTickedPacketCollection(this NetDataReader reader) {
        int count = (int)reader.GetVarUInt();
        Dictionary<int, List<BasePacket>> ret = new(count);
        for(int i=0; i<count; i++) {
            int tick = reader.GetInt();
            List<BasePacket> packets = GetNetPacketForClientList(reader);
            ret[tick] = packets;
        }

        return ret;
    }

    public static void PutCollectionLength<T>(this NetDataWriter writer, ICollection<T> collection) {
        writer.PutVarUInt((uint)collection.Count);
    }
    public static int GetCollectionLength(this NetDataReader reader) {
        return (int)reader.GetVarUInt();
    }
}