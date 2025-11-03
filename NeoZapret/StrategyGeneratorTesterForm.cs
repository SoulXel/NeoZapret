using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NeoZapret
{
    public partial class StrategyGeneratorTesterForm : Form
    {
        // Элементы генератора
        private NumericUpDown numRepeats;
        private NumericUpDown numTcpRepeats;
        private ComboBox cmbDesyncMode;
        private ComboBox cmbCutoff;
        private CheckBox chkIncludeTLS;
        private CheckBox chkIncludeQUIC;
        private CheckBox chkRandomGeneration;
        private RichTextBox txtGenerated;
        private Button btnGenerate;
        private Button btnSave;
        private Button btnApply;
        
        // Элементы тестера
        private RichTextBox txtResults;
        private Button btnTest;
        private Button btnAutoDetect;
        private ProgressBar progressBar;
        private Label lblStatus;
        private TabControl tabControl;
        
        public string GeneratedStrategy { get; private set; }
        public bool StrategyApplied { get; private set; }

        // Расширенный список тестовых сайтов (заблокированные в РФ)
        private readonly string[] testUrls = new string[]
        {
            // Основные сервисы
            "google.com",
            "youtube.com",
            "gmail.com",
            "google.ru",
            
            // Discord и коммуникации
            "discord.com",
            "discord.gg",
            "discord.media",
            "discordapp.com",
            
            // Разработка и код
            "github.com",
            "gitlab.com",
            "bitbucket.org",
            "cursor.sh",
            "vscode.dev",
            
            // AI сервисы
            "openai.com",
            "chat.openai.com",
            "claude.ai",
            "perplexity.ai",
            "anthropic.com",
            
            // Социальные сети
            "twitter.com",
            "x.com",
            "reddit.com",
            "instagram.com",
            "facebook.com",
            "tiktok.com",
            "linkedin.com",
            
            // Стриминг и медиа
            "twitch.tv",
            "spotify.com",
            "soundcloud.com",
            "netflix.com",
            "vimeo.com",
            "dailymotion.com",
            
            // Игровые платформы
            "steampowered.com",
            "steamcommunity.com",
            "epicgames.com",
            "unity.com",
            "unrealengine.com",
            "ea.com",
            
            // Adobe и Autodesk
            "adobe.com",
            "autodesk.com",
            
            // Прочее
            "wikipedia.org",
            "archive.org",
            "cloudflare.com"
        };

        public StrategyGeneratorTesterForm()
        {
            InitializeComponent();
            StrategyApplied = false;
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "Генератор стратегий";
            this.Size = new Size(900, 840);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(32, 32, 38);
            this.Padding = new Padding(0);
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

            // Заголовок
            Label lblTitle = new Label
            {
                Text = "Генератор и тестирование стратегий",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 220, 230),
                AutoSize = true,
                Location = new Point(20, 15)
            };
            
            // Информация об авторе и тестровщике
            Label lblAuthor = new Label
            {
                Text = "Разработчик: Soulxel | GitHub: soulxel | Telegram: @xeldi | Discord: Lu1ky | Тестеровщик: Матвей Котов",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 150, 160),
                AutoSize = true,
                Location = new Point(20, 795)
            };

            // Вкладки
            tabControl = new TabControl
            {
                Location = new Point(20, 50),
                Size = new Size(860, 730),
                Font = new Font("Segoe UI", 11),
                Appearance = TabAppearance.FlatButtons,
                Padding = new Point(30, 8)
            };
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += TabControl_DrawItem;
            tabControl.BackColor = Color.FromArgb(32, 32, 38);
            tabControl.SelectedIndex = 0;

            // Вкладка 1: Генератор
            TabPage tabGenerator = new TabPage("Генератор");
            tabGenerator.BackColor = Color.FromArgb(32, 32, 38);
            tabGenerator.Padding = new Padding(0);
            tabGenerator.UseVisualStyleBackColor = false;
            InitializeGeneratorTab(tabGenerator);

            // Вкладка 2: Тестирование
            TabPage tabTester = new TabPage("Тестирование");
            tabTester.BackColor = Color.FromArgb(32, 32, 38);
            tabTester.Padding = new Padding(0);
            tabTester.UseVisualStyleBackColor = false;
            InitializeTesterTab(tabTester);

            tabControl.TabPages.Add(tabGenerator);
            tabControl.TabPages.Add(tabTester);

            Button btnClose = CreateButton("Закрыть", 
                new Point(740, 790), Color.FromArgb(75, 75, 85), 140);
            btnClose.DialogResult = DialogResult.Cancel;

            this.Controls.Add(lblTitle);
            this.Controls.Add(tabControl);
            this.Controls.Add(btnClose);
            this.Controls.Add(lblAuthor);
        }

        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabControl tc = (TabControl)sender;
            TabPage tab = tc.TabPages[e.Index];
            Rectangle r = e.Bounds;
            
            // Увеличиваем высоту вкладок для лучшего вида
            r.Height += 2;
            
            if (e.Index == tc.SelectedIndex)
            {
                // Активная вкладка - основной цвет формы
                using (var brush = new SolidBrush(Color.FromArgb(32, 32, 38)))
                {
                    e.Graphics.FillRectangle(brush, r);
                }
                
                // Верхняя граница активной вкладки - убрана
                
                // Рамка вокруг активной вкладки (темная)
                using (var pen = new Pen(Color.FromArgb(50, 50, 56), 1))
                {
                    e.Graphics.DrawLine(pen, r.X, r.Y, r.X, r.Y + r.Height); // Левая
                    e.Graphics.DrawLine(pen, r.X + r.Width - 1, r.Y, r.X + r.Width - 1, r.Y + r.Height); // Правая
                    e.Graphics.DrawLine(pen, r.X, r.Y + r.Height - 1, r.X + r.Width, r.Y + r.Height - 1); // Нижняя
                }
                
                e.Graphics.DrawString(tab.Text, new Font("Segoe UI", 11, FontStyle.Bold), 
                    new SolidBrush(Color.FromArgb(240, 240, 245)), r, 
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }
            else
            {
                // Неактивная вкладка - темнее
                using (var brush = new SolidBrush(Color.FromArgb(38, 38, 44)))
                {
                    e.Graphics.FillRectangle(brush, r);
                }
                
                // Тонкая граница снизу для неактивной вкладки
                using (var pen = new Pen(Color.FromArgb(50, 50, 56), 1))
                {
                    e.Graphics.DrawLine(pen, r.X, r.Y + r.Height - 1, r.X + r.Width, r.Y + r.Height - 1);
                }
                
                e.Graphics.DrawString(tab.Text, new Font("Segoe UI", 11), 
                    new SolidBrush(Color.FromArgb(140, 140, 150)), r, 
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }
        }

        private void InitializeGeneratorTab(TabPage tab)
        {
            int yPos = 20;

            Label lblInfo = new Label
            {
                Text = "Создайте собственную стратегию обхода с вашими параметрами",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(150, 150, 160),
                AutoSize = true,
                Location = new Point(20, yPos)
            };
            yPos += 35;

            // UDP повторы
            Label lblUdpRepeats = new Label
            {
                Text = "UDP повторы (DPI desync):",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(200, 200, 210),
                AutoSize = true,
                Location = new Point(30, yPos)
            };

            numRepeats = new NumericUpDown
            {
                Location = new Point(250, yPos),
                Size = new Size(100, 25),
                Minimum = 1,
                Maximum = 20,
                Value = 3,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(38, 38, 44),
                ForeColor = Color.FromArgb(220, 220, 230),
                BorderStyle = BorderStyle.None
            };
            numRepeats.Paint += (s, e) =>
            {
                var rect = numRepeats.ClientRectangle;
                using (var pen = new Pen(Color.FromArgb(60, 60, 70), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
                }
            };

            Label lblInfo1 = new Label
            {
                Text = "(больше = надежнее, но медленнее)",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(120, 120, 130),
                AutoSize = true,
                Location = new Point(360, yPos + 5)
            };
            yPos += 40;

            // TCP повторы
            Label lblTcpRepeats = new Label
            {
                Text = "TCP повторы:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(200, 200, 210),
                AutoSize = true,
                Location = new Point(30, yPos)
            };

            numTcpRepeats = new NumericUpDown
            {
                Location = new Point(250, yPos),
                Size = new Size(100, 25),
                Minimum = 1,
                Maximum = 20,
                Value = 6,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(38, 38, 44),
                ForeColor = Color.FromArgb(220, 220, 230),
                BorderStyle = BorderStyle.None
            };
            numTcpRepeats.Paint += (s, e) =>
            {
                var rect = numTcpRepeats.ClientRectangle;
                using (var pen = new Pen(Color.FromArgb(60, 60, 70), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
                }
            };
            yPos += 40;

            // Режим десинхронизации
            Label lblDesync = new Label
            {
                Text = "Режим DPI desync:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(200, 200, 210),
                AutoSize = true,
                Location = new Point(30, yPos)
            };

            cmbDesyncMode = new ComboBox
            {
                Location = new Point(250, yPos),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(38, 38, 44),
                ForeColor = Color.FromArgb(220, 220, 230),
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Items = { "fake", "multisplit", "fakedsplit", "fake,multisplit", "fake,fakedsplit" }
            };
            cmbDesyncMode.SelectedIndex = 1;
            cmbDesyncMode.Paint += (s, e) =>
            {
                var rect = cmbDesyncMode.ClientRectangle;
                using (var pen = new Pen(Color.FromArgb(60, 60, 70), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
                }
            };
            yPos += 40;

            // Cutoff режим
            Label lblCutoff = new Label
            {
                Text = "Cutoff режим:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(200, 200, 210),
                AutoSize = true,
                Location = new Point(30, yPos)
            };

            cmbCutoff = new ComboBox
            {
                Location = new Point(250, yPos),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(38, 38, 44),
                ForeColor = Color.FromArgb(220, 220, 230),
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Items = { "n1 (быстро)", "n2 (средне)", "n3 (медленно)" }
            };
            cmbCutoff.SelectedIndex = 0;
            cmbCutoff.Paint += (s, e) =>
            {
                var rect = cmbCutoff.ClientRectangle;
                using (var pen = new Pen(Color.FromArgb(60, 60, 70), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
                }
            };
            yPos += 40;

            // TLS/QUIC
            chkIncludeTLS = new CheckBox
            {
                Text = "Включить TLS fooling",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(220, 220, 230),
                BackColor = Color.FromArgb(32, 32, 38),
                AutoSize = true,
                Location = new Point(30, yPos),
                Checked = true,
                FlatStyle = FlatStyle.Flat
            };
            ApplyDarkCheckBoxStyle(chkIncludeTLS);
            yPos += 30;

            chkIncludeQUIC = new CheckBox
            {
                Text = "Включить QUIC fake packets",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(220, 220, 230),
                BackColor = Color.FromArgb(32, 32, 38),
                AutoSize = true,
                Location = new Point(30, yPos),
                Checked = true,
                FlatStyle = FlatStyle.Flat
            };
            ApplyDarkCheckBoxStyle(chkIncludeQUIC);
            yPos += 30;

            // Чекбокс рандомной генерации
            chkRandomGeneration = new CheckBox
            {
                Text = "Рандомная генерация (случайные параметры при каждом нажатии)",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 200, 210),
                BackColor = Color.FromArgb(32, 32, 38),
                AutoSize = true,
                Location = new Point(30, yPos),
                Checked = false,
                FlatStyle = FlatStyle.Flat
            };
            ApplyDarkCheckBoxStyle(chkRandomGeneration);
            yPos += 50;

            // Кнопка генерации
            Color buttonColor = Color.FromArgb(50, 50, 58);
            
            btnGenerate = CreateButton("Сгенерировать и протестировать стратегию", 
                new Point(20, yPos), buttonColor, 800);
            btnGenerate.Click += BtnGenerate_Click;
            yPos += 60;

            // Сгенерированный результат
            Label lblResult = new Label
            {
                Text = "Сгенерированная стратегия:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 200, 210),
                AutoSize = true,
                Location = new Point(20, yPos)
            };
            yPos += 30;

            txtGenerated = new RichTextBox
            {
                Location = new Point(20, yPos),
                Size = new Size(800, 200),
                BackColor = Color.FromArgb(38, 38, 44),
                ForeColor = Color.FromArgb(220, 220, 230),
                Font = new Font("Consolas", 8),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                BorderStyle = BorderStyle.None
            };
            yPos += 210;

            btnSave = CreateButton("Сохранить в файл", 
                new Point(20, yPos), buttonColor, 390);
            btnSave.Click += BtnSave_Click;
            
            btnApply = CreateButton("Применить стратегию", 
                new Point(420, yPos), buttonColor, 390);
            btnApply.Click += BtnApply_Click;
            
            yPos += 55; // Увеличиваем отступ после кнопок

            tab.Controls.Add(lblInfo);
            tab.Controls.Add(lblUdpRepeats);
            tab.Controls.Add(numRepeats);
            tab.Controls.Add(lblInfo1);
            tab.Controls.Add(lblTcpRepeats);
            tab.Controls.Add(numTcpRepeats);
            tab.Controls.Add(lblDesync);
            tab.Controls.Add(cmbDesyncMode);
            tab.Controls.Add(lblCutoff);
            tab.Controls.Add(cmbCutoff);
            tab.Controls.Add(chkIncludeTLS);
            tab.Controls.Add(chkIncludeQUIC);
            tab.Controls.Add(chkRandomGeneration);
            tab.Controls.Add(btnGenerate);
            tab.Controls.Add(lblResult);
            tab.Controls.Add(txtGenerated);
            tab.Controls.Add(btnSave);
            tab.Controls.Add(btnApply);
        }

        private void InitializeTesterTab(TabPage tab)
        {
            int yPos = 20;

            Label lblInfo = new Label
            {
                Text = $"Автоматическое тестирование на {testUrls.Length} заблокированных сайтах",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(150, 150, 160),
                AutoSize = true,
                Location = new Point(20, yPos)
            };
            yPos += 35;

            btnTest = CreateButton("Тестировать текущую стратегию", 
                new Point(20, yPos), Color.FromArgb(80, 80, 90), 800);
            btnTest.Click += BtnTest_Click;
            yPos += 60;

            btnAutoDetect = CreateButton("Автоопределение оптимальной стратегии", 
                new Point(20, yPos), Color.FromArgb(78, 78, 88), 800);
            btnAutoDetect.Click += BtnAutoDetect_Click;
            yPos += 60;

            progressBar = new ProgressBar
            {
                Location = new Point(20, yPos),
                Size = new Size(800, 25),
                Style = ProgressBarStyle.Continuous,
                ForeColor = Color.FromArgb(100, 100, 110),
                BackColor = Color.FromArgb(38, 38, 44)
            };
            yPos += 35;

            lblStatus = new Label
            {
                Text = "Готов к тестированию",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(200, 200, 210),
                AutoSize = true,
                Location = new Point(20, yPos)
            };
            yPos += 35;

            Panel resultPanel = new Panel
            {
                Location = new Point(20, yPos),
                Size = new Size(800, 440),
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
                Size = new Size(790, 430),
                BackColor = Color.FromArgb(24, 24, 30),
                ForeColor = Color.FromArgb(220, 220, 230),
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                BorderStyle = BorderStyle.None
            };
            resultPanel.Controls.Add(txtResults);

            tab.Controls.Add(lblInfo);
            tab.Controls.Add(btnTest);
            tab.Controls.Add(btnAutoDetect);
            tab.Controls.Add(progressBar);
            tab.Controls.Add(lblStatus);
            tab.Controls.Add(resultPanel);
        }

        private Button CreateButton(string text, Point location, Color backColor, int width)
        {
            var btn = new Button
            {
                Text = text,
                Location = location,
                Size = new Size(width, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.FromArgb(220, 220, 230),
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

        private async void BtnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                btnGenerate.Enabled = false;
                btnSave.Enabled = false;
                btnApply.Enabled = false;

                int repeats;
                int tcpRepeats;
                string desync;
                string cutoff;

                // Если включена рандомная генерация, выбираем случайные параметры
                if (chkRandomGeneration.Checked)
                {
                    Random rnd = new Random();
                    
                    // Случайные UDP повторы (от 2 до 8)
                    repeats = rnd.Next(2, 9);
                    
                    // Случайные TCP повторы (от 3 до 10)
                    tcpRepeats = rnd.Next(3, 11);
                    
                    // Случайный режим DPI desync
                    string[] desyncModes = { "fake", "multisplit", "fakedsplit", "fake,multisplit", "fake,fakedsplit" };
                    desync = desyncModes[rnd.Next(desyncModes.Length)];
                    
                    // Случайный cutoff режим
                    string[] cutoffModes = { "n1", "n2", "n3" };
                    cutoff = cutoffModes[rnd.Next(cutoffModes.Length)];
                    
                    // Случайно включаем/выключаем TLS и QUIC
                    chkIncludeTLS.Checked = rnd.Next(2) == 1;
                    chkIncludeQUIC.Checked = rnd.Next(2) == 1;
                    
                    // Обновляем UI с новыми значениями
                    numRepeats.Value = repeats;
                    numTcpRepeats.Value = tcpRepeats;
                    cmbDesyncMode.SelectedItem = desync;
                    cmbCutoff.SelectedIndex = cutoff == "n1" ? 0 : (cutoff == "n2" ? 1 : 2);
                }
                else
                {
                    // Используем значения из UI
                    repeats = (int)numRepeats.Value;
                    tcpRepeats = (int)numTcpRepeats.Value;
                    desync = cmbDesyncMode.SelectedItem.ToString();
                    cutoff = cmbCutoff.SelectedIndex == 0 ? "n1" : cmbCutoff.SelectedIndex == 1 ? "n2" : "n3";
                }
                
                var gameFilter = "12";
                var args = $"--wf-tcp=80,443,2053,2083,2087,2096,8443,{gameFilter} --wf-udp=443,19294-19344,50000-50100,{gameFilter} ";

                // Генерация параметров
                args += $"--filter-udp=443 --hostlist=\"lists\\list-general.txt\" --hostlist-exclude=\"lists\\list-exclude.txt\" --ipset-exclude=\"lists\\ipset-exclude.txt\" --dpi-desync={desync} --dpi-desync-repeats={repeats} ";
                
                if (chkIncludeQUIC.Checked)
                {
                    args += "--dpi-desync-fake-quic=\"bin\\quic_initial_www_google_com.bin\" ";
                }
                
                args += "--new ";
                args += $"--filter-tcp=80,443 --hostlist=\"lists\\list-general.txt\" --hostlist-exclude=\"lists\\list-exclude.txt\" --ipset-exclude=\"lists\\ipset-exclude.txt\" --dpi-desync={desync} --dpi-desync-repeats={tcpRepeats} ";
                
                if (chkIncludeTLS.Checked)
                {
                    args += "--dpi-desync-fooling=ts --dpi-desync-fake-tls=\"bin\\tls_clienthello_www_google_com.bin\" ";
                }
                
                args += "--new ";
                args += $"--filter-udp={gameFilter} --ipset=\"lists\\ipset-all.txt\" --ipset-exclude=\"lists\\ipset-exclude.txt\" --dpi-desync=fake --dpi-desync-autottl=1 --dpi-desync-repeats={repeats * 2} --dpi-desync-any-protocol=1 --dpi-desync-cutoff={cutoff}";
                
                if (chkIncludeQUIC.Checked)
                {
                    args += " --dpi-desync-fake-unknown-udp=\"bin\\quic_initial_www_google_com.bin\"";
                }

                txtGenerated.Text = args;
                GeneratedStrategy = args;

                // Автоматически переключаемся на вкладку тестирования и запускаем тест
                tabControl.SelectedIndex = 1;
                await Task.Delay(300);

                if (chkRandomGeneration.Checked)
                {
                    LogInfo("════════════════════════════════════════");
                    LogInfo($"Генерация РАНДОМНОЙ стратегии завершена!");
                    LogInfo($"Параметры: UDP повторы={repeats}, TCP повторы={tcpRepeats}, Desync={desync}, Cutoff={cutoff}");
                    LogInfo($"TLS={chkIncludeTLS.Checked}, QUIC={chkIncludeQUIC.Checked}");
                    LogInfo("════════════════════════════════════════");
                }
                else
                {
                    LogInfo("Генерация стратегии завершена");
                }
                
                LogInfo("Начинаю автоматическое тестирование...");
                LogInfo("════════════════════════════════════════");

                await RunTests();

                LogInfo("════════════════════════════════════════");
                LogSuccess("Тестирование завершено!");
                LogInfo("Переключитесь на вкладку 'Генератор' для сохранения или применения стратегии.");

                btnGenerate.Enabled = true;
                btnSave.Enabled = true;
                btnApply.Enabled = true;
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при генерации: {ex.Message}");
                btnGenerate.Enabled = true;
                btnSave.Enabled = true;
                btnApply.Enabled = true;
            }
        }

        private async Task RunTests()
        {
            try
            {
                UpdateUI(() => { progressBar.Value = 0; lblStatus.Text = "Тестирование стратегии..."; });
                
                int success = 0;
                int total = testUrls.Length;

                for (int i = 0; i < total; i++)
                {
                    UpdateUI(() =>
                    {
                        lblStatus.Text = $"Проверяю {testUrls[i]}... ({i + 1}/{total})";
                        progressBar.Value = (int)((i + 1.0) / total * 100);
                    });

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

                    await Task.Delay(100); // Небольшая задержка между проверками
                }

                UpdateUI(() =>
                {
                    progressBar.Value = 100;
                    lblStatus.Text = $"Тестирование завершено: {success}/{total} доступно";
                });

                if (success == total)
                {
                    LogSuccess($"\nВсе сайты доступны! ({success}/{total})");
                }
                else if (success >= total * 0.7)
                {
                    LogWarning($"\nХорошая доступность ({success}/{total})");
                }
                else
                {
                    LogWarning($"\nЧастичная доступность ({success}/{total})");
                    LogInfo("Рекомендуется настроить параметры стратегии");
                }
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при тестировании: {ex.Message}");
                UpdateUI(() => { lblStatus.Text = "Ошибка тестирования"; });
            }
        }

        private void UpdateUI(Action action)
        {
            try
            {
                if (this.IsDisposed || !this.IsHandleCreated)
                    return;

                if (this.InvokeRequired)
                {
                    if (!this.IsDisposed && this.IsHandleCreated)
                        this.Invoke(action);
                }
                else
                {
                    action();
                }
            }
            catch (ObjectDisposedException)
            {
                // Игнорируем, если объект уже уничтожен
            }
            catch (InvalidOperationException)
            {
                // Игнорируем, если форма закрыта
            }
            catch { }
        }

        private async void BtnTest_Click(object sender, EventArgs e)
        {
            try
            {
                btnTest.Enabled = false;
                btnAutoDetect.Enabled = false;
                
                txtResults.Clear();
                LogInfo("Начинаю тестирование доступности сайтов...");
                LogInfo("════════════════════════════════════════");

                await RunTests();

                btnTest.Enabled = true;
                btnAutoDetect.Enabled = true;
            }
            catch (Exception ex)
            {
                LogError($"Ошибка: {ex.Message}");
                btnTest.Enabled = true;
                btnAutoDetect.Enabled = true;
            }
        }

        private async void BtnAutoDetect_Click(object sender, EventArgs e)
        {
            try
            {
                btnTest.Enabled = false;
                btnAutoDetect.Enabled = false;
                
                txtResults.Clear();
                UpdateUI(() => progressBar.Value = 0);

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
                LogInfo($"Тестирую стратегию: {GetStrategyDisplayName(strategy)}...");
                
                await Task.Delay(500);

                int score = 0;
                foreach (var url in testUrls)
                {
                    currentTest++;
                    UpdateUI(() =>
                    {
                        progressBar.Value = (int)(currentTest / (double)totalTests * 100);
                        lblStatus.Text = $"Тестирую {GetStrategyDisplayName(strategy)}: {url}...";
                    });
                    
                    bool result = await TestSite(url);
                    if (result) score++;
                    
                    await Task.Delay(50);
                }

                strategyScores[strategy] = score;
                LogInfo($"Стратегия '{GetStrategyDisplayName(strategy)}': {score}/{testUrls.Length} доступных");
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

            UpdateUI(() =>
            {
                progressBar.Value = 100;
                lblStatus.Text = $"Рекомендация: {GetStrategyDisplayName(bestStrategy)}";
            });
            
            btnTest.Enabled = true;
            btnAutoDetect.Enabled = true;
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при автоопределении: {ex.Message}");
                btnTest.Enabled = true;
                btnAutoDetect.Enabled = true;
            }
        }

        private string GetStrategyDisplayName(string strategy)
        {
            switch (strategy)
            {
                case "fast": return "Быстрая";
                case "recommended": return "Рекомендуемая";
                case "max": return "Максимальная защита";
                case "games": return "Только игры";
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

        private void Log(string message, Color color)
        {
            try
            {
                if (txtResults == null || txtResults.IsDisposed || this.IsDisposed || !this.IsHandleCreated)
                    return;

                if (txtResults.InvokeRequired)
                {
                    if (!txtResults.IsDisposed && !this.IsDisposed && this.IsHandleCreated)
                    {
                        txtResults.Invoke(new Action<string, Color>(Log), message, color);
                    }
                    return;
                }

                if (txtResults.IsDisposed) return;

                txtResults.SelectionStart = txtResults.TextLength;
                txtResults.SelectionLength = 0;
                txtResults.SelectionColor = color;
                txtResults.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                txtResults.SelectionColor = txtResults.ForeColor;
                txtResults.ScrollToCaret();
            }
            catch (ObjectDisposedException)
            {
                // Игнорируем, если объект уже уничтожен
            }
            catch (InvalidOperationException)
            {
                // Игнорируем, если форма закрыта
            }
        }

        private void LogSuccess(string message) => Log(message, Color.FromArgb(150, 200, 150));
        private void LogError(string message) => Log(message, Color.FromArgb(220, 150, 150));
        private void LogWarning(string message) => Log(message, Color.FromArgb(220, 200, 150));
        private void LogInfo(string message) => Log(message, Color.FromArgb(180, 180, 200));

        private void ApplyDarkCheckBoxStyle(CheckBox chk)
        {
            chk.Paint += (s, e) =>
            {
                var checkbox = s as CheckBox;
                if (checkbox == null) return;
                
                e.Graphics.Clear(checkbox.BackColor);
                
                // Рамка чекбокса (темная)
                var checkBoxRect = new Rectangle(0, (checkbox.Height - 18) / 2, 18, 18);
                using (var borderPen = new Pen(Color.FromArgb(120, 120, 130), 2))
                {
                    e.Graphics.DrawRectangle(borderPen, checkBoxRect);
                }
                
                // Если отмечен - рисуем галочку
                if (checkbox.Checked)
                {
                    using (var checkBrush = new SolidBrush(Color.FromArgb(150, 200, 150)))
                    {
                        e.Graphics.FillRectangle(checkBrush, checkBoxRect);
                        
                        // Галочка
                        using (var checkPen = new Pen(checkbox.BackColor, 2))
                        {
                            var points = new[]
                            {
                                new Point(4, checkBoxRect.Height / 2),
                                new Point(checkBoxRect.Width / 2 - 1, checkBoxRect.Height - 4),
                                new Point(checkBoxRect.Width - 4, 2)
                            };
                            e.Graphics.DrawLines(checkPen, points);
                        }
                    }
                }
                
                // Текст
                var textRect = new Rectangle(24, 0, checkbox.Width - 24, checkbox.Height);
                TextRenderer.DrawText(e.Graphics, checkbox.Text, checkbox.Font, textRect, checkbox.ForeColor, checkbox.BackColor, 
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            };
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtGenerated.Text))
            {
                MessageBox.Show("Сначала сгенерируйте стратегию!", "NeoZapret", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var startupPath = Application.StartupPath;
                if (string.IsNullOrEmpty(startupPath))
                    startupPath = AppDomain.CurrentDomain.BaseDirectory;
                
                var strategiesPath = Path.Combine(startupPath ?? Environment.CurrentDirectory ?? ".", "strategies");
                if (!Directory.Exists(strategiesPath))
                {
                    Directory.CreateDirectory(strategiesPath);
                }

                using (var saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                    saveDialog.InitialDirectory = strategiesPath;
                    saveDialog.FileName = $"custom_strategy_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                    
                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(saveDialog.FileName, txtGenerated.Text);
                        MessageBox.Show($"Стратегия сохранена в:\n{saveDialog.FileName}", "NeoZapret", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "NeoZapret", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtGenerated.Text))
            {
                MessageBox.Show("Сначала сгенерируйте стратегию!", "NeoZapret", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            GeneratedStrategy = txtGenerated.Text;
            StrategyApplied = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

