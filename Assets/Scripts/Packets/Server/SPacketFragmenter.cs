using System;
using LiteNetLib.Utils;
using Unity.Profiling;
using UnityEngine;

public static class SPacketFragmenter {
    public const int PKT_MAX_TOT_LEN = 100; //1431;
    public const int PKT_HEADER_LEN = FragmentedPacketInfo.LENGTH + sizeof(int);
    public const int PKT_MAX_PAYLOAD_LEN = PKT_MAX_TOT_LEN - PKT_HEADER_LEN;
    public static int FragmentsRequiredForPayload(int payloadLength) => Mathf.CeilToInt(payloadLength / PKT_MAX_PAYLOAD_LEN);

    public static NetDataWriter[] FragmentPacketCollection(NetDataWriter writer) {
        if(writer.Length <= PKT_MAX_TOT_LEN) {
            SetFragmentedPacketPayloadLen(writer, (ushort)(writer.Length - PKT_HEADER_LEN));
            return new NetDataWriter[1] { writer };
        }

        if(writer.Length > ushort.MaxValue) {
            throw new Exception("Unreliable packet is too long (65535+)! Cannot send!");
        }

        // Otherwise, fragment packet into pieces
        int tick = BitConverter.ToInt32(writer.Data, 0);
        int writerPayloadLen = writer.Length - PKT_HEADER_LEN;
        int fragmentsCount = Mathf.CeilToInt((float)writerPayloadLen / PKT_MAX_PAYLOAD_LEN);
        NetDataWriter[] fragmentedWriters = new NetDataWriter[fragmentsCount];
        for(int i=0, writerIdx = PKT_HEADER_LEN, bytesRemaining = writerPayloadLen; i<fragmentsCount; i++) {
            NetDataWriter fragmentedWriter = new(false, PKT_MAX_TOT_LEN);
            FragmentedPacketInfo fragmentInfo = new() { curFragmentIdx = (byte)i, finalPacketPayloadLen = (ushort)writerPayloadLen };

            fragmentedWriter.Put(tick);
            fragmentInfo.Serialize(fragmentedWriter);

            int bytesToCopy = Mathf.Min(PKT_MAX_PAYLOAD_LEN, bytesRemaining);
            Debug.Log(writerIdx);
            Debug.Log(bytesToCopy);
            fragmentedWriter.Put(writer.Data, writerIdx, bytesToCopy);

            fragmentedWriters[i] = fragmentedWriter;
            writerIdx += bytesToCopy;
            bytesRemaining -= bytesToCopy;
        }

        return fragmentedWriters;
    }

    
    public static void PutFragmentedPacketHeader(NetDataWriter writer, int tick) {
        writer.Put(tick);
        FragmentedPacketInfo fragmentInfo = new() {
            finalPacketPayloadLen = 0,
            curFragmentIdx = 0,
        };
        fragmentInfo.Serialize(writer);
    }


    public static void SetFragmentedPacketPayloadLen(NetDataWriter writer, ushort finalPacketPayloadLen) {
        byte[] bytes = BitConverter.GetBytes(finalPacketPayloadLen);
        int payloadStartWriteIdx = sizeof(int);
        writer.Data[payloadStartWriteIdx] = bytes[0];
        writer.Data[payloadStartWriteIdx + 1] = bytes[1];
    }
}
