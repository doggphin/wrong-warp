using UnityEngine;
using UnityEngine.Windows;

namespace Networking
{
    struct BitFlags
    {
        public int flags;

        public readonly bool GetFlag(int flag)
        {
            return (flags | (byte)(1 << (int)flag)) != 0;
        }
        public void SetFlag(int flag, bool value)
        {
            if (value)
            {
                flags |= (byte)(1 << (int)flag);
            }
            else
            {
                flags &= (byte)(byte.MaxValue ^ (byte)(1 << (int)flag));
            }
        }
    }

    enum PlayerActionFlag : int
    {
        FORWARD = 0,
        RIGHT = 1,
        BACKWARD = 2,
        LEFT = 3,
    }
}
