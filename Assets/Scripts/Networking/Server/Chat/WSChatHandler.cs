using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using Networking.Shared;
using Unity.VisualScripting;
using UnityEngine;

namespace Networking.Server {
    public static class WSChatHandler {
        public const int MAX_CHAT_MESSAGE_LENGTH = 128;
        public const int MAX_CHAT_COMMAND_LENGTH = 256;

        // Returns if a given message was valid or not.
        public static bool HandleChatMessage(string msg, NetPeer fromPeer, bool isServerMessage) {
            if(msg.Length == 0)
                return false;
            
            if(msg[0] == '/')
                return HandleChatCommand(msg, fromPeer, isServerMessage);
            
            if(msg.Length > MAX_CHAT_MESSAGE_LENGTH)
                return false;

            BroadcastChatMessage(msg, fromPeer, isServerMessage);
            return true;
        }


        // Returns if the command was valid or not.
        private static bool HandleChatCommand(string msg, NetPeer fromPeer, bool isServerMessage) {
            // Message must include a character after the /
            if(msg.Length > MAX_CHAT_COMMAND_LENGTH || msg.Length < 2)
                return false;
            
            msg.Remove(0, 1);
            string[] args = msg.Split(' ', 2);

            switch(args[0]) {
                case "tp":
                    HandleTeleportCommand(args[1], fromPeer, isServerMessage);
                    break;
                case "broadcast":
                    Debug.Log("Not yet implemented!");
                    break;
                case "msg":
                    Debug.Log("Not yet implemented!");
                    break;
            }
            return true;
        }


        private static void BroadcastChatMessage(string msg, NetPeer fromPeer, bool isServerMessage) {
            WSPlayer player = null;
            // If fromPeer is null, it's possible this is a server/host message
            if(fromPeer != null && !WSPlayer.FromPeer(fromPeer, out player))
                return;

            WSChatMessagePkt chatMessagePkt = new() {
                isServerMessage = isServerMessage,
                speakerId = isServerMessage ? 0 : player.Entity.Id,
                message = msg
            };

            if(!isServerMessage) {
                player.Entity.CurrentChunk.AddGenericUpdate(WSNetServer.Tick, chatMessagePkt, true);
                // If the host is sending this, show in host chat
                if(fromPeer == null) {
                    ChatUiManager.ReceiveChatMessage(chatMessagePkt);
                }
            } else {
                // Send server messages directly to each client
                NetDataWriter writer = new();
                chatMessagePkt.Serialize(writer);
                foreach(var peer in WSNetServer.ServerNetManager.ConnectedPeerList) {
                    peer.Send(writer, DeliveryMethod.ReliableUnordered);
                }
                // Show message on host as well
                ChatUiManager.ReceiveChatMessage(chatMessagePkt);
            }
            
            WSChatMessagePkt pkt = new() {
                speakerId = player.Entity.Id,
                isServerMessage = fromPeer == null,
                message = msg
            };
        }


        private static bool HandleTeleportCommand(string argsString, NetPeer sender, bool isServerMessage) {
            string[] args = argsString.Split(' ');

            switch(args.Length) {
                case 3:
                    if(isServerMessage)
                        return false;
                    
                    if(!float.TryParse(args[0], out float posX) || !float.TryParse(args[1], out float posY) || !float.TryParse(args[2], out float posZ))
                        return false;

                    WSEntity playerEntity = ((WSPlayer)sender.Tag).Entity;

                    if(playerEntity != null)
                        playerEntity.positionsBuffer[WSNetServer.Tick] = new Vector3(posX, posY, posZ);
                    
                    return true;
                default:
                    return false;
            }
        }


    }
}