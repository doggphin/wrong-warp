using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


namespace Networking
{
    public class NetServer
    {
        private UdpClient udpServer;

        private TcpListener tcpServer;

        private ConcurrentDictionary<int, ConcurrentDictionary<long, List<ServerNetMessage>>> tickBuffer;
        public ConcurrentDictionary<int, NetConnection> connections = new();

        public bool IsRunning { get; private set; } = false;
        public NetServer(IPEndPoint udpHostFrom, IPEndPoint tcpHostFrom) {
            udpServer = new UdpClient(udpHostFrom);
            tcpServer = new TcpListener(tcpHostFrom);
            _ = Task.Run(() => ReceiveUdpPackets(udpServer));
            _ = Task.Run(() => ReceiveTcpPackets(tcpServer));

            tickBuffer = new();
            StartNewTick(0);

            Debug.Log("Server has started!");
            IsRunning = true;
        }


        private async Task AcceptTcpClient(TcpListener tcpServer) {
            for (; ; ) {
                TcpClient client = await tcpServer.AcceptTcpClientAsync();


            }
        }


        private async Task ReceiveTcpPackets(TcpListener tcpServer) {

        }


        public void StartNewTick(int tick) {
            tickBuffer.TryAdd(tick, new());

            if(tick % 5 == 0) {
                var ticksToSend = new List<ConcurrentDictionary<long, List<ServerNetMessage>>>();

                for(int i=0; i < 5; i++) {
                    int pastTick = tick - 5 + i;
                    tickBuffer.TryRemove(pastTick, out var tickToSend);
                    ticksToSend[pastTick] = tickToSend;
                }

                // Send the ticks out
            }
        }


        public void AddMessage(long objectId, ServerNetMessage message) {
            int tick = NetManager.Instance.Tick;

            ConcurrentDictionary<long, List<ServerNetMessage>> objectsToMessages;
            List<ServerNetMessage> netObjectMessages;

            // Try to get dict for this 
            objectsToMessages = tickBuffer.GetOrAdd(tick, new ConcurrentDictionary<long, List<ServerNetMessage>>());
            netObjectMessages = objectsToMessages.GetOrAdd(objectId, new List<ServerNetMessage>());

            netObjectMessages.Add(message);
        }


        private async Task ReceiveUdpPackets(UdpClient udpServer) {
            for(;;) {
                var udpReceived = await udpServer.ReceiveAsync();
                _ = Task.Run(() => ProcessUdpPacket(udpReceived.RemoteEndPoint, udpReceived.Buffer));
            }
        }


        private void ProcessUdpPacket(IPEndPoint remoteEndPoint, byte[] buffer) {

        }

        
        public void Close() {
            udpServer.Close();
            tcpServer.Stop();

            IsRunning = false;
        }
    }
}
