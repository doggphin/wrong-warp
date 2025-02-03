using System;
using LiteNetLib.Utils;
using Unity.Profiling;
using UnityEngine;

public static class SPacketFragmenter {
    public const int PKT_MAX_LEN = 100; //1431;
    public const int PKT_HEADER_LEN = FragmentedPacketInfo.LENGTH + sizeof(int);
    public const int PKT_MAX_PAYLOAD_LEN = PKT_MAX_LEN - PKT_HEADER_LEN;
    public static int FragmentsRequiredForPayload(int payloadLength) => Mathf.CeilToInt(payloadLength / PKT_MAX_PAYLOAD_LEN);

    public static NetDataWriter[] FragmentPacketCollection(NetDataWriter writer) {
        if(writer.Length <= PKT_MAX_LEN) {
            writer.SetFragmentedPacketPayloadLen((ushort)(writer.Length - PKT_HEADER_LEN));
            return new NetDataWriter[1] { writer };
        }

        if(writer.Length > ushort.MaxValue) {
            throw new Exception("Unreliable packet is too long (65535+)! Cannot send!");
        }

        // Otherwise, fragment packet into pieces
        int tick = BitConverter.ToInt32(writer.Data, 0);
        int writerPayloadLen = writer.Length - PKT_HEADER_LEN;
        int fragmentsCount = Mathf.CeilToInt(writerPayloadLen / PKT_MAX_PAYLOAD_LEN);
        NetDataWriter[] fragmentedWriters = new NetDataWriter[fragmentsCount];
        for(int i=0, writerIdx = PKT_HEADER_LEN, bytesRemaining = writerPayloadLen; i<fragmentsCount; i++) {
            NetDataWriter fragmentedWriter = new(false, PKT_MAX_LEN);

            fragmentedWriter.Put(tick);

            FragmentedPacketInfo fragmentInfo = new() { curFragmentIdx = (byte)i, finalPacketPayloadLen = (ushort)writerPayloadLen };
            fragmentInfo.Serialize(fragmentedWriter);

            int bytesToCopy = Mathf.Min(fragmentsCount, bytesRemaining);
            Array.Copy(writer.Data, writerIdx, fragmentedWriter.Data, PKT_HEADER_LEN, bytesToCopy);

            writerIdx += bytesToCopy;
        }

        return fragmentedWriters;
    }


    public static void SetFragmentedPacketPayloadLen(this NetDataWriter writer, ushort finalPacketPayloadLen) {
        byte[] bytes = BitConverter.GetBytes(finalPacketPayloadLen);
        writer.Data[sizeof(int)] = bytes[0];
        writer.Data[sizeof(int) + 1] = bytes[1];
    }
}
