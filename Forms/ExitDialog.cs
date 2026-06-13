using System;
using System.Drawing;
using System.Windows.Forms;

namespace SEBClone.Forms
{
    /// <summary>
    /// A small, normal-bordered modal dialog that prompts for the quit password
    /// before allowing the application to exit.  No kiosk / lockdown is applied.
    /// </summary>
    internal sealed class ExitDialog : Form
    {
        // ── Quit password ─────────────────────────────────────────────────────
        private const string QuitPassword = "admin123";

        // ── Controls ──────────────────────────────────────────────────────────
        private Label     _promptLabel    = null!;
        private TextBox   _passwordBox    = null!;
        private Button    _quitButton     = null!;
        private Button    _cancelButton   = null!;

        // ── Constructor ───────────────────────────────────────────────────────
        public ExitDialog()
        {
            InitializeComponent();
        }

        // ── Component initialisation ──────────────────────────────────────────
        private void InitializeComponent()
        {
            SuspendLayout();

            // ── Form properties ───────────────────────────────────────────────
            Text                  = "Quit Safe Exam Browser";
            ClientSize            = new Size(360, 220);
            StartPosition         = FormStartPosition.CenterScreen;
            FormBorderStyle       = FormBorderStyle.FixedDialog;
            MaximizeBox           = false;
            MinimizeBox           = false;
            TopMost               = true;
            BackColor             = Color.FromArgb(0xF0, 0xF0, 0xF0);
            DoubleBuffered        = true;
            KeyPreview            = true;

            // Close on Escape key
            KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };

            // ── Prompt label ──────────────────────────────────────────────────
            _promptLabel = new Label
            {
                Text      = "Enter quit password to exit:",
                Font      = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel),
                ForeColor = Color.Black,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(20, 24),
            };
            Controls.Add(_promptLabel);

            // ── Password TextBox ──────────────────────────────────────────────
            _passwordBox = new TextBox
            {
                Font         = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel),
                PasswordChar = '●',
                Location     = new Point(20, 56),
                Width        = 360 - 40,            // fills panel with 20px margin on each side
                BorderStyle  = BorderStyle.FixedSingle,
            };
            // Accept Enter as Quit attempt
            _passwordBox.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    AttemptQuit();
                }
            };
            Controls.Add(_passwordBox);

            // ── Quit button ───────────────────────────────────────────────────
            _quitButton = new Button
            {
                Text      = "Quit",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0xD3, 0x2F, 0x2F),
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(100, 34),
                Location  = new Point(360 / 2 - 100 - 10, 220 - 34 - 20),
                Cursor    = Cursors.Hand,
                UseVisualStyleBackColor = false,
            };
            _quitButton.FlatAppearance.BorderSize = 0;
            _quitButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(0xB7, 0x1C, 0x1C);
            _quitButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(0x8B, 0x00, 0x00);
            _quitButton.Click += OnQuitButtonClick;
            Controls.Add(_quitButton);

            // ── Cancel button ─────────────────────────────────────────────────
            _cancelButton = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Pixel),
                ForeColor = Color.Black,
                BackColor = Color.FromArgb(0xCC, 0xCC, 0xCC),
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(100, 34),
                Location  = new Point(360 / 2 + 10, 220 - 34 - 20),
                Cursor    = Cursors.Hand,
                UseVisualStyleBackColor = false,
            };
            _cancelButton.FlatAppearance.BorderSize = 0;
            _cancelButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(0xBB, 0xBB, 0xBB);
            _cancelButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(0xAA, 0xAA, 0xAA);
            _cancelButton.Click += OnCancelButtonClick;
            Controls.Add(_cancelButton);

            ResumeLayout(false);
        }

        // ── Event handlers ────────────────────────────────────────────────────
        private void OnQuitButtonClick(object? sender, EventArgs e) => AttemptQuit();

        private void OnCancelButtonClick(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void AttemptQuit()
        {
            if (_passwordBox.Text == QuitPassword)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(
                    "Incorrect password.",
                    "Access Denied",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                _passwordBox.Clear();
                _passwordBox.Focus();
            }
        }
    }
}
