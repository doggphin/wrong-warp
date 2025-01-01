using LiteNetLib;
using UnityEngine;

namespace Networking.Server {
    public static class WSChatHandler {
        public const int MAX_CHAT_MESSAGE_LENGTH = 128;
        public const int MAX_CHAT_COMMAND_LENGTH = 256;

        // Returns if a given message was valid or not.
        public static bool HandleChatMessage(string msg, NetPeer fromPeer) {
            if(msg.Length == 0)
                return false;
            
            if(msg[0] == '/')
                return HandleChatCommand(msg, fromPeer);
            
            if(msg.Length > MAX_CHAT_MESSAGE_LENGTH)
                return false;

            return true;
        }


        // Returns if the command was valid or not.
        private static bool HandleChatCommand(string msg, NetPeer fromPeer) {
            // Message must include a character after the /
            if(msg.Length > MAX_CHAT_COMMAND_LENGTH || msg.Length < 2)
                return false;
            
            msg.Remove(0, 1);
            string[] args = msg.Split(' ');

            switch(args[0]) {
                case "tp":
                    break;
            }
            return true;
        }


        // Sends a message to all players.
        private static void BroadcastChatMessage(string msg, NetPeer fromPeer) {

        }

        
        private static void SendChatMessage(string msg, NetPeer fromPeer, NetPeer toPeer) {

        }


        private static bool HandleTeleportCommand(string[] args, NetPeer sender) {
            switch(args.Length) {
                case 4:
                    if(!float.TryParse(args[1], out float posX) || !float.TryParse(args[2], out float posY) || !float.TryParse(args[3], out float posZ))
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