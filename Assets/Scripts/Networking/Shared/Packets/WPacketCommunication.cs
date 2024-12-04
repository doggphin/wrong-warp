using LiteNetLib.Utils;
using LiteNetLib;
using System;
using UnityEngine;


namespace Networking.Shared {
    public static class WPacketCommunication {
        public static void SendSingle<T>(
            NetDataWriter writer, 
            NetPeer peer, 
            int tick,  
            T packet, 
            DeliveryMethod deliveryMethod) where T : INetSerializable
        {
            if (peer == null)
                return;

            writer.Reset();
            writer.Put(tick);
            packet.Serialize(writer);

            peer.Send(writer, deliveryMethod);
        }


        public static int? GetTick(NetDataReader reader, bool beSafe = true) {
            if (!beSafe || reader.AvailableBytes >= 4)
                return reader.GetInt();

            return null;
        }


        public static WPacketType? GetNextPacketType(NetDataReader reader, bool beSafe = true) {
            if(!beSafe || reader.AvailableBytes >= 2)
                return (WPacketType)reader.GetUShort();

            return null;
        }


        public static void StartMultiPacket(NetDataWriter writer, int tick) {
            writer.Reset();
            writer.Put(tick);
        }

        
        public static void AddToMultiPacket<T>(NetDataWriter writer, T packet) where T : INetSerializable {
            packet.Serialize(writer);
        }

        
        /// <summary>
        /// Reads a multipacket.
        /// </summary>
        /// <param name="reader"> Reader with the multipacket. </param>
        /// <param name="packetHandler"> This function should return whether it deserialized the next packet successfully </param>
        /// <param name="beSafe"> Should this check if bytes are available before reading further? </param>
        /// <returns></returns>
        public static bool ReadMultiPacket(
            NetPeer receivedFrom, 
            NetDataReader reader, 
            Func<NetPeer, NetDataReader, int, WPacketType, bool> packetHandler,
            bool beSafe = true) {

            if (beSafe && reader.AvailableBytes < 4)
                return false;

            int tick = reader.GetInt();

            for(;;) {
                if(reader.AvailableBytes == 0)
                    return true;

                if (beSafe && reader.AvailableBytes < 2)
                    return false;

                WPacketType packetType = (WPacketType)reader.GetUShort();

                if (!packetHandler(receivedFrom, reader, tick, packetType)) {
                    return false;
                }
            }
        }
    }
}
