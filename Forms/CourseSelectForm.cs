using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Utilities;

namespace SEBClone.Forms
{
    public class CourseSelectForm : Form
    {
        private readonly string _studentName;
        private readonly string _loginDate;

        private const int NavbarHeight = 56;
        private const int CardWidth = 260;
        private const int CardHeight = 260;
        private const int CardImageHeight = 160;
        private const int SidebarCardW = 300;
        private const int SidebarCardH = 220;
        private const int SidebarMarginR = 20;  // gap from right edge
        private const int SidebarMarginT = 48;  // gap below navbar

        private Panel _navbar = null!;
        private Panel _sidebar = null!;
        private Panel _whiteArea = null!;
        private TextBox _searchBox = null!;
        private ComboBox _sortDropdown = null!;

        public CourseSelectForm(string studentName)
        {
            _studentName = studentName;
            _loginDate = DateTime.Now.ToString("dddd, dd MMMM yyyy, hh:mm tt");
            InitializeComponent();
            LockdownManager.ApplyLockdown(this);
        }

        private void InitializeComponent()
        {
            Text = "My exam – c1.exam.et";
            Size = new Size(1280, 800);
            MinimumSize = new Size(1000, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(0xF0, 0xF0, 0xF0);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = true;
            Icon = new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "icons", "Application.ico"));
            WindowState = FormWindowState.Maximized;
            Font = new Font("Segoe UI", 9f);
            Padding = Padding.Empty;

            BuildNavbar();
            BuildWhiteArea();
            BuildSidebar();
        }

        // ── NAVBAR ───────────────────────────────────────────────────────
        private void BuildNavbar()
        {
            _navbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = NavbarHeight,
                BackColor = Color.White,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
            };
            AddBottomShadow(_navbar);

            // Logo
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                           "Assets", "images", "pagelogo.png");
            if (File.Exists(logoPath))
            {
                var logo = new PictureBox
                {
                    Image = Image.FromFile(logoPath),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(44, 38),
                    Location = new Point(16, 9),
                };
                _navbar.Controls.Add(logo);
            }

            // Tabs
            _navbar.Controls.Add(NavTab("Home", 68, false));
            _navbar.Controls.Add(NavTab("My exam", 148, true));

            // Avatar + arrow (anchored top-right)
            string initials = GetInitials(_studentName);

            var avatarCircle = new Panel
            {
                Size = new Size(36, 36),
                BackColor = Color.Transparent,
            };
            avatarCircle.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillEllipse(new SolidBrush(Color.FromArgb(0xE0, 0xE0, 0xE0)), 0, 0, 35, 35);
                using var f = new Font("Segoe UI", 10f, FontStyle.Bold);
                var sz = g.MeasureString(initials, f);
                g.DrawString(initials, f, Brushes.Black,
                    (36 - sz.Width) / 2f, (36 - sz.Height) / 2f);
            };

            var arrow = new Label
            {
                Text = "▾",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(0x69, 0x69, 0x69),
                AutoSize = true,
            };

            // Use a TableLayoutPanel so the avatar+arrow stay right-anchored cleanly
            var avatarRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            avatarRow.Controls.Add(avatarCircle);
            avatarRow.Controls.Add(arrow);

            // Position after navbar is added (so Width is known)
            _navbar.Controls.Add(avatarRow);
            _navbar.Layout += (s, e) =>
            {
                avatarRow.Location = new Point(
                    _navbar.ClientSize.Width - avatarRow.Width - 16,
                    (_navbar.Height - 36) / 2);
            };

            Controls.Add(_navbar);
        }

        private Label NavTab(string text, int x, bool active)
        {
            var lbl = new Label
            {
                Text = text,
                AutoSize = false,
                Size = new Size(80, NavbarHeight),
                Location = new Point(x, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10f, active ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = active ? Color.FromArgb(0x37, 0x4F, 0xBF) : Color.FromArgb(0x69, 0x69, 0x69),
                Cursor = Cursors.Hand,
            };
            if (active)
            {
                lbl.Paint += (s, e) =>
                    e.Graphics.FillRectangle(
                        new SolidBrush(Color.FromArgb(0x37, 0x4F, 0xBF)),
                        0, NavbarHeight - 3, lbl.Width, 3);
            }
            return lbl;
        }

        // ── WHITE CONTENT AREA ───────────────────────────────────────────
        private void BuildWhiteArea()
        {
            // Sidebar total column width = card + right margin + left gap
            int sideColW = SidebarCardW + SidebarMarginR + 16;

            _whiteArea = new Panel
            {
                BackColor = Color.White,
                Location = new Point(0, NavbarHeight),
            };

            void SizeWhite() =>
                _whiteArea.Size = new Size(
                    ClientSize.Width - sideColW,
                    ClientSize.Height - NavbarHeight);

            SizeWhite();
            Resize += (_, _) => SizeWhite();

            // ── Heading ──────────────────────────────────────────────────
            var heading = new Label
            {
                Text = "My exam",
                Font = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = true,
                Location = new Point(48, 56),
            };
            _whiteArea.Controls.Add(heading);

            // ── Subtitle ─────────────────────────────────────────────────
            var subtitle = new Label
            {
                Text = "Exam Overview",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0x69, 0x69, 0x69),
                AutoSize = true,
                Location = new Point(48, 96),
            };
            _whiteArea.Controls.Add(subtitle);

            // ── Divider ───────────────────────────────────────────────────
            var divider = new Panel
            {
                Location = new Point(48, 126),
                Size = new Size(620, 1),
                BackColor = Color.FromArgb(0xCC, 0xCC, 0xCC),
            };
            _whiteArea.Controls.Add(divider);

            // ── Search ────────────────────────────────────────────────────
            var searchWrapper = RoundedInputWrapper(48, 142, 200, 34);
            _searchBox = new TextBox
            {
                PlaceholderText = "Search",
                Location = new Point(8, 6),
                Size = new Size(184, 22),
                Font = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
            };
            searchWrapper.Controls.Add(_searchBox);
            _whiteArea.Controls.Add(searchWrapper);

            // ── Sort dropdown ─────────────────────────────────────────────
            var sortWrapper = RoundedInputWrapper(260, 142, 200, 34);
            _sortDropdown = new ComboBox
            {
                Location = new Point(6, 5),
                Size = new Size(186, 22),
                Font = new Font("Segoe UI", 10f),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
            };
            _sortDropdown.Items.AddRange(new object[]
                { "Sort by course name", "Sort by date", "Sort by subject" });
            _sortDropdown.SelectedIndex = 0;
            sortWrapper.Controls.Add(_sortDropdown);
            _whiteArea.Controls.Add(sortWrapper);

            // ── Cards ─────────────────────────────────────────────────────
            _whiteArea.Controls.Add(BuildCard("Natural Science", "yellow", 48, 192));
            _whiteArea.Controls.Add(BuildCard("Social Science", "pink", 328, 192));

            Controls.Add(_whiteArea);
        }

        private static Panel RoundedInputWrapper(int x, int y, int w, int h)
        {
            var p = new Panel { Location = new Point(x, y), Size = new Size(w, h), BackColor = Color.White };
            p.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(new Rectangle(0, 0, p.Width - 1, p.Height - 1), 6);
                using var pen = new Pen(Color.FromArgb(0xCC, 0xCC, 0xCC), 1);
                g.DrawPath(pen, path);
            };
            return p;
        }

        // ── SIDEBAR ──────────────────────────────────────────────────────
        private void BuildSidebar()
        {
            var card = new SidebarCardPanel(8)
            {
                Size = new Size(SidebarCardW, SidebarCardH),
                BackColor = Color.White,
            };

            void PositionCard() =>
                card.Location = new Point(
                    ClientSize.Width - SidebarCardW - SidebarMarginR,
                    NavbarHeight + SidebarMarginT);

            PositionCard();
            Resize += (_, _) => PositionCard();

            // "Exam Overview" title
            card.Controls.Add(new Label
            {
                Text = "Exam Overview",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = true,
                Location = new Point(16, 14),
            });

            // Avatar
            string initials = GetInitials(_studentName);
            var avatar = new Panel
            {
                Size = new Size(36, 36),
                Location = new Point(16, 50),
                BackColor = Color.Transparent,
            };
            avatar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillEllipse(new SolidBrush(Color.FromArgb(0xE0, 0xE0, 0xE0)), 0, 0, 35, 35);
                using var f = new Font("Segoe UI", 10f, FontStyle.Bold);
                var sz = g.MeasureString(initials, f);
                g.DrawString(initials, f, Brushes.Black,
                    (36 - sz.Width) / 2f, (36 - sz.Height) / 2f);
            };
            card.Controls.Add(avatar);

            // Name
            card.Controls.Add(new Label
            {
                Text = _studentName,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = true,
                Location = new Point(60, 58),
            });

            // Info rows
            int y = 104;
            AddInfoRow(card, "Country:", "Ethiopia", ref y);
            AddInfoRow(card, "Institution:", "Addis Ababa", ref y);
            AddInfoRow(card, "Log in:", _loginDate, ref y);

            Controls.Add(card);
        }

        private void AddInfoRow(Panel parent, string key, string value, ref int y)
        {
            var boldFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            var regFont = new Font("Segoe UI", 9f);

            int keyW;
            using (var g = parent.CreateGraphics())
                keyW = (int)Math.Ceiling(g.MeasureString(key, boldFont).Width);

            parent.Controls.Add(new Label
            {
                Text = key,
                Font = boldFont,
                ForeColor = Color.Black,
                AutoSize = true,
                Location = new Point(16, y),
            });
            parent.Controls.Add(new Label
            {
                Text = value,
                Font = regFont,
                ForeColor = Color.FromArgb(0x44, 0x44, 0x44),
                AutoSize = true,
                Location = new Point(16 + keyW, y),
            });
            y += 30;
        }

        // ── SUBJECT CARD ─────────────────────────────────────────────────
        private Panel BuildCard(string subject, string imageKey, int x, int y)
        {
            var card = new Panel
            {
                Size = new Size(CardWidth, CardHeight),
                Location = new Point(x, y),
                BackColor = Color.White,
                Cursor = Cursors.Hand,
            };
            AddCardBorder(card);

            var img = new PictureBox
            {
                Size = new Size(CardWidth, CardImageHeight),
                Location = Point.Empty,
                SizeMode = PictureBoxSizeMode.Zoom,
            };

            string[] paths = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "images", imageKey + ".png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "images", imageKey + ".jpg"),
                $@"C:\Project\money\Assets\images\{imageKey}.png",
                $@"C:\Project\money\Assets\images\{imageKey}.jpg",
            };
            bool loaded = false;
            foreach (var p in paths)
                if (File.Exists(p)) { img.Image = Image.FromFile(p); loaded = true; break; }
            if (!loaded)
                img.BackColor = imageKey == "yellow"
                    ? Color.FromArgb(0xF5, 0xC5, 0x18)
                    : Color.FromArgb(0xF0, 0x6E, 0x9B);

            card.Controls.Add(img);

            var lbl = new Label
            {
                Text = subject,
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(0x37, 0x4F, 0xBF),
                AutoSize = true,
                Location = new Point(12, CardImageHeight + 10),
            };
            card.Controls.Add(lbl);

            var dots = new Label
            {
                Text = "⋮",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0x69, 0x69, 0x69),
                AutoSize = true,
                Location = new Point(CardWidth - 28, CardImageHeight + 6),
                Cursor = Cursors.Hand,
            };
            dots.Click += (_, _) => ShowCardMenu(dots, subject);
            card.Controls.Add(dots);

            card.Click += (_, _) => LaunchExam(subject);
            img.Click += (_, _) => LaunchExam(subject);
            lbl.Click += (_, _) => LaunchExam(subject);

            return card;
        }

        private void ShowCardMenu(Control anchor, string subject)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Start exam", null, (_, _) => LaunchExam(subject));
            menu.Items.Add("View details", null, (_, _) =>
                MessageBox.Show($"Subject: {subject}", "Details",
                    MessageBoxButtons.OK, MessageBoxIcon.Information));
            menu.Show(anchor, new Point(0, anchor.Height));
        }

        private void LaunchExam(string subject)
        {
            MessageBox.Show(
                $"No exam has been added for {subject} yet.",
                "No Exam Available",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // ── HELPERS ──────────────────────────────────────────────────────
        private static string GetInitials(string name)
        {
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
                : name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
        }

        private static void AddBottomShadow(Control c) =>
            c.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(40, 0, 0, 0), 1);
                e.Graphics.DrawLine(pen, 0, c.Height - 1, c.Width, c.Height - 1);
            };

        private static void AddCardBorder(Panel card) =>
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(0xCC, 0xCC, 0xCC), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class SidebarCardPanel : Panel
    {
        private readonly int _radius;
        public SidebarCardPanel(int radius) { _radius = radius; DoubleBuffered = true; }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float r2 = _radius * 2;
            using var path = new GraphicsPath();
            path.AddArc(0, 0, r2, r2, 180, 90);
            path.AddArc(Width - r2, 0, r2, r2, 270, 90);
            path.AddArc(Width - r2, Height - r2, r2, r2, 0, 90);
            path.AddArc(0, Height - r2, r2, r2, 90, 90);
            path.CloseFigure();
            g.FillPath(new SolidBrush(Color.White), path);
            g.DrawPath(new Pen(Color.FromArgb(0xCC, 0xCC, 0xCC), 1), path);
        }
    }
}