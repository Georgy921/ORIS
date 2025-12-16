using GameAndDot.Shared.Enums;
using GameAndDot.Shared.Models;
using System.Net.Sockets;
using System.Text.Json;
using System.Drawing;
using System.Collections.Concurrent;

namespace GameAndDot.WinForm
{
    public partial class Form1 : Form
    {
        private int dotSize = 10;

        private readonly StreamReader? _reader;
        private readonly StreamWriter? _writer;

        private readonly TcpClient _client;

        private readonly Dictionary<string, Color> _playerColors = new();
        private readonly List<(string Username, Point Position)> _allDots = new();

        const string host = "127.0.0.1";
        const int port = 8888;

        private string _playerName = "";

        public Form1()
        {
            InitializeComponent();

            _client = new TcpClient();

            gameField.Paint += GameFieldPaint;
            gameField.MouseClick += GameFieldMouseClick;

            listBox1.DrawMode = DrawMode.OwnerDrawFixed;
            listBox1.DrawItem += ListBox1_DrawItem;

            try
            {
                _client.Connect(host, port); 

                var stream = _client.GetStream();

                _reader = new StreamReader(stream);
                _writer = new StreamWriter(stream);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

     

        private async void button1_Click(object sender, EventArgs e)
        {
            label1.Visible = false;
            textBox1.Visible = false;
            button1.Visible = false;

            label2.Visible = true;
            label4.Visible = true;
            usernameLbl.Visible = true;
            colorLbl.Visible = true;
            listBox1.Visible = true;
            gameField.Visible = true;

            string name = textBox1.Text.Trim();
            _playerName = name;
            usernameLbl.Text = name;


            Task.Run(() => ReceiveMessageAsync());

            var message = new EventMessage()
            {
                Type = EventType.PlayerConnected,
                Username = name
            };

            string json = JsonSerializer.Serialize(message);

            await SendMessageAsync(json);
        }


        async Task ReceiveMessageAsync()
        {
            while (_client.Connected)
            {
                try
                {

                    string? jsonRequest = await _reader.ReadLineAsync();
                    var messageRequest = JsonSerializer.Deserialize<EventMessage>(jsonRequest);

                    switch (messageRequest.Type)
                    {
                        case EventType.PlayerConnected:

                            Invoke(() =>
                            {
                                listBox1.Items.Clear();
                                _playerColors.Clear();

                                foreach (var name in messageRequest.PlayerInfo)
                                {
                                    Color color = ColorTranslator.FromHtml(name.ColorHex);
                                    _playerColors[name.Username] = color;
                                    listBox1.Items.Add(name.Username);

                                    if (_playerColors.TryGetValue(_playerName, out var myCol))
                                    {
                                        colorLbl.Text = name.ColorHex;
                                        colorLbl.ForeColor = myCol;
                                    }
                                }
                            });
                            break;

                        case EventType.PlayerPlacedDot:
                            Invoke(() =>
                            {
                                if (messageRequest.DotX.HasValue && messageRequest.DotY.HasValue && messageRequest.Username != null)
                                {
                                    if (messageRequest.Username != _playerName)
                                    {
                                        AddDotLocally(messageRequest.Username, messageRequest.DotX.Value, messageRequest.DotY.Value);
                                    }
                                }
                            });
                            break;
                    }
                }
                catch
                {
                    break;
                }
            }
        }


        async Task SendMessageAsync(string message)
        {

            await _writer.WriteLineAsync(message);
            await _writer.FlushAsync();
        }

        

        private void GameFieldPaint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            foreach (var (username, pos) in _allDots)
            {
                if (!_playerColors.TryGetValue(username, out Color color)) continue;

                using SolidBrush brush = new SolidBrush(color);
                e.Graphics.FillEllipse(brush, pos.X - dotSize / 2, pos.Y - dotSize / 2, dotSize, dotSize);
            }
        }
        private void ListBox1_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            string username = listBox1.Items[e.Index].ToString();

            e.DrawBackground();


            Color color = _playerColors.ContainsKey(username)
                ? _playerColors[username]
                : Color.Black;

            using (Brush brush = new SolidBrush(color))
            {
                e.Graphics.DrawString(username, e.Font, brush, e.Bounds);
            }

            e.DrawFocusRectangle();
        }

        private async void GameFieldMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;


            int x = e.X;
            int y = e.Y;

            var moveMessage = new EventMessage
            {
                Type = EventType.PlayerPlacedDot, 
                DotX = x,
                DotY = y
            };

            string json = JsonSerializer.Serialize(moveMessage);
            await SendMessageAsync(json);


            AddDotLocally(usernameLbl.Text, x, y);
        }

        private void AddDotLocally(string username, int x, int y)
        {
            _allDots.Add((username, new Point(x, y)));
            gameField.Invalidate(); 
        }

        private void GameFieldClick(object sender, EventArgs e)
        {

        }
        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}