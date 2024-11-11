using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;


namespace Networking
{
    public class NetServer
    {
        private UdpClient udpServer;
        private IPEndPoint udpEndpoint;
        private Thread receiveUdpPacketsThread;

        private ConcurrentDictionary<long, ConcurrentDictionary<long, List<ServerNetMessage>>> tickBuffer;
        public ConcurrentDictionary<long, NetConnection> connections = new();

        public bool IsRunning { get; private set; } = false;
        public NetServer(string hostname, ushort udpPort) {
            udpEndpoint = new IPEndPoint(IPAddress.Parse(hostname), udpPort);

            udpServer = new UdpClient(udpEndpoint);
            receiveUdpPacketsThread = new Thread(
                () => ReceiveUdpPackets(udpServer));
            receiveUdpPacketsThread.Start();

            tickBuffer = new();
            StartNewTick(0);

            Debug.Log("Server has started!");
            IsRunning = true;
        }
        

        public void StartNewTick(long tick) {
            tickBuffer.TryAdd(tick, new());

            if(tick % 5 == 0) {
                var ticksToSend = new ConcurrentDictionary<long, List<ServerNetMessage>>[5];
                tickBuffer.TryRemove(tick - 5, out ticksToSend[0]);
                tickBuffer.TryRemove(tick - 4, out ticksToSend[1]);
                tickBuffer.TryRemove(tick - 3, out ticksToSend[2]);
                tickBuffer.TryRemove(tick - 2, out ticksToSend[3]);
                tickBuffer.TryRemove(tick - 1, out ticksToSend[4]);

                // Send the ticks out 
            }
        }


        public void AddMessage(long objectId, ServerNetMessage message) {
            long tick = NetManager.Instance.Tick;

            ConcurrentDictionary<long, List<ServerNetMessage>> objectsToMessages;
            List<ServerNetMessage> netObjectMessages;

            // Try to get dict for this 
            objectsToMessages = tickBuffer.GetOrAdd(tick, new ConcurrentDictionary<long, List<ServerNetMessage>>());
            netObjectMessages = objectsToMessages.GetOrAdd(objectId, new List<ServerNetMessage>());

            netObjectMessages.Add(message);
        }


        private void ProcessPacket(byte[] msg, IPEndPoint from) {
            Debug.Log("Received a message: " + System.Text.Encoding.ASCII.GetString(msg));
        }


        private void ReceiveUdpPackets(UdpClient udpServer) {
            Debug.Log("Now listening!");
            for(;;) {
                byte[] receiveBytes = udpServer.Receive(ref udpEndpoint);
                Debug.Log("a");
                ThreadPool.QueueUserWorkItem(_ => ProcessPacket(receiveBytes, udpEndpoint));
            }
        }

        
        public void Close() {
            udpServer.Close();

            receiveUdpPacketsThread.Join();

            IsRunning = false;
        }
    }
}
