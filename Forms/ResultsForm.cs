using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SEBClone.Models;
using Utilities;

namespace SEBClone.Forms
{
    /// <summary>
    /// Displays the student's exam results: score card, pass/fail verdict,
    /// and a scrollable per-question review panel.
    /// </summary>
    internal sealed class ResultsForm : Form
    {
        // ── Palette ───────────────────────────────────────────────────────────
        private static readonly Color BgColor      = Color.FromArgb(0xF0, 0xF0, 0xF0);
        private static readonly Color HeaderBlue   = Color.FromArgb(55, 79, 191);
        private static readonly Color PassGreen    = Color.FromArgb(76, 175, 80);
        private static readonly Color FailRed      = Color.FromArgb(211, 47, 47);

        // ── Data ──────────────────────────────────────────────────────────────
        private readonly int                  _score;
        private readonly int                  _total;
        private readonly List<QuestionResult> _results;
        private readonly string               _studentName;

        // ── Constructor ───────────────────────────────────────────────────────
        public ResultsForm(int score, int total, List<QuestionResult> results, string studentName)
        {
            _score       = score;
            _total       = total;
            _results     = results;
            _studentName = studentName;

            InitializeComponent();

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
            Text           = "Safe Exam Browser – Results";
            BackColor      = BgColor;
            DoubleBuffered = true;

            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                          "Assets", "icons", "SafeExamBrowser.ico");
            if (File.Exists(icoPath))
                Icon = new Icon(icoPath);

            // ── Header Panel ──────────────────────────────────────────────────
            var headerPanel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 60,
                BackColor = HeaderBlue,
            };

            // SEB icon (left)
            var headerIcon = new PictureBox
            {
                Size      = new Size(40, 40),
                SizeMode  = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Location  = new Point(10, 10),
            };
            if (File.Exists(icoPath))
                headerIcon.Image = new Icon(icoPath, 40, 40).ToBitmap();
            headerPanel.Controls.Add(headerIcon);

            // "Exam Results" title (center)
            var titleLabel = new Label
            {
                Text      = "Exam Results",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false,
                Dock      = DockStyle.Fill,
            };
            headerPanel.Controls.Add(titleLabel);
            titleLabel.SendToBack();

            // Student name (right)
            var studentLabel = new Label
            {
                Text      = _studentName,
                Font      = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize  = true,
            };
            headerPanel.Controls.Add(studentLabel);

            // Position the student name label dynamically
            headerPanel.Resize += (s, e) =>
            {
                studentLabel.Location = new Point(
                    headerPanel.Width - studentLabel.Width - 16,
                    (60 - studentLabel.Height) / 2);
            };

            Controls.Add(headerPanel);

            // ── Bottom Close Button ───────────────────────────────────────────
            var bottomPanel = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 60,
                BackColor = BgColor,
            };

            var closeButton = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = Color.White,
                BackColor = FailRed,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(120, 38),
                Cursor    = Cursors.Hand,
                UseVisualStyleBackColor = false,
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 40, 40);
            closeButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(160, 20, 20);
            closeButton.Click += (_, _) =>
            {
                using var dialog = new ExitDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                    Application.Exit();
            };

            bottomPanel.Resize += (s, e) =>
            {
                closeButton.Location = new Point(
                    (bottomPanel.Width - closeButton.Width) / 2,
                    (bottomPanel.Height - closeButton.Height) / 2);
            };
            bottomPanel.Controls.Add(closeButton);
            Controls.Add(bottomPanel);

            // ── Scroll Container (fills remaining area) ───────────────────────
            var scrollPanel = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = BgColor,
            };
            Controls.Add(scrollPanel);

            // ── Score Card (centered, 400×160, white rounded panel) ───────────
            var scoreCard = new RoundedPanel(16)
            {
                Size      = new Size(400, 160),
                BackColor = Color.White,
            };

            // Centre the card when the scroll panel resizes
            scrollPanel.Resize += (s, e) =>
            {
                scoreCard.Location = new Point(
                    (scrollPanel.Width - scoreCard.Width) / 2,
                    20);
            };

            double pct         = _total > 0 ? (double)_score / _total * 100 : 0;
            bool   passed      = pct >= 50;
            string pctString   = $"{pct:0}%";
            string verdictText = passed ? "PASSED" : "FAILED";
            Color  verdictClr  = passed ? PassGreen : FailRed;

            // Large score label  e.g. "8 / 10"
            var scoreLabel = new Label
            {
                Text      = $"{_score} / {_total}",
                Font      = new Font("Segoe UI", 36f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = HeaderBlue,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false,
                Size      = new Size(400, 60),
                Location  = new Point(0, 20),
            };
            scoreCard.Controls.Add(scoreLabel);

            // Percentage label  e.g. "80%"
            var percentLabel = new Label
            {
                Text      = pctString,
                Font      = new Font("Segoe UI", 18f, FontStyle.Regular, GraphicsUnit.Pixel),
                ForeColor = Color.DimGray,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false,
                Size      = new Size(400, 34),
                Location  = new Point(0, 82),
            };
            scoreCard.Controls.Add(percentLabel);

            // Verdict label  e.g. "PASSED"
            var verdictLabel = new Label
            {
                Text      = verdictText,
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = verdictClr,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false,
                Size      = new Size(400, 28),
                Location  = new Point(0, 118),
            };
            scoreCard.Controls.Add(verdictLabel);

            scrollPanel.Controls.Add(scoreCard);

            // ── Review Header ─────────────────────────────────────────────────
            var reviewHeader = new Label
            {
                Text      = "Question Review",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = Color.Black,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(20, 200),
            };
            scrollPanel.Controls.Add(reviewHeader);

            // ── Review Rows ───────────────────────────────────────────────────
            int yOffset = 230;
            foreach (var result in _results)
            {
                var row = BuildReviewRow(result, scrollPanel.Width);
                row.Location = new Point(0, yOffset);
                scrollPanel.Controls.Add(row);
                yOffset += row.Height + 4;
            }

            // Reposition review rows + review header when the scroll panel resizes
            scrollPanel.Resize += (s, e) =>
            {
                scoreCard.Location    = new Point((scrollPanel.Width - scoreCard.Width) / 2, 20);
                reviewHeader.Location = new Point(20, 200);

                int y = 230;
                // Skip scoreCard and reviewHeader (first two in this loop)
                foreach (Control ctrl in scrollPanel.Controls)
                {
                    if (ctrl is Panel rowPanel && ctrl != scoreCard)
                    {
                        ctrl.Width    = scrollPanel.Width;
                        ctrl.Location = new Point(0, y);
                        y += ctrl.Height + 4;
                    }
                }
            };

            ResumeLayout(false);
        }

        // ── Build a single review row ─────────────────────────────────────────
        private static Panel BuildReviewRow(QuestionResult result, int width)
        {
            const int rowHeight = 52;

            var row = new Panel
            {
                Size      = new Size(Math.Max(width, 400), rowHeight),
                BackColor = result.IsCorrect
                                ? Color.FromArgb(232, 245, 233)   // light green tint
                                : Color.FromArgb(255, 235, 238),  // light red tint
            };

            // Verdict icon (✓ / ✗)
            var iconLabel = new Label
            {
                Text      = result.IsCorrect ? "✓" : "✗",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = result.IsCorrect
                                ? Color.FromArgb(76, 175, 80)
                                : Color.FromArgb(211, 47, 47),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Size      = new Size(36, rowHeight),
                Location  = new Point(0, 0),
            };
            row.Controls.Add(iconLabel);

            // Question number + truncated text
            string shortText = result.QuestionText.Length > 60
                ? result.QuestionText[..60] + "…"
                : result.QuestionText;

            var qLabel = new Label
            {
                Text      = $"Q{result.QuestionNumber}. {shortText}",
                Font      = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel),
                ForeColor = Color.Black,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false,
                Size      = new Size(Math.Max(width - 300, 200), rowHeight),
                Location  = new Point(40, 0),
            };
            row.Controls.Add(qLabel);

            // Answer details (right side)
            var answerPanel = new Panel
            {
                BackColor = Color.Transparent,
                Size      = new Size(240, rowHeight),
            };
            // Anchor to the right of the row
            answerPanel.Location = new Point(row.Width - 244, 0);
            row.Resize += (s, e) =>
            {
                answerPanel.Location = new Point(row.Width - 244, 0);
                qLabel.Width = Math.Max(row.Width - 300, 200);
            };

            var yourAnswerLabel = new Label
            {
                Text      = $"Your answer:    {result.SelectedAnswer}",
                Font      = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel),
                ForeColor = result.IsCorrect
                                ? Color.FromArgb(30, 130, 50)
                                : Color.FromArgb(180, 30, 30),
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(0, 8),
            };
            answerPanel.Controls.Add(yourAnswerLabel);

            var correctAnswerLabel = new Label
            {
                Text      = $"Correct answer: {result.CorrectAnswer}",
                Font      = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel),
                ForeColor = Color.FromArgb(30, 130, 50),
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(0, 28),
            };
            answerPanel.Controls.Add(correctAnswerLabel);

            row.Controls.Add(answerPanel);

            // Bottom separator line
            row.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(200, 200, 200), 1);
                e.Graphics.DrawLine(pen, 0, rowHeight - 1, row.Width, rowHeight - 1);
            };

            return row;
        }
    }
}
