using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Utilities;

namespace SEBClone.Forms
{
    /// <summary>
    /// Profile Confirmation screen — Phase 5.
    /// Displays the resolved student name, username, and exam title before the
    /// exam begins.  Applies the same kiosk lockdown as <see cref="LoginForm"/>.
    /// </summary>
    internal sealed class ProfileConfirm : Form
    {
        // ── Asset path helper ─────────────────────────────────────────────────
        private static string Asset(string relativePath) =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);

        // ── Palette (from UI_Guidelines.md) ──────────────────────────────────
        /// <summary>BackgroundBrush #FFF0F0F0</summary>
        private static readonly Color BgColor       = Color.FromArgb(0xFF, 0xF0, 0xF0, 0xF0);
        /// <summary>PrimaryTextBrush #000000</summary>
        private static readonly Color PrimaryText   = Color.Black;
        /// <summary>SecondaryTextBrush #696969</summary>
        private static readonly Color SecondaryText = Color.FromArgb(0x69, 0x69, 0x69);
        /// <summary>Card surface: plain white</summary>
        private static readonly Color CardWhite     = Color.White;
        /// <summary>Accent used for Begin Exam button</summary>
        private static readonly Color AccentBlue    = Color.FromArgb(55, 90, 180);
        /// <summary>Drop-shadow tint</summary>
        private static readonly Color ShadowCol     = Color.FromArgb(60, 0, 0, 0);

        // ── Controls ──────────────────────────────────────────────────────────
        private Panel      _shadowPanel   = null!;
        private Panel      _cardPanel     = null!;
        private PictureBox _logoPicture   = null!;
        private Label      _headerLabel   = null!;
        private Label      _divider       = null!;
        private Label      _nameLabel     = null!;
        private Label      _usernameLabel = null!;
        private Label      _examLabel     = null!;
        private Button     _beginButton   = null!;
        private Button     _backButton    = null!;

        // ── State ─────────────────────────────────────────────────────────────
        private readonly string _studentName;
        private readonly string _username;
        private const string ExamTitle = "Grade 12 ESSLCE Mock Exam";

        // ── Constructor ───────────────────────────────────────────────────────

        /// <param name="studentName">Display name shown on the confirmation card.</param>
        /// <param name="username">Username shown on the confirmation card.</param>
        public ProfileConfirm(string studentName, string username)
        {
            _studentName = studentName;
            _username    = username;

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
            BackColor      = BgColor;
            DoubleBuffered = true;

            string icoPath = Asset(Path.Combine("Assets", "icons", "SafeExamBrowser.ico"));
            if (File.Exists(icoPath))
                Icon = new Icon(icoPath);

            // ── Shadow panel ──────────────────────────────────────────────────
            _shadowPanel = new Panel
            {
                BackColor = ShadowCol,
                Size      = new Size(406, 456),
            };
            Controls.Add(_shadowPanel);

            // ── Card panel ────────────────────────────────────────────────────
            _cardPanel = new RoundedPanel(16)
            {
                BackColor = CardWhite,
                Size      = new Size(400, 450),
            };
            _cardPanel.Paint += OnCardPaint;
            Controls.Add(_cardPanel);

            // ── SEB icon (64×64) ──────────────────────────────────────────────
            _logoPicture = new PictureBox
            {
                Size      = new Size(64, 64),
                SizeMode  = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
            };
            if (File.Exists(icoPath))
                _logoPicture.Image = new Icon(icoPath, 64, 64).ToBitmap();
            _cardPanel.Controls.Add(_logoPicture);

            // ── "Profile Confirmation" header — Segoe UI Bold 20px black ──────
            _headerLabel = new Label
            {
                Text      = "Profile Confirmation",
                Font      = new Font("Segoe UI", 20f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = PrimaryText,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };
            _cardPanel.Controls.Add(_headerLabel);

            // ── Divider ───────────────────────────────────────────────────────
            _divider = new Label
            {
                AutoSize  = false,
                BackColor = Color.FromArgb(220, 225, 235),
                Height    = 1,
            };
            _cardPanel.Controls.Add(_divider);

            // ── Student Name — Segoe UI 14px black ────────────────────────────
            _nameLabel = MakeInfoLabel(
                $"Student Name:  {_studentName}",
                PrimaryText,
                14f);
            _cardPanel.Controls.Add(_nameLabel);

            // ── Username — Segoe UI 14px DimGray (#696969) ────────────────────
            _usernameLabel = MakeInfoLabel(
                $"Username:          {_username}",
                SecondaryText,
                14f);
            _cardPanel.Controls.Add(_usernameLabel);

            // ── Exam Title — Segoe UI 14px black ──────────────────────────────
            _examLabel = MakeInfoLabel(
                $"Exam Title:          {ExamTitle}",
                PrimaryText,
                14f);
            _cardPanel.Controls.Add(_examLabel);

            // ── "Begin Exam" — primary button ─────────────────────────────────
            _beginButton = new Button
            {
                Text      = "Begin Exam",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.White,
                BackColor = AccentBlue,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
                UseVisualStyleBackColor = false,
            };
            _beginButton.FlatAppearance.BorderSize           = 0;
            _beginButton.FlatAppearance.MouseOverBackColor   = Color.FromArgb(70, 110, 200);
            _beginButton.FlatAppearance.MouseDownBackColor   = Color.FromArgb(40,  70, 150);
            _beginButton.Click      += OnBeginExamClick;
            _beginButton.MouseEnter += (_, _) => _beginButton.BackColor = Color.FromArgb(70, 110, 200);
            _beginButton.MouseLeave += (_, _) => _beginButton.BackColor = AccentBlue;
            _cardPanel.Controls.Add(_beginButton);

            // ── "Back" — secondary button ─────────────────────────────────────
            _backButton = new Button
            {
                Text      = "Back",
                Font      = new Font("Segoe UI", 12f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = PrimaryText,
                BackColor = Color.FromArgb(225, 225, 225),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
                UseVisualStyleBackColor = false,
            };
            _backButton.FlatAppearance.BorderSize           = 0;
            _backButton.FlatAppearance.MouseOverBackColor   = Color.FromArgb(210, 210, 210);
            _backButton.FlatAppearance.MouseDownBackColor   = Color.FromArgb(190, 190, 190);
            _backButton.Click      += OnBackClick;
            _backButton.MouseEnter += (_, _) => _backButton.BackColor = Color.FromArgb(210, 210, 210);
            _backButton.MouseLeave += (_, _) => _backButton.BackColor = Color.FromArgb(225, 225, 225);
            _cardPanel.Controls.Add(_backButton);

            // ── Layout ────────────────────────────────────────────────────────
            LayoutCard();

            _cardPanel.BringToFront();
            ResumeLayout(false);

            Load += OnFormLoad;
        }

        // ── Layout ────────────────────────────────────────────────────────────

        private void LayoutCard()
        {
            const int cw = 400;    // card width
            const int mx = 28;     // horizontal margin
            int aw = cw - mx * 2;  // available width
            int y  = 28;

            // Icon — centred
            _logoPicture.Location = new Point((cw - 64) / 2, y);
            y += 76;

            // Header
            _headerLabel.Location = new Point(mx, y);
            _headerLabel.Size     = new Size(aw, 30);
            y += 38;

            // Divider
            _divider.Location = new Point(mx, y);
            _divider.Size     = new Size(aw, 1);
            y += 20;

            // Info rows
            _nameLabel.Location = new Point(mx, y);
            _nameLabel.Size     = new Size(aw, 22);
            y += 34;

            _usernameLabel.Location = new Point(mx, y);
            _usernameLabel.Size     = new Size(aw, 22);
            y += 34;

            _examLabel.Location = new Point(mx, y);
            _examLabel.Size     = new Size(aw, 22);
            y += 46;

            // Button row: [Back (35%) gap (5%) BeginExam (60%)]
            int backW  = (int)(aw * 0.35);
            int beginW = aw - backW - 8;  // 8 px gap

            _backButton.Location  = new Point(mx, y);
            _backButton.Size      = new Size(backW, 46);

            _beginButton.Location = new Point(mx + backW + 8, y);
            _beginButton.Size     = new Size(beginW, 46);
        }

        // ── Form Load ─────────────────────────────────────────────────────────

        private void OnFormLoad(object? sender, EventArgs e)
        {
            int cx = (ClientSize.Width  - _cardPanel.Width)  / 2;
            int cy = (ClientSize.Height - _cardPanel.Height) / 2;

            _cardPanel.Location   = new Point(cx, cy);
            _shadowPanel.Location = new Point(cx + 4, cy + 4);
        }

        // ── Card accent stripe ────────────────────────────────────────────────

        private void OnCardPaint(object? sender, PaintEventArgs e)
        {
            using var brush = new System.Drawing.SolidBrush(AccentBlue);
            e.Graphics.FillRectangle(brush, 0, 0, _cardPanel.Width, 4);
        }

        // ── Button handlers ───────────────────────────────────────────────────

        private void OnBeginExamClick(object? sender, EventArgs e)
        {
            var exam = new ExamForm(_studentName);
            exam.Show();
            this.Close();
        }

        private void OnBackClick(object? sender, EventArgs e)
        {
            // Re-open LoginForm before closing this form so the app doesn't exit.
            var login = new LoginForm();
            login.Show();
            this.Close();
        }

        // ── Factory helpers ───────────────────────────────────────────────────

        private static Label MakeInfoLabel(string text, Color foreColor, float sizePx) =>
            new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", sizePx, FontStyle.Regular, GraphicsUnit.Pixel),
                ForeColor = foreColor,
                AutoSize  = false,
                BackColor = Color.Transparent,
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
}
