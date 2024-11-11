using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Networking
{
    public class NetClient
    {
        private UdpClient udpClient;
        private IPEndPoint udpEndpoint;

        private List<ClientNetMessage> sendBuffer;
        private Thread receiveUdpPacketsThread;

        public static ConcurrentBag<ServerNetMessage> receiveBuffer;

        public bool isConnected = false;

        public NetClient(string hostname, ushort udpPort)
        {
            udpEndpoint = new IPEndPoint(IPAddress.Parse(hostname), udpPort);

            udpClient = new UdpClient(udpEndpoint);

            receiveUdpPacketsThread = new Thread(
                () => ReceiveUdpPackets(udpClient));
            receiveUdpPacketsThread.Start();

            isConnected = true;
        }


        private void ProcessPacket(byte[] data) {

            uint tick = BitConverter.ToUInt32(data, 0);

            for (int i = sizeof(uint); i < data.Length; i++) {
                ServerNetMessageType messageType = (ServerNetMessageType)BitConverter.ToUInt16(data, i);
                i += sizeof(ushort);

                ServerNetMessage netMessage;
                int? indicesRead = null;
                switch (messageType) {
                    case ServerNetMessageType.NetObjectUpdate:
                        netMessage = new SNM_NetObjectUpdate();
                        netMessage.Deserialize(data, i);
                        break;
                    default:
                        throw new Exception("Unimplemented ServerNetMessageType decode case!");
                }

                if (indicesRead == null) {
                    throw new Exception($"Not enough bytes for a message of type ${messageType}! ({data.Length - i} bytes remained).");
                } else {
                    i += (int)indicesRead;
                }

                receiveBuffer.Add(netMessage);
            }
        }


        private void ReceiveUdpPackets(UdpClient udpClient)
        {
            while(true) {
                byte[] receiveBytes = udpClient.Receive(ref udpEndpoint);
                ThreadPool.QueueUserWorkItem(_ => ProcessPacket(receiveBytes));
            }
        }


        public void Disconnect()
        {
            udpClient.Close();

            receiveUdpPacketsThread.Join();

            isConnected = false;
        }


        public void PushMessage(ClientNetMessage msg)
        {
            sendBuffer.Add(msg);
        }


        public void WrapAndSendMessages(List<ClientNetMessage> messages, ConnectionType connectionType) {
            SendMessageBuffer(sendBuffer, NetManager.Instance.Tick, ConnectionType.UDP);
        }

        public void SendMessageBuffer(List<ClientNetMessage> messages, long tick, ConnectionType overChannel)
        {
            byte[][] serializedMessages = new byte[messages.Count][];

            int totalMessagesBytes = 0;
            for(int i = 0; i < messages.Count; i++)
            {
                serializedMessages[i] = messages[i].Serialize();
                totalMessagesBytes += serializedMessages[i].Length;
            }

            // Include tick in bytesToSend size
            byte[] bytesToSend = new byte[sizeof(uint) + totalMessagesBytes];
            Buffer.BlockCopy(BitConverter.GetBytes(tick), 0, bytesToSend, 0, sizeof(uint));

            for(int i = 0, writeIndex = 0; writeIndex < bytesToSend.Length; i++)
            {
                Buffer.BlockCopy(serializedMessages[i], 0,
                    bytesToSend, writeIndex,
                    serializedMessages[i].Length);
                writeIndex += serializedMessages[i].Length;
            }

            if (overChannel == ConnectionType.UDP) {
                udpClient.Send(bytesToSend, bytesToSend.Length);
            } else {
                Debug.Log("TCP not yet implemented");
            }    

            messages.Clear();
        }
    }
}
