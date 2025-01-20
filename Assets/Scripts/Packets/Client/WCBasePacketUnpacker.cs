// using System;
// using System.Collections.Generic;
// using LiteNetLib.Utils;
// using Networking.Client;
// using Networking.Shared;
// using UnityEngine;

// public abstract class WCBasePacketForClientUnpacker<BaseT> : BaseSingleton<WCBasePacketForClientUnpacker<BaseT>> where BaseT : INetPacketForClient {
//     private T2 Deserialize<T2>(NetDataReader reader) where T2 : class, BaseT, new() {
//         T2 ret = new();
//         ret.Deserialize(reader);
//         return ret;
//     }

//     private readonly Dictionary<WPacketIdentifier, Func<NetDataReader, BaseT>> packetDeserializers = new();

//     public void RegisterSerializer<ImplementedT>(WPacketIdentifier packetIdentifier, ImplementedT packetType) where ImplementedT : class, BaseT, new() {
//         packetDeserializers[packetIdentifier] = Deserialize<ImplementedT>;
//     }

//     ///<summary> Reads out a packet type and then a packet that matches that type from a NetDataReader </summary>
//     ///<returns> The deserialized packet </returns>
//     public BaseT DeserializeNextPacket(NetDataReader reader) {
//         ushort packetTypeUShort = reader.GetUShort();
//         if(!packetDeserializers.TryGetValue((WPacketIdentifier)packetTypeUShort, out var function)) {
//             throw new Exception($"No handler for {(WPacketIdentifier)packetTypeUShort} (Code {packetTypeUShort})!");
//         }
//         return function(reader);
//     }

//     ///<summary> Either caches or immediatedly applies a packet based on its logic </summary>
//     public void ConsumePacket(int tick, INetPacketForClient packet) {
//         if(packet.ShouldCache) {
//             WCPacketCacheManager.CachePacket(tick, packet);
//         } else {
//             packet.ApplyOnClient(tick);
//         }
//     }

//     ///<summary> Reads through next packet in NetDataReader, applying or caching it as per the packet's logic  </summary>
//     public void ConsumeNextPacket(int tick, NetDataReader reader) {
//         BaseT packet = DeserializeNextPacket(reader);
//         ConsumePacket(tick, packet);
//     }

//     ///<summary> Reads through an entire NetDataReader, applying or caching each packet read </summary>
//     public void ConsumeAllPackets(int tick, NetDataReader reader) {
//         while(reader.AvailableBytes >= sizeof(ushort)) {
//             ConsumeNextPacket(tick, reader);
//         }
//     }
// }