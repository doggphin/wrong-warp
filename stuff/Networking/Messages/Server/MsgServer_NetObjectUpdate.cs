using System;
using UnityEngine;

namespace Networking
{
    class SNM_NetObjectUpdate : ServerNetMessage
    {
        public Vector3? position = null;
        public Quaternion? rotation = null;
        public Vector3? scale = null;

        /* Data is serialized into the following format:
         * BYTE     Bitflags : 0 = Position is present, 1 = Rotation is present, 2 = Scale is present
         * VECTOR3  Position (optional)
         * VECTOR3  Rotation (optional)
         * VECTOR3  SCALE (optional)
         */
        public override int? Deserialize(byte[] data, int readFrom = 0)
        {
            if(data.Length - readFrom < 1)
            {
                return null;
            }

            bool containsPosition = (data[readFrom] & 1) == 1;
            bool containsRotation = (data[readFrom] & 2) == 2;
            bool containsScale = (data[readFrom] & 4) == 4;

            int expectedBytes = 1 + (containsPosition ? 4 : 0) + (containsRotation ? 4 : 0) + (containsScale ? 4 : 0);
            if(data.Length - readFrom < expectedBytes)
            {
                return null;
            }

            int indicesWritten = 1;
            if(containsPosition) {
                position = NetCommon.DeserializeVector3(data, readFrom + indicesWritten);
                indicesWritten += 12;
            }
            if(containsRotation) {
                rotation = Quaternion.Euler(NetCommon.DeserializeVector3(data, readFrom + indicesWritten));
                indicesWritten += 12;
            }
            if (containsRotation) {
                scale = NetCommon.DeserializeVector3(data, readFrom + indicesWritten);
                indicesWritten += 12;
            }

            return indicesWritten;
        }


        public override byte[] Serialize()
        {
            byte[] ret = new byte[1 + (position == null ? 0 : 12) + (rotation == null ? 0 : 12) + (scale == null ? 0 : 12)];

            int idx = 1;

            if (position != null) {
                ret[0] |= 1;
                NetCommon.SerializeVector3(ret, (Vector3)position, idx);
                idx += 12;
            }
            if(rotation != null) {
                ret[0] |= 2;
                NetCommon.SerializeVector3(ret, ((Quaternion)rotation).eulerAngles, idx);
                idx += 12;
            }
            if(scale != null) {
                ret[0] |= 4;
                NetCommon.SerializeVector3(ret, (Vector3)scale, idx);
            }

            return ret;
        }
    }
}
