using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using XProtocol;
using XProtocol.Serializator;

namespace TCPServer
{
    internal class ConnectedClient
    {
        public Socket Client { get; }
        private readonly XServer _server;

        public string Username { get; private set; }
        public string ColorHex { get; private set; }


        private readonly Queue<byte[]> _packetSendingQueue = new Queue<byte[]>();

        private static readonly HashSet<string> UsedColors = new HashSet<string>();
        private static readonly Random Random = new Random();
        private static readonly object ColorLock = new object();

        public ConnectedClient(Socket client, XServer server)
        {
            Client = client;
            _server = server;

            Task.Run((Action)ProcessIncomingPackets);
            Task.Run((Action)SendPackets);
        }

        private void ProcessIncomingPackets()
        {
            var buffer = new byte[256];
            var data = new List<byte>();

            while (true)
            {
                try
                {
                    int received = Client.Receive(buffer);
                    if (received == 0) break;

                    data.AddRange(buffer.Take(received));

                    int endMarkerIndex = -1;
                    for (int i = 0; i < data.Count - 1; i++)
                    {
                        if (data[i] == 0xFF && data[i + 1] == 0x00)
                        {
                            endMarkerIndex = i + 1;
                            break;
                        }
                    }

                    if (endMarkerIndex != -1)
                    {
                        var packetBytes = data.Take(endMarkerIndex + 1).ToArray();
                        data.RemoveRange(0, endMarkerIndex + 1);

                        var parsed = XPacket.Parse(packetBytes);
                        if (parsed != null)
                        {
                            ProcessIncomingPacket(parsed);
                        }
                    }
                }
                catch (SocketException)
                {
                    Console.WriteLine($"Client disconnected: {Client.RemoteEndPoint}");
                    _server.Clients.Remove(this);
                    lock (ColorLock)
                    {
                        if (!string.IsNullOrEmpty(ColorHex))
                        {
                            UsedColors.Remove(ColorHex);
                        }
                    }
                    _server.BroadcastPlayerList();
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error packet: {ex.Message}");
                    Console.WriteLine($"Inner: {ex.InnerException?.Message}");
                    Console.WriteLine(ex.StackTrace);
                    return;
                }
            }
        }

        private void ProcessIncomingPacket(XPacket packet)
        {
            var type = XPacketTypeManager.GetTypeFromPacket(packet);

            switch (type)
            {
                case XPacketType.Handshake:
                    ProcessHandshake(packet);
                    break;

                case XPacketType.Point:
                    ProcessPoint(packet);
                    break;

                case XPacketType.Unknown:
                    Console.WriteLine($"Received unknown packet from {Client.RemoteEndPoint}");
                    break;

                default:
                    Console.WriteLine($"Received unhandled packet type: {type:G}");
                    break;
            }
        }

        private void ProcessPoint(XPacket packet)
        {
            var pointPacket = XPacketConverter.Deserialize<XPacketPoint>(packet);

            pointPacket.ColorHex = ColorHex;
            pointPacket.Username = Username;

            _server.AddPointToHistory(pointPacket);

            Console.WriteLine($"Point placed by {pointPacket.Username} at ({pointPacket.X},{pointPacket.Y}).");
            var outPacket = XPacketConverter.Serialize(XPacketType.Point, pointPacket);

            _server.BroadcastPacket(outPacket);
        }

        private void ProcessHandshake(XPacket packet)
        {
            var handshake = XPacketConverter.Deserialize<XPacketHandshake>(packet);

            Username = handshake.Username;

            lock (ColorLock)
            {
                string newColor;
                int attempts = 0;
                do
                {
                    int r = Random.Next(50, 256);
                    int g = Random.Next(50, 256);
                    int b = Random.Next(50, 256);
                    newColor = $"#{r:X2}{g:X2}{b:X2}";
                    attempts++;
                }
                while (UsedColors.Contains(newColor) && attempts < 50);

                ColorHex = newColor;
                UsedColors.Add(newColor);
            }

            Console.WriteLine($"Player {Username} connected. assigned color {ColorHex}");

            var responseHandshake = new XPacketHandshake
            {
                Username = Username,
                ColorHex = ColorHex
            };
            QueuePacketSend(XPacketConverter.Serialize(XPacketType.Handshake, responseHandshake).ToPacket());

            var history = _server.GetGameHistory();

            var resetPacket = new XPacketMapState { SerializedPointsJson = "[]" };
            QueuePacketSend(XPacketConverter.Serialize(XPacketType.MapState, resetPacket).ToPacket());

            foreach (var p in history)
            {
                QueuePacketSend(XPacketConverter.Serialize(XPacketType.Point, p).ToPacket());
            }


            _server.BroadcastPlayerList();
        }

        public void QueuePacketSend(byte[] packet)
        {
            if (packet.Length > 256)
            {
                throw new Exception("Max packet size 256 bytes.");
            }

            _packetSendingQueue.Enqueue(packet);
        }

        private void SendPackets()
        {
            while (true)
            {
                if (_packetSendingQueue.Count == 0)
                {
                    Thread.Sleep(100);
                    continue;
                }

                var packet = _packetSendingQueue.Dequeue();
                Client.Send(packet);

                Thread.Sleep(100);
            }
        }
    }
}
