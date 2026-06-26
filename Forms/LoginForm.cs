using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using SEBClone.Models;
using Utilities;

namespace SEBClone.Forms
{
    /// <summary>
    /// Full-screen kiosk login form.
    /// Applies <see cref="LockdownManager"/> on construction and presents a
    /// centred white card containing the exam logo, title, username/exam-code
    /// inputs, and a "Log in" button.
    /// </summary>
    internal sealed class LoginForm : Form
    {
        // ── P/Invoke for Cue Banner (Placeholders) ────────────────────────────
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        // ── Asset path helper ─────────────────────────────────────────────────
        private static string Asset(string relativePath) =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);

        // ── Palette ───────────────────────────────────────────────────────────
        private static readonly Color PageBgCol     = Color.FromArgb(247, 248, 252); // #F7F8FC light lavender-grey
        private static readonly Color TitleBlue     = Color.FromArgb(30, 64, 175);   // #1E40AF bold title blue
        private static readonly Color ButtonBlue    = Color.FromArgb(50, 99, 195);   // #3263C3 solid blue login button
        private static readonly Color ButtonHover   = Color.FromArgb(38, 77, 155);   // darker blue on hover
        private static readonly Color ButtonPressed = Color.FromArgb(26, 55, 112);   // deep blue on press
        private static readonly Color BorderGrey    = Color.FromArgb(209, 213, 219); // #D1D5DB light grey input border

        // ── Controls ──────────────────────────────────────────────────────────
        private Panel       _cardPanel    = null!;
        private PictureBox  _logoPicture  = null!;
        private Label       _titleLabel   = null!;
        private Label       _userLabel    = null!;
        private TextBox     _userBox      = null!;
        private Label       _codeLabel    = null!;
        private TextBox     _codeBox      = null!;
        private Button      _startButton  = null!;

        // Containers for rounded inputs
        private Panel       _userBoxContainer = null!;
        private Panel       _codeBoxContainer = null!;

        // ── Constructor ───────────────────────────────────────────────────────

        public LoginForm()
        {
            InitializeComponent();

            // Apply kiosk lockdown AFTER InitializeComponent so the handle
            // has not been created yet (avoids recreating the native window).
            LockdownManager.ApplyLockdown(this);
        }

        // ── Kiosk: strip min/max buttons from the native window style ─────────

        protected override CreateParams CreateParams =>
            LockdownManager.GetLockedCreateParams(base.CreateParams);

        // ── Component initialisation ──────────────────────────────────────────

        private void InitializeComponent()
        {
            SuspendLayout();

            // ── Form ──────────────────────────────────────────────────────────
            Text           = "Safe Exam Browser";
            BackColor      = PageBgCol;
            DoubleBuffered = true;
            ShowInTaskbar = true;
            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "icons", "Application.ico");
            if (File.Exists(icoPath)) Icon = new Icon(icoPath);
            Paint         += OnFormPaint;

            // ── Card panel (Transparent background, paints white rounded rect) ─
            _cardPanel = new RoundedPanel(16)
            {
                Size = new Size(550, 480),
            };
            Controls.Add(_cardPanel);

            // ── Logo (168x132, cropped static PictureBox) ─────────────────────
            _logoPicture = new PictureBox
            {
                SizeMode  = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
            };
            // Pointed to Assets/images/pagelogo.png as requested
            string logoPath = Asset(Path.Combine("Assets", "images", "pagelogo.png"));
            if (File.Exists(logoPath))
                _logoPicture.Image = Image.FromFile(logoPath);
            _cardPanel.Controls.Add(_logoPicture);

            // ── Title ─────────────────────────────────────────────────────────
            _titleLabel = new Label
            {
                Text      = "Grade 12 - National Exam",
                Font      = new Font("Segoe UI", 26f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = TitleBlue,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };
            _cardPanel.Controls.Add(_titleLabel);

            // ── Username Text Box & Container ─────────────────────────────────
            // Maintain invisible labels to prevent breaking any future checks
            _userLabel         = new Label { Visible = false };
            _userBox           = new TextBox { PasswordChar = '\0' };
            _userBox.Font      = new Font("Segoe UI", 12f, GraphicsUnit.Point);
            _userBox.HandleCreated += (s, e) => SendMessage(_userBox.Handle, EM_SETCUEBANNER, 1, "Username");
            
            _userBoxContainer  = new RoundedTextBoxContainer(_userBox, 8);
            _cardPanel.Controls.Add(_userBoxContainer);

            // ── Password (Exam Code) Text Box & Container ─────────────────────
            _codeLabel         = new Label { Visible = false };
            _codeBox           = new TextBox { PasswordChar = '●' };
            _codeBox.Font      = new Font("Segoe UI", 12f, GraphicsUnit.Point);
            _codeBox.HandleCreated += (s, e) => SendMessage(_codeBox.Handle, EM_SETCUEBANNER, 1, "Password");
            
            _codeBoxContainer  = new RoundedTextBoxContainer(_codeBox, 8);
            _cardPanel.Controls.Add(_codeBoxContainer);

            // ── Solid Blue "Log in" button (Custom Rounded Button) ────────────
            _startButton = new RoundedButton(8)
            {
                Text         = "Log in",
                Font         = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor    = Color.White,
                NormalColor  = ButtonBlue,
                HoverColor   = ButtonHover,
                PressedColor = ButtonPressed,
                Cursor       = Cursors.Hand,
            };
            _startButton.Click += OnStartExamClick;
            _cardPanel.Controls.Add(_startButton);

            // ── Layout — relative to card panel ───────────────────────────────
            LayoutCard();

            _cardPanel.BringToFront();
            ResumeLayout(false);

            // Centre the card on the screen after the form bounds are applied
            Load += OnFormLoad;
        }

        // ── Layout ────────────────────────────────────────────────────────────

        private void LayoutCard()
        {
            const int cw = 550;   // card width
            const int mx = 50;    // horizontal padding
            int aw = cw - mx * 2; // content width (450px)
            int y  = 40;          // top padding

            // Logo
            _logoPicture.Size     = new Size(168, 132);
            _logoPicture.Location = new Point((cw - 168) / 2, y);
            y += 132 + 15;

            // Title
            _titleLabel.Location = new Point(mx, y);
            _titleLabel.Size     = new Size(aw, 40);
            y += 40 + 15;

            // Username input
            _userBoxContainer.Location = new Point(mx, y);
            _userBoxContainer.Size     = new Size(aw, 50);
            y += 50 + 15;

            // Password input
            _codeBoxContainer.Location = new Point(mx, y);
            _codeBoxContainer.Size     = new Size(aw, 50);
            y += 50 + 20;

            // Button
            _startButton.Location = new Point(mx, y);
            _startButton.Size     = new Size(aw, 48);
        }

        // ── Form Load — centre the card now that Bounds are set ───────────────

        private void OnFormLoad(object? sender, EventArgs e)
        {
            int cx = (ClientSize.Width  - _cardPanel.Width)  / 2;
            int cy = (ClientSize.Height - _cardPanel.Height) / 2;

            _cardPanel.Location = new Point(cx, cy);
            Invalidate(); // trigger Paint to draw the soft shadow
        }

        // ── Form Paint — Draw smooth drop shadow behind the card ──────────────

        private void OnFormPaint(object? sender, PaintEventArgs e)
        {
            if (_cardPanel != null && _cardPanel.Width > 0 && _cardPanel.Height > 0)
            {
                DrawSoftShadow(e.Graphics, _cardPanel.Bounds, 16, 12);
            }
        }

        private void DrawSoftShadow(Graphics g, Rectangle rect, int radius, int shadowSize)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            for (int i = 1; i <= shadowSize; i++)
            {
                int alpha = (int)(15 * (1.0 - (double)i / shadowSize));
                if (alpha <= 0) continue;
                
                using (var pen = new Pen(Color.FromArgb(alpha, 0, 0, 0), i * 1.5f))
                {
                    pen.LineJoin = LineJoin.Round;
                    using (var path = GetRoundRectPath(
                        rect.X - i + 2, 
                        rect.Y - i + 4, // slight vertical offset for drop shadow
                        rect.Width + (i * 2) - 4, 
                        rect.Height + (i * 2) - 8, 
                        radius + i))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }
        }

        private static GraphicsPath GetRoundRectPath(float x, float y, float width, float height, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float r2 = radius * 2;
            if (r2 > width) r2 = width;
            if (r2 > height) r2 = height;
            
            path.AddArc(x, y, r2, r2, 180, 90);
            path.AddArc(x + width - r2, y, r2, r2, 270, 90);
            path.AddArc(x + width - r2, y + height - r2, r2, r2, 0, 90);
            path.AddArc(x, y + height - r2, r2, r2, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void OnStartExamClick(object? sender, EventArgs e)
        {
            string username = _userBox.Text.Trim();
            string examCode = _codeBox.Text.Trim();

            // Locate users.json next to the exe.
            string usersJsonPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Data", "users.json");

            if (!File.Exists(usersJsonPath))
            {
                MessageBox.Show(
                    $"Configuration error: users.json not found.\n{usersJsonPath}",
                    "Safe Exam Browser",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            UserList? userList;
            try
            {
                string json = File.ReadAllText(usersJsonPath);
                userList = JsonSerializer.Deserialize<UserList>(json);
            }
            catch (JsonException ex)
            {
                MessageBox.Show(
                    $"Failed to read user data.\n{ex.Message}",
                    "Safe Exam Browser",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            bool match = userList?.Students?.Exists(u =>
                string.Equals(u.Username,  username, StringComparison.Ordinal) &&
                string.Equals(u.SecretKey, examCode, StringComparison.Ordinal)) == true;

            if (!match)
            {
                MessageBox.Show(
                    "Invalid username or exam code.",
                    "Safe Exam Browser",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Credentials validated — open the Course Selection screen.
            var confirm = new CourseSelectForm(username);
            confirm.Show();
            this.Close();
        }

        // ── Cleanup ───────────────────────────────────────────────────────────

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _logoPicture.Image?.Dispose();
                Icon?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // ── Rounded-corner panel helper ───────────────────────────────────────────

    /// <summary>
    /// A <see cref="Panel"/> whose visible region paints as a white rounded rectangle with anti-aliased edges.
    /// </summary>
    internal sealed class RoundedPanel : Panel
    {
        private readonly int _radius;

        public RoundedPanel(int cornerRadius)
        {
            _radius        = cornerRadius;
            DoubleBuffered = true;
            BackColor      = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (var path = new GraphicsPath())
            {
                float r2 = _radius * 2;
                path.AddArc(0, 0, r2, r2, 180, 90);
                path.AddArc(Width - 1 - r2, 0, r2, r2, 270, 90);
                path.AddArc(Width - 1 - r2, Height - 1 - r2, r2, r2, 0, 90);
                path.AddArc(0, Height - 1 - r2, r2, r2, 90, 90);
                path.CloseFigure();

                using (var brush = new SolidBrush(Color.White))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }
        }
    }

    // ── Rounded Text Box Container ────────────────────────────────────────────

    /// <summary>
    /// A container for a borderless <see cref="TextBox"/> that draws a rounded background and border.
    /// </summary>
    internal sealed class RoundedTextBoxContainer : Panel
    {
        private readonly TextBox _textBox;
        private readonly int _radius;
        private readonly Color _borderColor = Color.FromArgb(209, 213, 219);
        private bool _isFocused = false;

        public RoundedTextBoxContainer(TextBox textBox, int radius)
        {
            _textBox       = textBox;
            _radius        = radius;
            DoubleBuffered = true;
            BackColor      = Color.Transparent;

            _textBox.BorderStyle = BorderStyle.None;
            _textBox.BackColor   = Color.White;
            _textBox.GotFocus   += (s, e) => { _isFocused = true; Invalidate(); };
            _textBox.LostFocus  += (s, e) => { _isFocused = false; Invalidate(); };

            // Focus textbox if user clicks the surrounding container panel
            Click += (s, e) => _textBox.Focus();

            Controls.Add(_textBox);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (_textBox != null)
            {
                // Align TextBox vertically within container height
                _textBox.Location = new Point(14, (Height - _textBox.Height) / 2);
                _textBox.Width    = Width - 28;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (var path = new GraphicsPath())
            {
                float r2 = _radius * 2;
                path.AddArc(0, 0, r2, r2, 180, 90);
                path.AddArc(Width - 1 - r2, 0, r2, r2, 270, 90);
                path.AddArc(Width - 1 - r2, Height - 1 - r2, r2, r2, 0, 90);
                path.AddArc(0, Height - 1 - r2, r2, r2, 90, 90);
                path.CloseFigure();

                using (var brush = new SolidBrush(Color.White))
                {
                    e.Graphics.FillPath(brush, path);
                }

                Color currentBorderColor = _isFocused ? Color.FromArgb(59, 130, 246) : _borderColor;
                using (var pen = new Pen(currentBorderColor, 1.5f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }
    }

    // ── Rounded Button ────────────────────────────────────────────────────────

    /// <summary>
    /// A custom <see cref="Button"/> drawn with rounded corners, custom hover states, and anti-aliasing.
    /// </summary>
    internal sealed class RoundedButton : Button
    {
        private readonly int _radius;
        private Color _normalColor;
        private Color _hoverColor;
        private Color _pressedColor;
        private bool _isHovered = false;
        private bool _isPressed = false;

        public RoundedButton(int radius)
        {
            _radius                   = radius;
            DoubleBuffered            = true;
            FlatStyle                 = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
        }

        public Color NormalColor
        {
            get => _normalColor;
            set { _normalColor = value; BackColor = value; }
        }

        public Color HoverColor
        {
            get => _hoverColor;
            set => _hoverColor = value;
        }

        public Color PressedColor
        {
            get => _pressedColor;
            set => _pressedColor = value;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            _isPressed = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color currentBg = _normalColor;
            if (_isPressed) currentBg = _pressedColor;
            else if (_isHovered) currentBg = _hoverColor;

            using (var path = new GraphicsPath())
            {
                float r2 = _radius * 2;
                path.AddArc(0, 0, r2, r2, 180, 90);
                path.AddArc(Width - 1 - r2, 0, r2, r2, 270, 90);
                path.AddArc(Width - 1 - r2, Height - 1 - r2, r2, r2, 0, 90);
                path.AddArc(0, Height - 1 - r2, r2, r2, 90, 90);
                path.CloseFigure();

                using (var brush = new SolidBrush(currentBg))
                {
                    g.FillPath(brush, path);
                }
            }

            TextRenderer.DrawText(
                g,
                Text,
                Font,
                ClientRectangle,
                ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
            );
        }
    }
}
