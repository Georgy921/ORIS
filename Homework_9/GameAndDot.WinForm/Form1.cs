

namespace GameAndDot.WinForm
{
    public partial class Form1 : Form
    {
        private GameClient _gameClient;
        private readonly List<Dot> _dots = new List<Dot>();
        private readonly Dictionary<string, Color> _playerColors = new Dictionary<string, Color>();

        public Form1()
        {
            InitializeComponent();

            gameField.Image = new Bitmap(gameField.Width, gameField.Height);
            gameField.Click += GameField_Click;

            button1.Click += Button1_Click;

            listBox1.DrawMode = DrawMode.OwnerDrawFixed;
            listBox1.DrawItem += ListBox1_DrawItem;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            var username = textBox1.Text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Error of name!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _gameClient = new GameClient(this);
                Task.Run(() => _gameClient.ConnectAndHandshake("127.0.0.1", 4910, username));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error of connection: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void HandleSuccessfulHandshake()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(HandleSuccessfulHandshake));
                return;
            }

            label1.Visible = false;
            textBox1.Visible = false;
            button1.Visible = false;
            gameField.Visible = true;
            listBox1.Visible = true;
            label2.Visible = true;
            label4.Visible = true;
            usernameLbl.Visible = true;
            colorLbl.Visible = true;
        }

        private void GameField_Click(object sender, EventArgs e)
        {
            if (_gameClient == null || gameField.Visible == false) return;

            var mouseEventArgs = (MouseEventArgs)e;
            int x = mouseEventArgs.X;
            int y = mouseEventArgs.Y;

            Task.Run(() => _gameClient.SendPoint(x, y));
        }

        public void ClearGameField()
        {
            _dots.Clear();
            using (var g = Graphics.FromImage(gameField.Image))
            {
                g.Clear(Color.White);
            }
            gameField.Invalidate();
        }

        public void AddDot(Dot dot)
        {
            _dots.Add(dot);

            DrawAllDots();
        }

        public void UpdatePlayerListWithColors(List<PlayerInfo> players)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<List<PlayerInfo>>(UpdatePlayerListWithColors), players);
                return;
            }

            _playerColors.Clear();
            foreach (var p in players)
            {
                if (!string.IsNullOrEmpty(p.ColorHex))
                {
                    _playerColors[p.Username] = ColorTranslator.FromHtml(p.ColorHex);
                }
            }

            listBox1.Items.Clear();
            foreach (var p in players)
            {
                listBox1.Items.Add(p.Username);
            }

            listBox1.Invalidate();
        }

        private void DrawAllDots()
        {
            using (var g = Graphics.FromImage(gameField.Image))
            {
                foreach (var dot in _dots)
                {
                    using (var brush = new SolidBrush(dot.Color))
                    {
                        const int dotSize = 16;
                        g.FillEllipse(brush, dot.X - dotSize / 2, dot.Y - dotSize / 2, dotSize, dotSize);
                    }
                }
            }
            gameField.Invalidate();
        }
        private void ListBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            string username = listBox1.Items[e.Index].ToString();

            e.DrawBackground();

            Color textColor = _playerColors.TryGetValue(username, out Color color) ? color : Color.Black;

            using (Brush brush = new SolidBrush(textColor))
            {
                e.Graphics.DrawString(username, e.Font, brush, e.Bounds);
            }

            e.DrawFocusRectangle();
        }
    }
}