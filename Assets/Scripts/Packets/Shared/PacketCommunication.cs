using LiteNetLib.Utils;
using LiteNetLib;
using System;
using UnityEngine;


namespace Networking.Shared {
    public static class PacketCommunication {
        private static NetDataWriter defaultWriter = new();

        /// <param name="writer"> If left null, will (safely) use default writer </param>

        public static void SendSingle<T>(NetDataWriter writer, NetPeer peer, int tick,  T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
        {
            if (peer == null)
                return;

            if(writer == null) {
                writer = defaultWriter;
                writer.Reset();
            }

            StartMultiPacket(writer, tick);
            packet.Serialize(writer);
            peer.Send(writer, deliveryMethod);
        }


        public static void StartMultiPacket(NetDataWriter writer, int tick) {
            writer.Reset();
            writer.Put(tick);
        }


        /// <param name="packetHandler"> Delegate that will handle all packets found in this MultiPacket. </param>
        /// <returns> Whether no errors occursed. Should improve this at some later date. </returns>
        public static bool ReadMultiPacket(NetPeer receivedFrom, NetDataReader reader, Func<NetPeer, NetDataReader, int, PacketIdentifier, bool> packetHandler) {
            int tick = reader.GetInt();

            for(;;) {
                if (reader.AvailableBytes < 2)
                    return reader.AvailableBytes == 0;

                PacketIdentifier packetType = reader.GetPacketType();
                if (!packetHandler(receivedFrom, reader, tick, packetType))
                    return false;
            }
        }
    }
}