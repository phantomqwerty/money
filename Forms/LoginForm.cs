using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Utilities;

namespace SEBClone.Forms
{
    /// <summary>
    /// Full-screen kiosk login form.
    /// Applies <see cref="LockdownManager"/> on construction and presents a
    /// centred white card containing the SEB logo, title, username/exam-code
    /// inputs, and a "Start Exam" button.
    /// </summary>
    internal sealed class LoginForm : Form
    {
        // ── Asset path helper ─────────────────────────────────────────────────
        private static string Asset(string relativePath) =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);

        // ── Palette ───────────────────────────────────────────────────────────
        private static readonly Color NavyDark   = Color.FromArgb(30,  39,  60);
        private static readonly Color NavyMid    = Color.FromArgb(42,  54,  82);
        private static readonly Color AccentBlue = Color.FromArgb(55,  90, 180);
        private static readonly Color LabelGrey  = Color.FromArgb(90, 100, 120);
        private static readonly Color ShadowCol  = Color.FromArgb(60,   0,   0,   0);

        // ── Controls ──────────────────────────────────────────────────────────
        private Panel       _shadowPanel  = null!;
        private Panel       _cardPanel    = null!;
        private PictureBox  _logoPicture  = null!;
        private Label       _titleLabel   = null!;
        private Label       _divider      = null!;
        private Label       _userLabel    = null!;
        private TextBox     _userBox      = null!;
        private Label       _codeLabel    = null!;
        private TextBox     _codeBox      = null!;
        private Button      _startButton  = null!;

        // ── Constructor ───────────────────────────────────────────────────────

        public LoginForm()
        {
            InitializeComponent();

            // Apply kiosk lockdown AFTER InitializeComponent so the handle
            // has not been created yet (avoids recreating the native window).
            LockdownManager.ApplyLockdown(this);
            FormClosed += (_, _) => Application.Exit();
        }

        // ── Kiosk: strip min/max buttons from the native window style ─────────

        protected override CreateParams CreateParams =>
            LockdownManager.GetLockedCreateParams(base.CreateParams);

        // ── Component initialisation ──────────────────────────────────────────

        private void InitializeComponent()
        {
            SuspendLayout();

            // ── Form ──────────────────────────────────────────────────────────
            Text          = "Safe Exam Browser";
            BackColor     = NavyDark;
            DoubleBuffered = true;

            string icoPath = Asset(Path.Combine("Assets", "icons", "SafeExamBrowser.ico"));
            if (File.Exists(icoPath))
                Icon = new Icon(icoPath);

            // ── Shadow panel (offset behind the card for depth) ───────────────
            _shadowPanel = new Panel
            {
                BackColor = ShadowCol,
                Size      = new Size(406, 486),
            };
            Controls.Add(_shadowPanel);

            // ── Card panel ────────────────────────────────────────────────────
            _cardPanel = new RoundedPanel(16)
            {
                BackColor = Color.White,
                Size      = new Size(400, 480),
            };
            _cardPanel.Paint += OnCardPaint;
            Controls.Add(_cardPanel);

            // ── Logo (64×64 from .ico) ────────────────────────────────────────
            _logoPicture = new PictureBox
            {
                Size     = new Size(64, 64),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
            };
            if (File.Exists(icoPath))
                _logoPicture.Image = new Icon(icoPath, 64, 64).ToBitmap();
            _cardPanel.Controls.Add(_logoPicture);

            // ── Title ─────────────────────────────────────────────────────────
            _titleLabel = new Label
            {
                Text      = "Safe Exam Browser — Login",
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = NavyDark,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };
            _cardPanel.Controls.Add(_titleLabel);

            // ── Thin divider ──────────────────────────────────────────────────
            _divider = new Label
            {
                AutoSize  = false,
                BackColor = Color.FromArgb(220, 225, 235),
                Height    = 1,
            };
            _cardPanel.Controls.Add(_divider);

            // ── Username ──────────────────────────────────────────────────────
            _userLabel = MakeFieldLabel("Username");
            _cardPanel.Controls.Add(_userLabel);

            _userBox = MakeTextBox(false);
            _cardPanel.Controls.Add(_userBox);

            // ── Exam Code ─────────────────────────────────────────────────────
            _codeLabel = MakeFieldLabel("Exam Code");
            _cardPanel.Controls.Add(_codeLabel);

            _codeBox = MakeTextBox(true);
            _cardPanel.Controls.Add(_codeBox);

            // ── Start Exam button ─────────────────────────────────────────────
            _startButton = new Button
            {
                Text      = "Start Exam",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.White,
                BackColor = AccentBlue,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
                UseVisualStyleBackColor = false,
            };
            _startButton.FlatAppearance.BorderSize        = 0;
            _startButton.FlatAppearance.MouseOverBackColor  = Color.FromArgb(70, 110, 200);
            _startButton.FlatAppearance.MouseDownBackColor  = Color.FromArgb(40,  70, 150);
            _startButton.Click      += OnStartExamClick;
            _startButton.MouseEnter += (_, _) => _startButton.BackColor = Color.FromArgb(70, 110, 200);
            _startButton.MouseLeave += (_, _) => _startButton.BackColor = AccentBlue;
            _cardPanel.Controls.Add(_startButton);

            // ── Layout — all relative to card panel ───────────────────────────
            LayoutCard();

            ResumeLayout(false);

            // Centre the card on the screen after the form bounds are applied.
            Load += OnFormLoad;
        }

        // ── Layout ────────────────────────────────────────────────────────────

        private void LayoutCard()
        {
            const int cw = 400;   // card width
            const int mx = 24;    // horizontal margin
            int aw = cw - mx * 2; // available width for controls
            int y  = 30;

            // Logo — centred
            _logoPicture.Location = new Point((cw - 64) / 2, y);
            y += 76;

            // Title
            _titleLabel.Location = new Point(mx, y);
            _titleLabel.Size     = new Size(aw, 38);
            y += 44;

            // Divider
            _divider.Location = new Point(mx, y);
            _divider.Size     = new Size(aw, 1);
            y += 18;

            // Username
            _userLabel.Location = new Point(mx, y);
            _userLabel.Size     = new Size(aw, 20);
            y += 24;

            _userBox.Location = new Point(mx, y);
            _userBox.Size     = new Size(aw, 32);
            y += 50;

            // Exam Code
            _codeLabel.Location = new Point(mx, y);
            _codeLabel.Size     = new Size(aw, 20);
            y += 24;

            _codeBox.Location = new Point(mx, y);
            _codeBox.Size     = new Size(aw, 32);
            y += 56;

            // Button
            _startButton.Location = new Point(mx, y);
            _startButton.Size     = new Size(aw, 46);
        }

        // ── Form Load — centre the card now that Bounds are set ───────────────

        private void OnFormLoad(object? sender, EventArgs e)
        {
            int cx = (ClientSize.Width  - _cardPanel.Width)  / 2;
            int cy = (ClientSize.Height - _cardPanel.Height) / 2;

            _cardPanel.Location   = new Point(cx, cy);
            _shadowPanel.Location = new Point(cx + 4, cy + 4); // 4 px drop-shadow
        }

        // ── Card panel paint — draw subtle top-accent bar ─────────────────────

        private void OnCardPaint(object? sender, PaintEventArgs e)
        {
            // 4-pixel accent stripe along the top of the card in AccentBlue.
            using var brush = new SolidBrush(AccentBlue);
            e.Graphics.FillRectangle(brush, 0, 0, _cardPanel.Width, 4);
        }

        // ── Button click — Phase 5 placeholder ───────────────────────────────

        private void OnStartExamClick(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "Login successful — Profile screen next",
                "Safe Exam Browser",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // ── Factory helpers ───────────────────────────────────────────────────

        private static Label MakeFieldLabel(string text) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = LabelGrey,
            AutoSize  = false,
            BackColor = Color.Transparent,
        };

        private static TextBox MakeTextBox(bool masked) => new TextBox
        {
            Font         = new Font("Segoe UI", 11f, GraphicsUnit.Point),
            BorderStyle  = BorderStyle.FixedSingle,
            PasswordChar = masked ? '●' : '\0',
        };

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
    /// A <see cref="Panel"/> whose visible region is clipped to a rounded rectangle.
    /// </summary>
    internal sealed class RoundedPanel : Panel
    {
        private readonly int _radius;

        public RoundedPanel(int cornerRadius)
        {
            _radius      = cornerRadius;
            DoubleBuffered = true;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // Rebuild the clip region whenever the panel is resized.
            Region = Region.FromHrgn(CreateRoundRectRgn(
                0, 0, Width, Height, _radius, _radius));
        }

        // P/Invoke: creates a rounded-rectangle region handle.
        [System.Runtime.InteropServices.DllImport("Gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);
    }
}
