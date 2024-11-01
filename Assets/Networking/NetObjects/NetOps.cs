using System;
using UnityEngine;
using System.Runtime.CompilerServices;

public static class NetOps
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValid(byte[] data, int from, int bytesNeeded, bool safe)
    {
        return !safe || data.Length - from >= bytesNeeded;
    }
    public static Vector3? ReadVec3(byte[] data, int from, bool safe = true)
    {
        return IsValid(data, from, 12, safe) ? new Vector3(
            BitConverter.ToSingle(data, from), 
            BitConverter.ToSingle(data, from + 4), 
            BitConverter.ToSingle(data, from + 8)
            ) : null;
    }

    public static Quaternion? ReadQuat4(byte[] data, int from, bool safe = true)
    {
        return IsValid(data, from, 16, safe) ? new Quaternion(
            BitConverter.ToSingle(data, from),
            BitConverter.ToSingle(data, from + 4),
            BitConverter.ToSingle(data, from + 8),
            BitConverter.ToSingle(data, from + 12)
            ) : null;
    }

    /// <summary>
    /// Reads a 12 bytes representation of euler angles as a quaternion.
    /// </summary>
    public static Quaternion? ReadQuat3(byte[] data, int from, bool safe = true)
    {
        return IsValid(data, from, 12, safe) ? Quaternion.Euler(
            BitConverter.ToSingle(data, from),
            BitConverter.ToSingle(data, from + 4),
            BitConverter.ToSingle(data, from + 8)
            ) : null;
    }

    public static float? ReadFloat(byte[] data, int from, bool safe = true)
    {
        return IsValid(data, from, 4, safe) ? BitConverter.ToSingle(data, from) : null;
    }
}
