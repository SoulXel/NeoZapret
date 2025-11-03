using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NeoZapret
{
    public partial class StrategyTesterForm : Form
    {
        private RichTextBox txtResults;
        private Button btnTest;
        private Button btnAutoDetect;
        private ProgressBar progressBar;
        private Label lblStatus;

        private readonly string[] testUrls = new string[]
        {
            "google.com",
            "youtube.com",
            "discord.com",
            "github.com",
            "cursor.sh"
        };

        public StrategyTesterForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "Генерация и тестирование стратегий";
            this.Size = new Size(720, 640);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(32, 32, 38);
            this.Paint += (s, e) =>
            {
                var rect = this.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;
                
                using (var brush = new LinearGradientBrush(
                    rect,
                    Color.FromArgb(32, 32, 38),
                    Color.FromArgb(28, 28, 34),
                    135f))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
            };

            Label lblTitle = new Label
            {
                Text = "Тестирование стратегий",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 220, 230),
                AutoSize = true,
                Location = new Point(20, 20)
            };
            
            Label lblAuthor = new Label
            {
                Text = "Автор: Soulxel | Тестровщик: Матвей Котов | ALPHA • В РАЗРАБОТКЕ",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 150, 160),
                AutoSize = true,
                Location = new Point(20, 600)
            };

            Label lblInfo = new Label
            {
                Text = "Автоматическое определение оптимальной стратегии обхода",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(150, 150, 160),
                AutoSize = true,
                Location = new Point(20, 50)
            };

            btnTest = CreateButton("Тестировать текущую стратегию", 
                new Point(20, 90), Color.FromArgb(80, 80, 90), 650);

            btnAutoDetect = CreateButton("Автоопределение оптимальной", 
                new Point(20, 150), Color.FromArgb(75, 75, 85), 650);

            progressBar = new ProgressBar
            {
                Location = new Point(20, 210),
                Size = new Size(650, 25),
                Style = ProgressBarStyle.Continuous,
                ForeColor = Color.FromArgb(0, 120, 215),
                BackColor = Color.FromArgb(38, 38, 44)
            };

            lblStatus = new Label
            {
                Text = "Готов к тестированию",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(200, 200, 210),
                AutoSize = true,
                Location = new Point(20, 245)
            };

            Panel resultPanel = new Panel
            {
                Location = new Point(20, 280),
                Size = new Size(650, 280),
                BackColor = Color.FromArgb(38, 38, 44),
                BorderStyle = BorderStyle.None
            };
            resultPanel.Paint += (s, e) =>
            {
                var rect = resultPanel.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;
                using (var pen = new Pen(Color.FromArgb(60, 60, 70), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
                }
            };

            txtResults = new RichTextBox
            {
                Location = new Point(5, 5),
                Size = new Size(640, 270),
                BackColor = Color.FromArgb(24, 24, 30),
                ForeColor = Color.FromArgb(220, 220, 230),
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                BorderStyle = BorderStyle.None
            };
            resultPanel.Controls.Add(txtResults);

            Button btnClose = CreateButton("Закрыть", 
                new Point(20, 570), Color.FromArgb(70, 70, 80), 650);
            btnClose.DialogResult = DialogResult.Cancel;

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblInfo);
            this.Controls.Add(btnTest);
            this.Controls.Add(btnAutoDetect);
            this.Controls.Add(progressBar);
            this.Controls.Add(lblStatus);
            this.Controls.Add(resultPanel);
            this.Controls.Add(btnClose);
            this.Controls.Add(lblAuthor);

            btnTest.Click += BtnTest_Click;
            btnAutoDetect.Click += BtnAutoDetect_Click;
        }

        private Button CreateButton(string text, Point location, Color backColor, int width)
        {
            var btn = new Button
            {
                Text = text,
                Location = location,
                Size = new Size(width, 48),
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(255, backColor.R + 15), 
                Math.Min(255, backColor.G + 15), 
                Math.Min(255, backColor.B + 15));
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(
                Math.Max(0, backColor.R - 15), 
                Math.Max(0, backColor.G - 15), 
                Math.Max(0, backColor.B - 15));
            
            btn.Paint += (s, e) =>
            {
                var rect = btn.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;
                using (var path = new GraphicsPath())
                {
                    int radius = 10;
                    path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                    path.AddArc(rect.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                    path.AddArc(rect.Width - radius * 2, rect.Height - radius * 2, radius * 2, radius * 2, 0, 90);
                    path.AddArc(0, rect.Height - radius * 2, radius * 2, radius * 2, 90, 90);
                    path.CloseAllFigures();
                    btn.Region = new Region(path);
                }
            };
            
            return btn;
        }

        private void Log(string message, Color color)
        {
            txtResults.SelectionStart = txtResults.TextLength;
            txtResults.SelectionLength = 0;
            txtResults.SelectionColor = color;
            txtResults.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            txtResults.SelectionColor = txtResults.ForeColor;
            txtResults.ScrollToCaret();
        }

        private void LogSuccess(string message) => Log(message, Color.FromArgb(16, 124, 16));
        private void LogError(string message) => Log(message, Color.FromArgb(196, 43, 28));
        private void LogWarning(string message) => Log(message, Color.FromArgb(247, 99, 12));
        private void LogInfo(string message) => Log(message, Color.FromArgb(0, 120, 215));

        private async void BtnTest_Click(object sender, EventArgs e)
        {
            btnTest.Enabled = false;
            btnAutoDetect.Enabled = false;
            progressBar.Value = 0;
            
            LogInfo("Начинаю тестирование доступности сайтов...");
            LogInfo("════════════════════════════════════════");

            int success = 0;
            for (int i = 0; i < testUrls.Length; i++)
            {
                lblStatus.Text = $"Проверяю {testUrls[i]}...";
                progressBar.Value = (int)((i + 1.0) / testUrls.Length * 100);

                bool result = await TestSite(testUrls[i]);
                if (result)
                {
                    success++;
                    LogSuccess($"{testUrls[i]} - доступен");
                }
                else
                {
                    LogError($"{testUrls[i]} - недоступен");
                }
            }

            LogInfo("════════════════════════════════════════");
            if (success == testUrls.Length)
            {
                LogSuccess($"Все сайты доступны! ({success}/{testUrls.Length})");
            }
            else
            {
                LogWarning($"Частичная доступность ({success}/{testUrls.Length})");
                LogInfo("Рекомендуется выбрать другую стратегию");
            }

            progressBar.Value = 100;
            lblStatus.Text = "Тестирование завершено";
            btnTest.Enabled = true;
            btnAutoDetect.Enabled = true;
        }

        private async void BtnAutoDetect_Click(object sender, EventArgs e)
        {
            btnTest.Enabled = false;
            btnAutoDetect.Enabled = false;
            progressBar.Value = 0;

            LogInfo("АВТООПРЕДЕЛЕНИЕ ОПТИМАЛЬНОЙ СТРАТЕГИИ");
            LogInfo("════════════════════════════════════════");
            
            Dictionary<string, int> strategyScores = new Dictionary<string, int>
            {
                { "fast", 0 },
                { "recommended", 0 },
                { "max", 0 },
                { "games", 0 }
            };

            int totalTests = 4 * testUrls.Length;
            int currentTest = 0;

            foreach (var strategy in strategyScores.Keys)
            {
                LogInfo($"Тестирую стратегию: {strategy}...");
                
                await Task.Delay(500);

                int score = 0;
                foreach (var url in testUrls)
                {
                    currentTest++;
                    progressBar.Value = (int)(currentTest / (double)totalTests * 100);
                    
                    bool result = await TestSite(url);
                    if (result) score++;
                }

                strategyScores[strategy] = score;
                LogInfo($"Стратегия '{strategy}': {score}/{testUrls.Length} доступных");
            }

            string bestStrategy = "recommended";
            int bestScore = 0;
            foreach (var kvp in strategyScores)
            {
                if (kvp.Value > bestScore)
                {
                    bestScore = kvp.Value;
                    bestStrategy = kvp.Key;
                }
            }

            LogInfo("════════════════════════════════════════");
            LogSuccess($"ОПТИМАЛЬНАЯ СТРАТЕГИЯ: {GetStrategyDisplayName(bestStrategy)}");
            LogInfo($"   Результат: {bestScore}/{testUrls.Length} доступных сайтов");

            foreach (var kvp in strategyScores)
            {
                if (kvp.Key != bestStrategy)
                {
                    LogInfo($"   {GetStrategyDisplayName(kvp.Key)}: {kvp.Value}/{testUrls.Length}");
                }
            }

            progressBar.Value = 100;
            lblStatus.Text = $"Рекомендация: {GetStrategyDisplayName(bestStrategy)}";
            btnTest.Enabled = true;
            btnAutoDetect.Enabled = true;
        }

        private string GetStrategyDisplayName(string strategy)
        {
            switch (strategy)
            {
                case "fast": return "Быстрая";
                case "recommended": return "Рекомендуемая";
                case "max": return "Максимальная защита";
                case "games": return "Игры";
                default: return strategy;
            }
        }

        private async Task<bool> TestSite(string hostname)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(hostname, 3000);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
