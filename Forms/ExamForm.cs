using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Utilities;
using Timer = System.Windows.Forms.Timer;

namespace SEBClone.Forms
{
    /// <summary>
    /// The main exam screen shell with SEB-style bottom taskbar — Phase 6b.
    /// </summary>
    internal sealed class ExamForm : Form
    {
        // ── Asset path helper ─────────────────────────────────────────────────
        private static string Asset(string relativePath) =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);

        // ── Palette (from UI_Guidelines.md) ──────────────────────────────────
        private static readonly Color BgColor       = Color.FromArgb(0xFF, 0xF0, 0xF0, 0xF0);
        private static readonly Color SecondaryText = Color.FromArgb(0x69, 0x69, 0x69);

        // ── Controls ──────────────────────────────────────────────────────────
        private Panel      _taskbarPanel  = null!;
        private PictureBox _logoPicture   = null!;
        private Label      _nameLabel     = null!;
        private Label      _clockLabel    = null!;
        private Button     _quitButton    = null!;
        private Panel      _contentPanel  = null!;
        private Label      _placeholder   = null!;
        private Timer      _clockTimer    = null!;

        // ── State ─────────────────────────────────────────────────────────────
        private readonly string _studentName;

        // ── Constructor ───────────────────────────────────────────────────────
        public ExamForm(string studentName)
        {
            _studentName = studentName;

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
            Text           = "Safe Exam Browser";
            BackColor      = BgColor;
            DoubleBuffered = true;

            string icoPath = Asset(Path.Combine("Assets", "icons", "SafeExamBrowser.ico"));
            if (File.Exists(icoPath))
                Icon = new Icon(icoPath);

            // ── Bottom Taskbar ────────────────────────────────────────────────
            _taskbarPanel = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 40,
                BackColor = BgColor,
            };
            _taskbarPanel.Paint  += OnTaskbarPaint;
            _taskbarPanel.Resize += (s, e) => LayoutTaskbarControls();

            // ── SEB Logo PictureBox ───────────────────────────────────────────
            _logoPicture = new PictureBox
            {
                Size      = new Size(40, 40),
                SizeMode  = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
            };
            if (File.Exists(icoPath))
                _logoPicture.Image = new Icon(icoPath, 40, 40).ToBitmap();
            _taskbarPanel.Controls.Add(_logoPicture);

            // ── Student Name Label ────────────────────────────────────────────
            _nameLabel = new Label
            {
                Text      = $"Student: {_studentName}",
                Font      = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Pixel),
                ForeColor = SecondaryText,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = true,
            };
            _taskbarPanel.Controls.Add(_nameLabel);

            // ── Live Clock Label ──────────────────────────────────────────────
            _clockLabel = new Label
            {
                Text      = DateTime.Now.ToString("HH:mm:ss"),
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.Black,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                Padding   = new Padding(10, 0, 10, 0),
                AutoSize  = true,
            };
            _clockLabel.SizeChanged += (s, e) => LayoutTaskbarControls();
            _taskbarPanel.Controls.Add(_clockLabel);

            // ── Quit Button ───────────────────────────────────────────────────
            _quitButton = new Button
            {
                Text      = "Quit",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(220, 40, 40),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
                UseVisualStyleBackColor = false,
            };
            _quitButton.FlatAppearance.BorderSize = 0;
            _quitButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 60, 60);
            _quitButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(180, 20, 20);
            _quitButton.MouseEnter += (_, _) => _quitButton.BackColor = Color.FromArgb(240, 60, 60);
            _quitButton.MouseLeave += (_, _) => _quitButton.BackColor = Color.FromArgb(220, 40, 40);
            _quitButton.Click      += (_, _) => Application.Exit();
            _taskbarPanel.Controls.Add(_quitButton);

            // ── Clock Timer ───────────────────────────────────────────────────
            _clockTimer = new Timer
            {
                Interval = 1000
            };
            _clockTimer.Tick += OnClockTick;
            _clockTimer.Start();

            // ── Main Content Area ─────────────────────────────────────────────
            _contentPanel = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
            };

            _placeholder = new Label
            {
                Text      = "Exam content goes here — Phase 6b",
                Font      = new Font("Segoe UI", 16f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.Black,
                BackColor = Color.Transparent,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
            };
            _contentPanel.Controls.Add(_placeholder);

            // ── Add to Form Controls ──────────────────────────────────────────
            Controls.Add(_contentPanel);
            Controls.Add(_taskbarPanel);

            // Perform initial layout calculation
            LayoutTaskbarControls();

            ResumeLayout(false);
        }

        // ── Layout calculation ────────────────────────────────────────────────
        private void LayoutTaskbarControls()
        {
            if (_taskbarPanel == null || _logoPicture == null || _nameLabel == null || _quitButton == null || _clockLabel == null)
                return;

            int w = _taskbarPanel.Width;

            // Left side
            _logoPicture.Location = new Point(0, 0);
            _logoPicture.Size     = new Size(40, 40);

            // Center-left (aligned vertically, starting after the logo icon)
            _nameLabel.Location   = new Point(48, (40 - _nameLabel.Height) / 2);

            // Far right
            _quitButton.Location  = new Point(w - 40, 0);
            _quitButton.Size      = new Size(40, 40);

            // Right side, to the left of the Quit button
            _clockLabel.Location  = new Point(w - 40 - _clockLabel.Width, (40 - _clockLabel.Height) / 2);
        }

        // ── Clock Update Event ────────────────────────────────────────────────
        private void OnClockTick(object? sender, EventArgs e)
        {
            _clockLabel.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        // ── Taskbar Paint Event (Draw 1px LightGray Top Border) ───────────────
        private void OnTaskbarPaint(object? sender, PaintEventArgs e)
        {
            using var pen = new Pen(Color.LightGray, 1);
            e.Graphics.DrawLine(pen, 0, 0, _taskbarPanel.Width, 0);
        }

        // ── Cleanup ───────────────────────────────────────────────────────────
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _logoPicture?.Image?.Dispose();
                _clockTimer?.Dispose();
                Icon?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
