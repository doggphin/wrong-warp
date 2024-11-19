using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

public static class NetDataExtensions {
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

        CJoin,

        SJoinAccept,
        SJoinDenied,
        SEntityTransformUpdate,
        SEntityKillUpdate,
    }
}