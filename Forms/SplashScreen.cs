using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SEBClone.Forms
{
    /// <summary>
    /// Borderless splash screen displayed for 2.5 seconds on application startup.
    /// Shows SplashScreen.png as the background and a "Loading…" label, then
    /// transitions automatically to <see cref="LoginForm"/>.
    /// </summary>
    internal sealed class SplashScreen : Form
    {
        // ── Controls ──────────────────────────────────────────────────────────
        private readonly Label _loadingLabel;
        private readonly System.Windows.Forms.Timer _timer;

        // ── Constructor ───────────────────────────────────────────────────────

        public SplashScreen()
        {
            SuspendLayout();

            // ── Form properties ───────────────────────────────────────────────
            Text                = string.Empty;
            FormBorderStyle     = FormBorderStyle.None;
            Size                = new Size(600, 400);
            StartPosition       = FormStartPosition.CenterScreen;
            ShowInTaskbar       = false;
            DoubleBuffered      = true;

            // ── Background image ──────────────────────────────────────────────
            string imagePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Assets", "icons", "SplashScreen.png");

            if (File.Exists(imagePath))
            {
                BackgroundImage       = Image.FromFile(imagePath);
                BackgroundImageLayout = ImageLayout.Stretch;
            }
            else
            {
                // Fallback: SEB dark navy if the asset is missing.
                BackColor = Color.FromArgb(30, 39, 60);
            }

            // ── Loading label (bottom-centre, white bold) ─────────────────────
            _loadingLabel = new Label
            {
                Text      = "Loading…",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Point),
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size      = new Size(600, 46),
                Location  = new Point(0, 346),          // 8 px above the bottom edge
                BackColor = Color.Transparent,
            };
            Controls.Add(_loadingLabel);

            // ── Timer — close splash and open LoginForm after 2 500 ms ────────
            _timer          = new System.Windows.Forms.Timer { Interval = 2500 };
            _timer.Tick    += OnTimerTick;

            ResumeLayout(false);

            // Start timer once the handle is created so the message loop is ready.
            Load += (_, _) => _timer.Start();
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void OnTimerTick(object? sender, EventArgs e)
        {
            _timer.Stop();
            var login = new LoginForm();
            login.FormClosed += (_, _) => Close();
            login.Show();
            Hide();
        }

        // ── Cleanup ───────────────────────────────────────────────────────────

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
                _loadingLabel.Dispose();
                BackgroundImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
