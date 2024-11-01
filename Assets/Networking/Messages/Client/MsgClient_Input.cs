using Networking;
using System;

namespace Networking
{
    enum PlayerInputFlag : int
    {
        FORWARD = 0,
        RIGHT = 1,
        BACKWARD = 2,
        LEFT = 3,
    }

    struct CliMsg_Inputs : INetMessage
    {
        public BitFlags inputs;

        public float rotX;
        public float rotY;

        public bool GetPlayerInputFlag(PlayerInputFlag flag)
        {
            return inputs.GetFlag((int)flag);
        }


        public bool Deserialize(byte[] data, int readFrom = 0)
        {
            if(data.Length - readFrom < 9)
            {
                return false;
            }

            inputs.flags = data[readFrom];
            rotX = BitConverter.ToSingle(data, readFrom + 1);
            rotY = BitConverter.ToSingle(data, readFrom + 5);

            return true;
        }


        public byte[] Serialize()
        {
            byte[] ret = new byte[1 + 4 + 4];

            ret[0] = (byte)inputs.flags;
            Array.Copy(BitConverter.GetBytes(rotX), 0, ret, 1, 4);
            Array.Copy(BitConverter.GetBytes(rotY), 0, ret, 5, 4);

            return ret;
        }


        public int GetSerializedLength()
        {
            return 9;
        }
    }
}
