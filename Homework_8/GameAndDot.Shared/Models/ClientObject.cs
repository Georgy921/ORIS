using GameAndDot.Shared.Enums;
using System.Drawing;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace GameAndDot.Shared.Models
{
    public class ClientObject
    {
        protected internal string Id { get; } = Guid.NewGuid().ToString();
        protected internal StreamWriter Writer { get; }
        protected internal StreamReader Reader { get; }
        public string Username { get; set; } = String.Empty;
        public string ColorHex { get; private set; } = "#000000";
        public int DotX { get; private set; }
        public int DotY { get; private set; }

        private const int FieldSize = 450;
        private const int DotSize = 30;

        TcpClient client;

        ServerObject server; // объект сервера

        private static readonly Random _rnd = new();

        public ClientObject(TcpClient tcpClient, ServerObject serverObject)
        {
            client = tcpClient;
            server = serverObject;
            // получаем NetworkStream для взаимодействия с сервером
            var stream = client.GetStream();
            // создаем StreamReader для чтения данных
            Reader = new StreamReader(stream);
            // создаем StreamWriter для отправки данных
            Writer = new StreamWriter(stream);
            // Генерируем случайный цвет
            ColorHex = GenerateRandomColor();
            // Генерируем позицию
            var pos = GenerateRandomPosition();
            DotX = pos.X;
            DotY = pos.Y;
        }

        private (int X, int Y) GenerateRandomPosition()
        {
            Random rnd = new();
            int max = FieldSize - DotSize - 20;
            return (
                rnd.Next(20, max > 20 ? max : 200),
                rnd.Next(20, max > 20 ? max : 200)
            );
        }
        private string GenerateRandomColor()
        {
            double hue = _rnd.NextDouble() * 360;
            var color = HsvToRgb(hue, 0.65, 0.85);
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private static Color HsvToRgb(double h, double s, double v)
        {
            int hi = (int)(h / 60) % 6;
            double f = h / 60 - hi;
            double p = v * (1 - s);
            double q = v * (1 - f * s);
            double t = v * (1 - (1 - f) * s);

            return hi switch
            {
                0 => Color.FromArgb((int)(v * 255), (int)(t * 255), (int)(p * 255)),
                1 => Color.FromArgb((int)(q * 255), (int)(v * 255), (int)(p * 255)),
                2 => Color.FromArgb((int)(p * 255), (int)(v * 255), (int)(t * 255)),
                3 => Color.FromArgb((int)(p * 255), (int)(q * 255), (int)(v * 255)),
                4 => Color.FromArgb((int)(t * 255), (int)(p * 255), (int)(v * 255)),
                _ => Color.FromArgb((int)(v * 255), (int)(p * 255), (int)(q * 255)),
            };
        }

        public async Task ProcessAsync()
        {
            try
            {
                while (true)
                {
                    var jsonRequest = await Reader.ReadLineAsync();
                    var messageRequest = JsonSerializer.Deserialize<EventMessage>(jsonRequest);

                    switch (messageRequest.Type)
                    {
                        case EventType.PlayerConnected:
                            Username = messageRequest.Username ?? "Unknown";
                            Console.WriteLine($"{Username} присоеденился к чату");

                            var players = server.Players
                                .Select(c => new PlayerInfo(c.Username, c.ColorHex))
                                .ToList();

                            var messageResponse = new EventMessage()
                            {
                                Type = EventType.PlayerConnected,
                                PlayerInfo = players,
                            };

                            string jsonResponse = JsonSerializer.Serialize(messageResponse);
                            await server.BroadcastMessageAllAsync(jsonResponse);

                            foreach (var historyDot in server.History)
                            {
                                // Сериализуем сообщение из истории
                                string historyJson = JsonSerializer.Serialize(historyDot);

                                // Отправляем ТОЛЬКО этому клиенту
                                await Writer.WriteLineAsync(historyJson);
                                await Writer.FlushAsync();
                            }
                            break;

                        case EventType.PlayerPlacedDot:

                            var broadcastMsg = new EventMessage
                            {
                                Type = EventType.PlayerPlacedDot,
                                Username = Username,
                                DotX = messageRequest.DotX,
                                DotY = messageRequest.DotY,
                            };

                            server.History.Add(broadcastMsg);

                            string json = JsonSerializer.Serialize(broadcastMsg);
                            await server.BroadcastMessageAllAsync(json);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                //закрываем ресурсы
                server.RemoveConnection(Id);
            }
        }
        // закрытие подключения
        protected internal void Close()
        {
            Writer.Close();
            Reader.Close();
            client.Close();
        }
    }
}
