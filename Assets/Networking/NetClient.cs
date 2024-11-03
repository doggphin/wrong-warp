using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine.InputSystem.Android;

namespace Networking
{
    public class NetUdpClient
    {
        private UdpClient udpClient;
        private List<ClientNetMessage> sendBuffer;
        private SynchronizedCollection<ServerNetMessage> receiveBuffer;
        private Thread receivePacketsThread;
        private IPEndPoint endpoint;


        public NetUdpClient(string hostname, ushort port)
        {
            endpoint = new IPEndPoint(IPAddress.Parse(hostname), port);
            udpClient = new UdpClient(endpoint);

            receivePacketsThread = new Thread(
                () => ReceivePackets(udpClient));
        }


        private void ReceivePackets(UdpClient udpClient)
        {
            
        }


        public void SendPackets()
        {

        }


        public void Disconnect()
        {
            udpClient.Close();
            receivePacketsThread.Join();
        }


        public void PushMessage(ClientNetMessage msg)
        {
            sendBuffer.Add(msg);
        }


        public void SendMessages(uint tick)
        {
            byte[][] serializedMessages = new byte[sendBuffer.Count][];

            int totalMessagesBytes = 0;
            for(int i = 0; i < sendBuffer.Count; i++)
            {
                serializedMessages[i] = sendBuffer[i].Serialize();
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

            udpClient.Send(bytesToSend, bytesToSend.Length);

            sendBuffer.Clear();
        }
    }
}
