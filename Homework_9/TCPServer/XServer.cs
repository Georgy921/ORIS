using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using XProtocol;
using XProtocol.Serializator;

namespace TCPServer
{
    internal class XServer
    {
        private static readonly List<XPacketPoint> GameHistory = new List<XPacketPoint>();
        private static readonly object HistoryLock = new object();

        private readonly Socket _socket;

        public List<ConnectedClient> Clients => _clients;
        private readonly List<ConnectedClient> _clients;

        private bool _listening;
        private bool _stopListening;

        public XServer()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _clients = new List<ConnectedClient>();
        }

        public void Start()
        {
            if (_listening)
            {
                throw new Exception("Server is listening ");
            }

            _socket.Bind(new IPEndPoint(IPAddress.Any, 4910));
            _socket.Listen(10);

            _listening = true;
        }

        public void Stop()
        {
            if (!_listening)
            {
                throw new Exception("Server is not listening ");
            }

            _stopListening = true;
            _socket.Shutdown(SocketShutdown.Both);
            _listening = false;
        }

        public void AcceptClients()
        {
            while (true)
            {
                if (_stopListening)
                {
                    return;
                }

                Socket client;

                try
                {
                    client = _socket.Accept();
                }
                catch { return; }

                Console.WriteLine($"Accepted client: {(IPEndPoint)client.RemoteEndPoint}");

                var c = new ConnectedClient(client, this);

                lock (_clients)
                {
                    _clients.Add(c);
                }
            }
        }

        public void BroadcastPacket(XPacket packetToSend, ConnectedClient excludeClient = null)
        {
            var rawPacket = packetToSend.ToPacket();

            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    if (client != excludeClient)
                    {
                        client.QueuePacketSend(rawPacket);
                    }
                }
            }
        }

        public List<XPacketPoint> GetGameHistory()
        {
            lock (HistoryLock)
            {
                return new List<XPacketPoint>(GameHistory);
            }
        }

        public void AddPointToHistory(XPacketPoint point)
        {
            lock (HistoryLock)
            {
                GameHistory.Add(point);
            }
        }

        public void BroadcastPlayerList()
        {
            var playerData = Clients
                .Where(c => !string.IsNullOrEmpty(c.Username))
                .Select(c => new { Username = c.Username, ColorHex = c.ColorHex })
                .ToList();

            var playerListPacket = new XPacketPlayerList
            {
                SerializedData = System.Text.Json.JsonSerializer.Serialize(playerData)
            };

            var packetBytes = XPacketConverter.Serialize(XPacketType.PlayerList, playerListPacket).ToPacket();

            foreach (var client in Clients)
            {
                client.QueuePacketSend(packetBytes);
            }
        }

        public void RemoveClient(ConnectedClient client)
        {
            lock (_clients)
            {
                _clients.Remove(client);
            }
            try
            {
                client.Client.Shutdown(SocketShutdown.Both);
                client.Client.Close();
            }
            catch { }
        }
    }
}
