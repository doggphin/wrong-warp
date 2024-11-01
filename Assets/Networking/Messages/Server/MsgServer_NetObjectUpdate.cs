using System;
using UnityEngine;

namespace Networking
{
    struct SrvMsg_NetObjectUpdate : INetMessage
    {
        public Vector3? position;
        public Quaternion? rotation;
        public Vector3? scale;

        public bool Deserialize(byte[] data, int readFrom = 0)
        {
            if(data.Length - readFrom < 1)
            {
                return false;
            }

            bool containsPosition = (data[readFrom] & 1) == 1;
            bool containsRotation = (data[readFrom] & 2) == 2;
            bool containsScale = (data[readFrom] & 4) == 4;

            int expectedBytes = 1 + (containsPosition ? 4 : 0) + (containsRotation ? 4 : 0) + (containsScale ? 4 : 0);
            if(data.Length - readFrom < expectedBytes)
            {
                return false;
            }

            // Subtract 4 here since += applies before running ToSingle
            int idx = readFrom + 1 - 4;

            position = containsPosition ? new Vector3(BitConverter.ToSingle(data, idx += 4), BitConverter.ToSingle(data, idx += 4), BitConverter.ToSingle(data, idx += 8)) : null;
            rotation = containsPosition ? Quaternion.Euler(BitConverter.ToSingle(data, idx += 4), BitConverter.ToSingle(data, idx += 4), BitConverter.ToSingle(data, idx += 8)) : null;
            scale = containsPosition ? new Vector3(BitConverter.ToSingle(data, idx += 4), BitConverter.ToSingle(data, idx += 4), BitConverter.ToSingle(data, idx += 8)) : null;

            return true;
        }

        public int GetSerializedLength()
        {
            return 1 + (position == null ? 0 : 12) + (rotation == null ? 0 : 12) + (scale == null ? 0 : 12);
        }

        public byte[] Serialize()
        {
            byte[] ret = new byte[1 + (position == null ? 0 : 12) + (rotation == null ? 0 : 12) + (scale == null ? 0 : 12)];

            int idx = 1 - 4;

            if (position != null)
            {
                ret[0] |= 1;
                Array.Copy(BitConverter.GetBytes(((Vector3)position).x), 0, ret, idx += 4, 4);
                Array.Copy(BitConverter.GetBytes(((Vector3)position).y), 0, ret, idx += 4, 4);
                Array.Copy(BitConverter.GetBytes(((Vector3)position).z), 0, ret, idx += 4, 4);
            }
            if(rotation != null)
            {
                ret[0] |= 2;
                Vector3 eulerAngles = ((Quaternion)rotation).eulerAngles;
                Array.Copy(BitConverter.GetBytes(eulerAngles.x), 0, ret, idx += 4, 4);
                Array.Copy(BitConverter.GetBytes(eulerAngles.y), 0, ret, idx += 4, 4);
                Array.Copy(BitConverter.GetBytes(eulerAngles.z), 0, ret, idx += 4, 4);
            }
            if(scale != null)
            {
                ret[0] |= 4;
                Array.Copy(BitConverter.GetBytes(((Vector3)scale).x), 0, ret, idx += 4, 4);
                Array.Copy(BitConverter.GetBytes(((Vector3)scale).y), 0, ret, idx += 4, 4);
                Array.Copy(BitConverter.GetBytes(((Vector3)scale).z), 0, ret, idx += 4, 4);
            }

            return ret;
        }
    }
}
