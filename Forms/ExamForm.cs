using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using SEBClone.Models;
using Utilities;
using Timer = System.Windows.Forms.Timer;

namespace SEBClone.Forms
{
    /// <summary>
    /// The main exam screen shell with SEB-style bottom taskbar and real questions panel.
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
        private Timer      _clockTimer    = null!;

        // Left Panel (Question Display)
        private Panel      _leftPanel = null!;
        private Panel      _leftTopBar = null!;
        private Label      _questionCounterLabel = null!;
        private Button     _flagButton = null!;
        private Panel      _questionBodyPanel = null!;
        private Label      _questionLabel = null!;
        private Panel      _optionsPanel = null!;
        private RadioButton[] _optionRadios = null!;
        private Panel      _leftNavPanel = null!;
        private Button     _prevButton    = null!;
        private Button     _nextButton    = null!;
        private Button     _submitButton  = null!;

        // Right Panel (Question Navigator)
        private Panel      _rightPanel = null!;
        private Label      _navigatorHeaderLabel = null!;
        private Panel      _navigatorGridPanel = null!;
        private Button[]   _navButtons = null!;
        private Panel      _legendPanel = null!;

        // ── State ─────────────────────────────────────────────────────────────
        private readonly string _studentName;
        private ExamData _examData = null!;
        private string?[] _answers = null!;
        private bool[] _flagged = null!;
        private int _currentQuestionIndex = 0;

        // ── Constructor ───────────────────────────────────────────────────────
        public ExamForm(string studentName)
        {
            _studentName = studentName;

            // Load mock exam questions data first
            LoadExamData();

            InitializeComponent();

            // Display initial question
            DisplayQuestion(0);

            // Apply kiosk lockdown AFTER InitializeComponent so the handle
            // has not been created yet (avoids recreating the native window).
            LockdownManager.ApplyLockdown(this);
        }

        // ── Kiosk: strip min/max buttons from the native window style ─────────
        protected override CreateParams CreateParams =>
            LockdownManager.GetLockedCreateParams(base.CreateParams);

        // ── Load Exam Data ────────────────────────────────────────────────────
        [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(_examData), nameof(_answers), nameof(_flagged))]
        private void LoadExamData()
        {
            string questionsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "questions.json");
            if (!File.Exists(questionsPath))
            {
                MessageBox.Show($"Configuration error: questions.json not found.\n{questionsPath}", "Safe Exam Browser", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _examData = new ExamData();
                _answers = Array.Empty<string?>();
                _flagged = Array.Empty<bool>();
                Application.Exit();
                return;
            }

            try
            {
                string json = File.ReadAllText(questionsPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                _examData = JsonSerializer.Deserialize<ExamData>(json, options) ?? new ExamData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load exam questions.\n{ex.Message}", "Safe Exam Browser", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _examData = new ExamData();
                _answers = Array.Empty<string?>();
                _flagged = Array.Empty<bool>();
                Application.Exit();
                return;
            }

            if (_examData.Questions == null)
            {
                _examData.Questions = new List<Question>();
            }

            int count = _examData.Questions.Count;
            _answers = new string?[count];
            _flagged = new bool[count];
        }

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
            string examTitle = _examData?.ExamTitle ?? "Grade 12 ESSLCE Mock Exam";
            _nameLabel = new Label
            {
                Text      = $"{examTitle}  |  Student: {_studentName}",
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

            // ── Left Column (Question Display) ────────────────────────────────
            _leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                BackColor = Color.White,
            };
            _contentPanel.Controls.Add(_leftPanel);

            // ── Right Column (Question Navigator) ─────────────────────────────
            _rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgColor,
            };
            _rightPanel.Paint += OnRightPanelPaint;
            _contentPanel.Controls.Add(_rightPanel);

            // Handle splitter resize proportion dynamically
            _contentPanel.Resize += (s, e) =>
            {
                _leftPanel.Width = (int)(_contentPanel.Width * 0.75);
            };

            // ── Left Panel Components ─────────────────────────────────────────
            _leftTopBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.Transparent,
            };
            _leftPanel.Controls.Add(_leftTopBar);

            _flagButton = new Button
            {
                Text = "⚑ Flag",
                Font = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0xFF, 0x8C, 0x00),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 28),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
            };
            _flagButton.FlatAppearance.BorderSize = 0;
            _flagButton.Click += OnFlagButtonClick;
            _leftTopBar.Controls.Add(_flagButton);

            _questionCounterLabel = new Label
            {
                Font = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel),
                ForeColor = SecondaryText,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = true,
            };
            _leftTopBar.Controls.Add(_questionCounterLabel);

            _leftTopBar.Resize += (s, e) => LayoutLeftTopBar();

            _leftNavPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.Transparent,
            };
            _leftPanel.Controls.Add(_leftNavPanel);

            _prevButton = new Button
            {
                Text = "← Previous",
                Font = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(55, 79, 191),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 34),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
            };
            _prevButton.FlatAppearance.BorderSize = 0;
            _prevButton.Click += (s, e) => GoToQuestion(_currentQuestionIndex - 1);
            _leftNavPanel.Controls.Add(_prevButton);

            _nextButton = new Button
            {
                Text = "Next →",
                Font = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(55, 79, 191),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 34),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
            };
            _nextButton.FlatAppearance.BorderSize = 0;
            _nextButton.Click += (s, e) => GoToQuestion(_currentQuestionIndex + 1);
            _leftNavPanel.Controls.Add(_nextButton);

            _submitButton = new Button
            {
                Text      = "Submit Exam",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0xD3, 0x2F, 0x2F),
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(120, 34),
                Cursor    = Cursors.Hand,
                UseVisualStyleBackColor = false,
            };
            _submitButton.FlatAppearance.BorderSize = 0;
            _submitButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(0xB7, 0x1C, 0x1C);
            _submitButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(0x8B, 0x00, 0x00);
            _submitButton.Click += OnSubmitClick;
            _leftNavPanel.Controls.Add(_submitButton);

            _leftNavPanel.Resize += (s, e) => LayoutLeftNavPanel();

            _questionBodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true,
            };
            _leftPanel.Controls.Add(_questionBodyPanel);

            _questionLabel = new Label
            {
                Font = new Font("Segoe UI", 14f, FontStyle.Regular, GraphicsUnit.Pixel),
                ForeColor = Color.Black,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(20, 20),
            };
            _questionBodyPanel.Controls.Add(_questionLabel);

            _optionsPanel = new Panel
            {
                BackColor = Color.Transparent,
                Height = 160,
            };
            _questionBodyPanel.Controls.Add(_optionsPanel);

            _optionRadios = new RadioButton[4];
            for (int i = 0; i < 4; i++)
            {
                _optionRadios[i] = new RadioButton
                {
                    Font = new Font("Segoe UI", 12f, FontStyle.Regular, GraphicsUnit.Pixel),
                    ForeColor = Color.Black,
                    Cursor = Cursors.Hand,
                    Location = new Point(30, 40 * i),
                    Height = 30,
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleLeft,
                    CheckAlign = ContentAlignment.MiddleLeft,
                };
                _optionRadios[i].CheckedChanged += OnOptionCheckedChanged;
                _optionsPanel.Controls.Add(_optionRadios[i]);
            }

            _questionLabel.SizeChanged += (s, e) => LayoutOptionsPanel();
            _questionBodyPanel.Resize += (s, e) =>
            {
                int maxW = _questionBodyPanel.Width - 40;
                if (maxW > 0)
                {
                    _questionLabel.MaximumSize = new Size(maxW, 0);
                    LayoutOptionsPanel();
                }
            };
            _optionsPanel.Resize += (s, e) =>
            {
                int rW = _optionsPanel.Width - 60;
                if (rW > 0)
                {
                    for (int i = 0; i < 4; i++)
                        _optionRadios[i].Width = rW;
                }
            };

            // Order layers
            _leftTopBar.SendToBack();
            _leftNavPanel.SendToBack();
            _questionBodyPanel.BringToFront();

            // ── Right Panel Components ────────────────────────────────────────
            _navigatorHeaderLabel = new Label
            {
                Text = "Questions",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = Color.Black,
                Location = new Point(20, 20),
                AutoSize = true,
            };
            _rightPanel.Controls.Add(_navigatorHeaderLabel);

            _navigatorGridPanel = new Panel
            {
                Location = new Point(20, 45),
                BackColor = Color.Transparent,
            };
            _rightPanel.Controls.Add(_navigatorGridPanel);

            // Populate navigator grid
            int totalQuestions = _examData!.Questions!.Count;
            _navButtons = new Button[totalQuestions];
            for (int i = 0; i < totalQuestions; i++)
            {
                int row = i / 4;
                int col = i % 4;
                var btn = new Button
                {
                    Text = (i + 1).ToString(),
                    Size = new Size(36, 36),
                    Location = new Point(col * (36 + 8), row * (36 + 8)),
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold, GraphicsUnit.Pixel),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Tag = i,
                    UseVisualStyleBackColor = false,
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += OnNavButtonClick;
                _navigatorGridPanel.Controls.Add(btn);
                _navButtons[i] = btn;
            }

            int numRows = (totalQuestions + 3) / 4;
            _navigatorGridPanel.Size = new Size(4 * 36 + 3 * 8, numRows * 36 + (numRows - 1) * 8);

            // Legend Panel
            _legendPanel = new Panel
            {
                BackColor = Color.Transparent,
                Width = 200,
                Height = 80,
                Location = new Point(20, _navigatorGridPanel.Bottom + 20),
            };
            _rightPanel.Controls.Add(_legendPanel);

            var legendColors = new Color[]
            {
                Color.FromArgb(0xCC, 0xCC, 0xCC), // Grey
                Color.FromArgb(0x4C, 0xAF, 0x50), // Green
                Color.FromArgb(0xFF, 0x8C, 0x00)  // Orange
            };
            var legendTexts = new string[]
            {
                "Unanswered",
                "Answered",
                "Flagged"
            };

            for (int i = 0; i < 3; i++)
            {
                var colorBlock = new Panel
                {
                    Size = new Size(12, 12),
                    Location = new Point(0, 20 * i + 4),
                    BackColor = legendColors[i],
                };
                _legendPanel.Controls.Add(colorBlock);

                var legendLabel = new Label
                {
                    Text = legendTexts[i],
                    Font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Pixel),
                    ForeColor = SecondaryText,
                    Location = new Point(18, 20 * i + 3),
                    AutoSize = true,
                };
                _legendPanel.Controls.Add(legendLabel);
            }

            // ── Add to Form Controls ──────────────────────────────────────────
            Controls.Add(_contentPanel);
            Controls.Add(_taskbarPanel);

            // Perform initial layout calculations
            LayoutTaskbarControls();
            LayoutLeftTopBar();
            LayoutLeftNavPanel();

            ResumeLayout(false);
        }

        // ── Layout calculations ───────────────────────────────────────────────
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

        private void LayoutLeftTopBar()
        {
            if (_leftTopBar == null || _flagButton == null || _questionCounterLabel == null)
                return;

            int w = _leftTopBar.Width;
            _flagButton.Location = new Point(w - 80 - 20, 8);
            _questionCounterLabel.Location = new Point(_flagButton.Left - _questionCounterLabel.Width - 10, (44 - _questionCounterLabel.Height) / 2);
        }

        private void LayoutLeftNavPanel()
        {
            if (_leftNavPanel == null || _prevButton == null || _nextButton == null || _submitButton == null)
                return;

            int w = _leftNavPanel.Width;
            int h = _leftNavPanel.Height;
            _prevButton.Location   = new Point(20, (h - 34) / 2);
            _nextButton.Location   = new Point(w - 120 - 20 - 120 - 12, (h - 34) / 2);
            _submitButton.Location = new Point(w - 120 - 20, (h - 34) / 2);
        }

        private void LayoutOptionsPanel()
        {
            if (_optionsPanel == null || _questionLabel == null || _questionBodyPanel == null)
                return;

            int top = _questionLabel.Bottom + 20;
            _optionsPanel.Location = new Point(0, top);
            _optionsPanel.Width = _questionBodyPanel.Width;
        }

        // ── Navigation & State Helpers ────────────────────────────────────────
        private void GoToQuestion(int newIndex)
        {
            if (_examData == null || newIndex < 0 || newIndex >= _examData.Questions.Count)
                return;

            SaveCurrentQuestionState();
            _currentQuestionIndex = newIndex;
            DisplayQuestion(_currentQuestionIndex);
        }

        private void DisplayQuestion(int index)
        {
            if (_examData == null || index < 0 || index >= _examData.Questions.Count)
                return;

            var question = _examData.Questions[index];

            // 1. Update Counter
            _questionCounterLabel.Text = $"Question {index + 1} of {_examData.Questions.Count}";
            LayoutLeftTopBar();

            // 2. Update Flag Button State
            UpdateFlagButtonState();

            // 3. Update Question Text
            _questionLabel.Text = question.Text;

            // 4. Update Options
            for (int i = 0; i < 4; i++)
                _optionRadios[i].CheckedChanged -= OnOptionCheckedChanged;

            for (int i = 0; i < 4; i++)
            {
                if (i < question.Options.Count)
                {
                    _optionRadios[i].Text = question.Options[i];
                    _optionRadios[i].Visible = true;
                }
                else
                {
                    _optionRadios[i].Visible = false;
                }
            }

            string? savedAnswer = _answers[index];
            _optionRadios[0].Checked = (savedAnswer == "A");
            _optionRadios[1].Checked = (savedAnswer == "B");
            _optionRadios[2].Checked = (savedAnswer == "C");
            _optionRadios[3].Checked = (savedAnswer == "D");

            for (int i = 0; i < 4; i++)
                _optionRadios[i].CheckedChanged += OnOptionCheckedChanged;

            // 5. Navigation buttons state
            _prevButton.Enabled = (index > 0);
            _nextButton.Enabled = (index < _examData.Questions.Count - 1);

            _prevButton.BackColor = _prevButton.Enabled ? Color.FromArgb(55, 79, 191) : Color.FromArgb(170, 180, 220);
            _nextButton.BackColor = _nextButton.Enabled ? Color.FromArgb(55, 79, 191) : Color.FromArgb(170, 180, 220);

            // 6. Refresh Navigator Colors
            UpdateNavigatorColors();

            // Force recalculation of options panel layout
            LayoutOptionsPanel();
        }

        private void SaveCurrentQuestionState()
        {
            if (_examData == null || _currentQuestionIndex < 0 || _currentQuestionIndex >= _examData.Questions.Count)
                return;

            string? selectedAnswer = null;
            if (_optionRadios[0].Checked) selectedAnswer = "A";
            else if (_optionRadios[1].Checked) selectedAnswer = "B";
            else if (_optionRadios[2].Checked) selectedAnswer = "C";
            else if (_optionRadios[3].Checked) selectedAnswer = "D";

            _answers[_currentQuestionIndex] = selectedAnswer;
        }

        private void UpdateFlagButtonState()
        {
            if (_flagged[_currentQuestionIndex])
            {
                _flagButton.Text = "⚑ Flagged";
                _flagButton.BackColor = Color.FromArgb(0xE0, 0x7B, 0x00);
            }
            else
            {
                _flagButton.Text = "⚑ Flag";
                _flagButton.BackColor = Color.FromArgb(0xFF, 0x8C, 0x00);
            }
        }

        private void UpdateNavigatorColors()
        {
            if (_navButtons == null || _answers == null || _flagged == null)
                return;

            for (int i = 0; i < _examData.Questions.Count; i++)
            {
                var btn = _navButtons[i];
                if (_flagged[i])
                {
                    btn.BackColor = Color.FromArgb(0xFF, 0x8C, 0x00);
                    btn.ForeColor = Color.White;
                }
                else if (!string.IsNullOrEmpty(_answers[i]))
                {
                    btn.BackColor = Color.FromArgb(0x4C, 0xAF, 0x50);
                    btn.ForeColor = Color.White;
                }
                else
                {
                    btn.BackColor = Color.FromArgb(0xCC, 0xCC, 0xCC);
                    btn.ForeColor = Color.Black;
                }

                // Highlight current question
                if (i == _currentQuestionIndex)
                {
                    btn.FlatAppearance.BorderSize = 2;
                    btn.FlatAppearance.BorderColor = Color.Black;
                }
                else
                {
                    btn.FlatAppearance.BorderSize = 0;
                }
            }
        }

        // ── Event Handlers ────────────────────────────────────────────────────
        private void OnClockTick(object? sender, EventArgs e)
        {
            _clockLabel.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void OnTaskbarPaint(object? sender, PaintEventArgs e)
        {
            using var pen = new Pen(Color.LightGray, 1);
            e.Graphics.DrawLine(pen, 0, 0, _taskbarPanel.Width, 0);
        }

        private void OnRightPanelPaint(object? sender, PaintEventArgs e)
        {
            using var pen = new Pen(Color.LightGray, 1);
            e.Graphics.DrawLine(pen, 0, 0, 0, _rightPanel.Height);
        }

        private void OnFlagButtonClick(object? sender, EventArgs e)
        {
            _flagged[_currentQuestionIndex] = !_flagged[_currentQuestionIndex];
            UpdateFlagButtonState();
            UpdateNavigatorColors();
        }

        private void OnOptionCheckedChanged(object? sender, EventArgs e)
        {
            if (sender is RadioButton rb && rb.Checked)
            {
                SaveCurrentQuestionState();
                UpdateNavigatorColors();
            }
        }

        private void OnNavButtonClick(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is int index)
            {
                GoToQuestion(index);
            }
        }

        private void OnSubmitClick(object? sender, EventArgs e)
        {
            // Save the currently displayed question's answer first
            SaveCurrentQuestionState();

            // Count unanswered questions
            int unansweredCount = _answers.Count(a => string.IsNullOrEmpty(a));

            if (unansweredCount > 0)
            {
                var choice = MessageBox.Show(
                    $"You have {unansweredCount} unanswered question(s). Are you sure you want to submit?",
                    "Submit Exam",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (choice == DialogResult.No)
                    return;
            }

            // Build results list
            var results = new List<QuestionResult>();
            for (int i = 0; i < _examData.Questions.Count; i++)
            {
                results.Add(new QuestionResult
                {
                    QuestionNumber = i + 1,
                    QuestionText   = _examData.Questions[i].Text,
                    SelectedAnswer = _answers[i] ?? "Not answered",
                    CorrectAnswer  = _examData.Questions[i].Answer,
                    IsCorrect      = _answers[i] == _examData.Questions[i].Answer,
                });
            }

            int score = results.Count(r => r.IsCorrect);

            var resultsForm = new ResultsForm(score, _examData.Questions.Count, results, _studentName);
            resultsForm.Show();
            this.Close();
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
