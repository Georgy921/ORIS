using System.Text.Json;
using XProtocol;
using XProtocol.Serializator;

namespace TCPClient
{
    internal class Program
    {
        private static string _playerName = "TestPlayer";
        private static string _playerColor = "#00FF00";

        private static void Main()
        {
            Console.Title = $"XClient | {_playerName}";
            Console.ForegroundColor = ConsoleColor.White;

            var client = new XClient();
            client.OnPacketReceived += OnPacketRecieve;
            client.Connect("127.0.0.1", 4910);

            Thread.Sleep(1000);

            var handshakePacket = new XPacketHandshake
            {
                Username = _playerName,
                ColorHex = _playerColor
            };

            client.QueuePacketSend(
                XPacketConverter.Serialize(XPacketType.Handshake, handshakePacket).ToPacket());

            while (true) { }
        }

        private static void ProcessIncomingPacket(XPacket packet)
        {
            var type = XPacketTypeManager.GetTypeFromPacket(packet);

            switch (type)
            {
                case XPacketType.Handshake:
                    Console.WriteLine("Server: Handshake successful.");
                    break;

                case XPacketType.Point:
                    ProcessPoint(packet);
                    break;

                case XPacketType.MapState:
                    ProcessMapState(packet);
                    break;

                case XPacketType.Unknown:
                    Console.WriteLine("Received Unknown packet.");
                    break;

                default:
                    Console.WriteLine($"Received unhandled packet type: {type:G}");
                    break;
            }
        }

        private static void OnPacketRecieve(XPacket packet)
        {
            ProcessIncomingPacket(packet);
        }

        private static void ProcessPoint(XPacket packet)
        {
            var point = XPacketConverter.Deserialize<XPacketPoint>(packet);
            Console.WriteLine($"Player {point.Username} ({point.ColorHex}) placed point at ({point.X}, {point.Y}).");
        }

        private static void ProcessMapState(XPacket packet)
        {
            var map = XPacketConverter.Deserialize<XPacketMapState>(packet);

            try
            {
                var history = JsonSerializer.Deserialize<List<XPacketPoint>>(map.SerializedPointsJson);

                Console.WriteLine($"Received map history with {history.Count} points. Applying now...");

                foreach (var p in history)
                {
                    Console.WriteLine($" - History dot: {p.Username} at ({p.X},{p.Y})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing map state JSON: {ex.Message}");
            }
        }
    }
}
