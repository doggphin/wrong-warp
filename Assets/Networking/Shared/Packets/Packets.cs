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
        SEntityTransformUpdate,
    }


    public class WCJoinPacket : INetSerializable {
        public string userName;

        public void Serialize(NetDataWriter writer) {
            writer.Put(userName);
        }

        public void Deserialize(NetDataReader reader) {
            userName = reader.GetString();
        }
    }


    public class WSJoinAcceptPacket : INetSerializable {
        public string userName;

        public void Serialize(NetDataWriter writer) {
            writer.Put(userName);
        }

        public void Deserialize(NetDataReader reader) {
            userName = reader.GetString();
        }
    }


    public class WSEntityTransformUpdatePacket : INetSerializable {
        public Vector3? position;
        public Quaternion? rotation;
        public Vector3? scale;

        public void Serialize(NetDataWriter writer) {
            byte flags = 0;
            if(position != null) {
                flags |= 1;
            }
            if(rotation != null) {
                flags |= 2;
            }
            if (scale != null) {
                flags |= 4;
            }

            writer.Put(flags);
            if(position != null) {
                writer.Put((Vector3)position);
            }
            if(rotation != null) {
                writer.Put((Quaternion)rotation);
            }
            if(scale != null) {
                writer.Put((Vector3)scale);
            }
        }

        public void Deserialize(NetDataReader reader) {
            byte flags = reader.GetByte();

            if((flags & 1) != 0) {
                position = reader.GetVector3();
            }
            if((flags & 2) != 0) {
                rotation = reader.GetQuaternion();
            }
            if((flags & 3) != 0) {
                scale = reader.GetVector3();
            }
        }
    }
}