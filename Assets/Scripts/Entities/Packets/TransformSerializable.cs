using UnityEngine;
using LiteNetLib.Utils;

public struct TransformSerializable : INetSerializable {
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
            rotation = reader.GetLossyQuaternion();
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
            writer.PutLossyQuaternion((Quaternion)rotation);
        }
        if (scale != null) {
            writer.Put((Vector3)scale);
        }
    }
}