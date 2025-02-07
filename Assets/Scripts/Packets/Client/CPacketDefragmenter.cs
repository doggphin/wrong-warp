using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using Networking.Shared;
using UnityEngine;

public class CPacketDefragmenter : BaseSingleton<CPacketDefragmenter> {
    private class PacketDefragmentationBuffer {
        private int tick;
        private byte[] bytesBuffer;
        private HashSet<int> expectedFragmentIndices;

        public PacketDefragmentationBuffer(int tick, int payloadLength) {
            this.tick = tick;

            int indicesRequired = SPacketFragmenter.FragmentsRequiredForPayload(payloadLength);

            expectedFragmentIndices = new(indicesRequired);
            for(int i=0; i<indicesRequired; i++) {
                expectedFragmentIndices.Add(i);
            }
            bytesBuffer = new byte[payloadLength];
        }

        public void AssimilateFragment(int fragmentIdx, NetDataReader reader) {
            // If this fragment index has already been assimilated, skip it
            if(!expectedFragmentIndices.Remove(fragmentIdx)) {
                return;
            }

            // Copy the data into the bytes buffer
            int copyIntoBytesBufferStart = fragmentIdx * SPacketFragmenter.PKT_MAX_PAYLOAD_LEN;
            // Copy either max packet payload length OR remainder to the end of the buffer
            int bytesToCopyCount = Mathf.Min(SPacketFragmenter.PKT_MAX_PAYLOAD_LEN, bytesBuffer.Length - copyIntoBytesBufferStart);
            Array.Copy(
                // the + 1 here is to deal with litenetlib prepending reader data with an empty byte for some reason??
                reader.RawData, SPacketFragmenter.PKT_HEADER_LEN + 1,
                bytesBuffer,
                copyIntoBytesBufferStart,
                bytesToCopyCount
            );

            // If the bytes buffer has been filled, consume it
            if(expectedFragmentIndices.Count == 0) {
                NetDataReader fullReader = new(bytesBuffer);
                CPacketUnpacker.ConsumeAllPackets(tick, fullReader);
            }
        }
    }


    private TimestampedCircularTickBuffer<PacketDefragmentationBuffer> tickedReceivedFragments = new();


    public void ProcessUnreliablePacket(NetDataReader reader) {
        int tick = reader.GetInt();

        // If reader is older than a packet being defragmented, toss it out
        if(!tickedReceivedFragments.IsInputTickNewer(tick, true)) {
            return;
        }

        FragmentedPacketInfo fragmentInfo = new() { };
        fragmentInfo.Deserialize(reader);

        if(!tickedReceivedFragments.TryGetByTimestamp(tick, out var fragmentedPacket)) {
            fragmentedPacket = new(tick, fragmentInfo.finalPacketPayloadLen);
            tickedReceivedFragments.SetValueAndTimestamp(fragmentedPacket, tick);
        }

        fragmentedPacket.AssimilateFragment(fragmentInfo.curFragmentIdx, reader);
    }
}