using Networking;
using System;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Networking
{
    enum PlayerInputFlag : int
    {
        FORWARD = 0,
        RIGHT = 1,
        BACKWARD = 2,
        LEFT = 3,
    }


    class CNM_Inputs : ClientNetMessage
    {
        public BitFlags inputs;
        public float rotX;
        public float rotY;

        public CNM_Inputs(uint playerId) : base(playerId) {
        }

        public bool GetPlayerInputFlag(PlayerInputFlag flag)
        {
            return inputs.GetFlag((int)flag);
        }


        public override int? Deserialize(byte[] data, int readFrom)
        {
            if(data.Length - readFrom < 4 * 3)
            {
                return null;
            }

            inputs.flags = BitConverter.ToInt32(data, 0);
            rotX = BitConverter.ToSingle(data, 4);
            rotY = BitConverter.ToSingle(data, 8);

            return 4 * 3;
        }


        public override byte[] Serialize()
        {
            byte[] ret = new byte[sizeof(int) * 3];

            Buffer.BlockCopy(BitConverter.GetBytes(inputs.flags), 0, ret, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(rotX), 0, ret, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(rotY), 0, ret, 8, 4);

            return ret;
        }


        public override void Apply()
        {
            throw new NotImplementedException();
        }
    }
}
