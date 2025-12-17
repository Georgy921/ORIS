using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TCPClient;
using XProtocol;
using XProtocol.Serializator;

namespace GameAndDot.WinForm
{
   public class Dot
    {
        public int X { get; set; }
        public int Y { get; set; }
        public Color Color { get; set; }
        public string Username { get; set; }
    }

    public class PlayerInfo
    {
        public string Username { get; set; }
        public string ColorHex { get; set; }
    }

    public class GameClient
    {
        private readonly XClient _client = new XClient();
        private readonly Form1 _form;

        public string Username { get; private set; }
        public Color PlayerColor { get; private set; }

        public GameClient(Form1 form)
        {
            _form = form;
            _client.OnPacketReceived += ProcessIncomingPacket;
        }

        public void ConnectAndHandshake(string ip, int port, string username)
        {
            Username = username;

            _client.Connect(ip, port);

            var handshakePacket = new XPacketHandshake
            {
                Username = Username,
                ColorHex = "#000000"
            };

            _client.QueuePacketSend(
                XPacketConverter.Serialize(XPacketType.Handshake, handshakePacket).ToPacket());
        }

        public void SendPoint(int x, int y)
        {
            var pointPacket = new XPacketPoint
            {
                X = x,
                Y = y,
                Username = Username,
                ColorHex = ColorTranslator.ToHtml(PlayerColor)
            };

            _client.QueuePacketSend(
                XPacketConverter.Serialize(XPacketType.Point, pointPacket).ToPacket());
        }

        private void ProcessIncomingPacket(XPacket packet)
        {
            var type = XPacketTypeManager.GetTypeFromPacket(packet);

            _form.Invoke((MethodInvoker)delegate
            {
                switch (type)
                {
                    case XPacketType.Handshake:
                        var handshakeResponse = XPacketConverter.Deserialize<XPacketHandshake>(packet);

                        if (!string.IsNullOrEmpty(handshakeResponse.ColorHex))
                        {
                            PlayerColor = ColorTranslator.FromHtml(handshakeResponse.ColorHex);

                            /*_form.usernameLbl.Text = Username;
                            _form.colorLbl.Text = handshakeResponse.ColorHex;
                            _form.colorLbl.ForeColor = PlayerColor;*/
                        }
                        _form.HandleSuccessfulHandshake();
                        break;

                    case XPacketType.Point:
                        ProcessPoint(packet);
                        break;

                    case XPacketType.MapState:
                        ProcessMapState(packet);
                        break;
                    case XPacketType.PlayerList:
                        var playerListPacket = XPacketConverter.Deserialize<XPacketPlayerList>(packet);
                        try
                        {
                            var playerData = JsonSerializer.Deserialize<List<PlayerInfo>>(playerListPacket.SerializedData);
                            _form.UpdatePlayerListWithColors(playerData);
                        }
                        catch
                        {
                            _form.UpdatePlayerListWithColors(new List<PlayerInfo>());
                        }
                        break;
                }
            });
        }

        private void ProcessPoint(XPacket packet)
        {
            var pointData = XPacketConverter.Deserialize<XPacketPoint>(packet);
            var dot = new Dot
            {
                X = pointData.X,
                Y = pointData.Y,
                Username = pointData.Username,
                Color = ColorTranslator.FromHtml(pointData.ColorHex)
            };
            _form.AddDot(dot);
        }

        private void ProcessMapState(XPacket packet)
        {
            Console.WriteLine("!!! Received MapState packet. Starting history load.");
            var map = XPacketConverter.Deserialize<XPacketMapState>(packet);
            try
            {
                var history = JsonSerializer.Deserialize<List<XPacketPoint>>(map.SerializedPointsJson);
                _form.ClearGameField();
                foreach (var pointData in history)
                {
                    var dot = new Dot
                    {
                        X = pointData.X,
                        Y = pointData.Y,
                        Username = pointData.Username,
                        Color = ColorTranslator.FromHtml(pointData.ColorHex)
                    };
                    _form.AddDot(dot);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке истории карты: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
