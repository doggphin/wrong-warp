using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using LiteNetLib.Utils;
using Networking.Shared;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

namespace Networking.Server {
    public class SChatHandler : BaseSingleton<SChatHandler> {
        public const int MAX_CHAT_MESSAGE_LENGTH = 128;
        public const int MAX_CHAT_COMMAND_LENGTH = 256;

        protected override void Awake() {
            CPacket<CChatMessagePkt>.ApplyUnticked += ReceiveChatMessage;
            base.Awake();
        }

        protected override void OnDestroy() {
            CPacket<CChatMessagePkt>.ApplyUnticked -= ReceiveChatMessage;
            base.OnDestroy();
        }

        // Returns if a given message was valid or not.
        private void ReceiveChatMessage(CChatMessagePkt pkt, NetPeer peer) {
            HandleChatMessage(pkt.message, peer, false);
        }
        public static bool HandleChatMessage(string msg, NetPeer fromPeer, bool isServerMessage) {
            if(msg.Length == 0)
                return false;
            
            if(msg[0] == '/')
                return HandleChatCommand(msg, fromPeer, isServerMessage);
            
            if(msg.Length > MAX_CHAT_MESSAGE_LENGTH)
                return false;

            msg = SanitizeMessageForSam(msg);

            BroadcastChatMessage(msg, fromPeer, isServerMessage);
            return true;
        }


        private static string SanitizeMessageForSam(string msg) {
            bool IsAlphanumeric(char character) {
                Debug.Log($"{character} is {(character >= 48 && character <= 57) || (character >= 65 && character <= 90) || (character >= 97 && character <= 122)}");
                return (character >= 48 && character <= 57) || (character >= 65 && character <= 90) || (character >= 97 && character <= 122);
            }

            List<char> sanitized = new();
            for(int i=0, punctuationLength = 0; i<msg.Length; i++) {
                char character = msg[i];
                if(character <= 31 || character >= 127) {
                    continue;
                }

                if(IsAlphanumeric(character)) {
                    punctuationLength = 0;
                    sanitized.Add(character);
                    continue;
                } else if(punctuationLength++ < 5) {
                    sanitized.Add(character);
                }
            }
            
            char lastCharacter = sanitized.Last();
            if(lastCharacter != '.' && lastCharacter != '?' && lastCharacter != '!') {
                sanitized.Add('.');
            }

            return new string(sanitized.ToArray());
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
            SPlayer player = null;

            if(!isServerMessage) {
                // Non-server message from a non-peer means it's a host message
                player = fromPeer == null ? SNetManager.HostPlayer : SPlayer.FromPeer(fromPeer);
            }
            
            SChatMessagePkt chatMessagePkt = new() {
                isServerMessage = isServerMessage,
                speakerId = isServerMessage ? 
                    0 
                    : player.Entity.Id,
                message = msg
            };

            // Display chat on host end
            ChatUiManager.ReceiveChatMessage(chatMessagePkt);

            // Non-server messages get sent locally
            if(!isServerMessage) {
                player.Entity.CurrentChunk.AddGenericUpdate(SNetManager.Instance.GetTick(), chatMessagePkt, true);
            // Server messages get sent globally
            } else {
                // Send server messages directly to each client
                NetDataWriter writer = new();
                chatMessagePkt.Serialize(writer);
                foreach(var peer in WWNetManager.ConnectedPeers) {
                    peer.Send(writer, DeliveryMethod.ReliableUnordered);
                }
            }
        }


        private static bool HandleTeleportCommand(string argsString, NetPeer sender, bool isServerMessage) {
            string[] args = argsString.Split(' ');

            switch(args.Length) {
                case 3:
                    if(isServerMessage)
                        return false;
                    
                    if(!float.TryParse(args[0], out float posX) || !float.TryParse(args[1], out float posY) || !float.TryParse(args[2], out float posZ))
                        return false;

                    WSEntity playerEntity = ((SPlayer)sender.Tag).Entity;

                    if(playerEntity != null)
                        playerEntity.positionsBuffer[SNetManager.Instance.GetTick()] = new Vector3(posX, posY, posZ);
                    
                    return true;
                default:
                    return false;
            }
        }


    }
}