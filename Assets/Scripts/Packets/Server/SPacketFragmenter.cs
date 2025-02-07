using System;
using LiteNetLib.Utils;
using Unity.Profiling;
using UnityEngine;

public static class SPacketFragmenter {
    public const int PKT_MAX_TOT_LEN = 1431;
    public const int PKT_HEADER_LEN = FragmentedPacketInfo.LENGTH + sizeof(int);
    public const int PKT_MAX_PAYLOAD_LEN = PKT_MAX_TOT_LEN - PKT_HEADER_LEN;
    public static int FragmentsRequiredForPayload(int payloadLength) => Mathf.CeilToInt((float)payloadLength / PKT_MAX_PAYLOAD_LEN);

    /// <summary>
    /// Fragments unreliable packet collections into pieces that can be safely sent.
    /// </summary>
    /// <param name="oWriter"> A writer with a fragmented packet header already attached to it. </param>
    /// <param name="tick"> The tick for this packet. </param>
    /// <returns> An array of fragmented NetDataWriters, each with their own header. This might just consist of the original NetDataWriter. </returns>
    /// <exception cref="Exception"> The writer length cannot be over 65535. This was arbitrarily chosen. </exception>
    public static NetDataWriter[] FragmentPacketCollection(NetDataWriter oWriter, int tick) {
        if(oWriter.Length <= PKT_MAX_TOT_LEN) {
            SetFragmentedPacketPayloadLen(oWriter, (ushort)(oWriter.Length - PKT_HEADER_LEN));
            return new NetDataWriter[1] { oWriter };
        }

        if(oWriter.Length > ushort.MaxValue) {
            throw new Exception("Unreliable packet is too long (65535+)! Cannot send!");
        }

        // Otherwise, fragment packet into two or more pieces
        int totalPayloadLen = oWriter.Length - PKT_HEADER_LEN;
        int fragmentsCount = FragmentsRequiredForPayload(totalPayloadLen);
        NetDataWriter[] fragmentedWriters = new NetDataWriter[fragmentsCount];
        for(int fragmentIdx=0, writerIdx = PKT_HEADER_LEN; fragmentIdx<fragmentsCount; fragmentIdx++) {
            int bytesToCopy = Mathf.Min(PKT_MAX_PAYLOAD_LEN, oWriter.Length - writerIdx);
            NetDataWriter fragmentedWriter = fragmentedWriters[fragmentIdx] = new(false, PKT_HEADER_LEN + bytesToCopy);

            fragmentedWriter.Put(tick);

            FragmentedPacketInfo fragmentInfo = new() { curFragmentIdx = (byte)fragmentIdx, finalPacketPayloadLen = (ushort)totalPayloadLen };
            fragmentInfo.Serialize(fragmentedWriter);

            fragmentedWriter.Put(oWriter.Data, writerIdx, bytesToCopy);
            writerIdx += bytesToCopy;
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
        Array.Copy(
            bytes, 0, 
            writer.Data, sizeof(int), 
            2
        );
    }
}
