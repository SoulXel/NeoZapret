using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.IO;

namespace NeoZapret
{
    public partial class StrategyGeneratorForm : Form
    {
        private NumericUpDown numRepeats;
        private NumericUpDown numTcpRepeats;
        private ComboBox cmbDesyncMode;
        private ComboBox cmbCutoff;
        private CheckBox chkIncludeTLS;
        private CheckBox chkIncludeQUIC;
        private RichTextBox txtGenerated;
        private Button btnGenerate;
        private Button btnSave;
        private Button btnApply;
        public string GeneratedStrategy { get; private set; }
        public bool StrategyApplied { get; private set; }

        public StrategyGeneratorForm()
        {
            InitializeComponent();
            StrategyApplied = false;
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "Генератор стратегий";
            this.Size = new Size(820, 800);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(32, 32, 38);
            this.Paint += (s, e) =>
            {
                var rect = this.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;
                
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
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
                Text = "Генератор стратегий",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 220, 230),
                AutoSize = true,
                Location = new Point(20, 20)
            };
            
            // Информация об авторе
            Label lblAuthor = new Label
            {
                Text = "Разработчик: Soulxel | GitHub: soulxel | Telegram: @xeldi | Discord: Lu1ky | Тестеровщик: Матвей Котов",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 150, 150),
                AutoSize = true,
                Location = new Point(20, 760)
            };

            Label lblInfo = new Label
            {
                Text = "Создайте собственную стратегию обхода с вашими параметрами",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(150, 150, 160),
                AutoSize = true,
                Location = new Point(20, 50)
            };

            int yPos = 90;

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
                BorderStyle = BorderStyle.FixedSingle
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
                BorderStyle = BorderStyle.FixedSingle
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

            yPos += 50;

            // Кнопки
            btnGenerate = CreateButton("Сгенерировать стратегию", 
                new Point(20, yPos), Color.FromArgb(0, 120, 215), 760);

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
                Size = new Size(760, 200),
                BackColor = Color.FromArgb(24, 24, 30),
                ForeColor = Color.FromArgb(220, 220, 230),
                Font = new Font("Consolas", 8),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                BorderStyle = BorderStyle.None
            };

            yPos += 210;

            btnSave = CreateButton("Сохранить в файл", 
                new Point(20, yPos), Color.FromArgb(16, 124, 16), 365);
            
            btnApply = CreateButton("Применить стратегию", 
                new Point(395, yPos), Color.FromArgb(0, 120, 215), 365);

            Button btnClose = CreateButton("Закрыть", 
                new Point(20, yPos + 60), Color.FromArgb(100, 100, 100), 760);
            btnClose.DialogResult = DialogResult.Cancel;

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblInfo);
            this.Controls.Add(lblUdpRepeats);
            this.Controls.Add(numRepeats);
            this.Controls.Add(lblInfo1);
            this.Controls.Add(lblTcpRepeats);
            this.Controls.Add(numTcpRepeats);
            this.Controls.Add(lblDesync);
            this.Controls.Add(cmbDesyncMode);
            this.Controls.Add(lblCutoff);
            this.Controls.Add(cmbCutoff);
            this.Controls.Add(chkIncludeTLS);
            this.Controls.Add(chkIncludeQUIC);
            this.Controls.Add(btnGenerate);
            this.Controls.Add(lblResult);
            this.Controls.Add(txtGenerated);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnApply);
            this.Controls.Add(btnClose);
            this.Controls.Add(lblAuthor);

            btnGenerate.Click += BtnGenerate_Click;
            btnSave.Click += BtnSave_Click;
            btnApply.Click += BtnApply_Click;
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
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(255, backColor.R + 25), 
                Math.Min(255, backColor.G + 25), 
                Math.Min(255, backColor.B + 25));
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(
                Math.Max(0, backColor.R - 25), 
                Math.Max(0, backColor.G - 25), 
                Math.Max(0, backColor.B - 25));
            
            // Закругление кнопок
            btn.Paint += (s, e) =>
            {
                var rect = btn.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;
                
                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
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

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            var repeats = (int)numRepeats.Value;
            var tcpRepeats = (int)numTcpRepeats.Value;
            var desync = cmbDesyncMode.SelectedItem.ToString();
            var cutoff = cmbCutoff.SelectedIndex == 0 ? "n1" : cmbCutoff.SelectedIndex == 1 ? "n2" : "n3";
            
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
            
            MessageBox.Show("Стратегия успешно сгенерирована!", "NeoZapret", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtGenerated.Text))
            {
                MessageBox.Show("Сначала сгенерируйте стратегию!", "NeoZapret", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Сохраняем в папку strategies рядом с приложением
            var strategiesPath = Path.Combine(Application.StartupPath, "strategies");
            if (!Directory.Exists(strategiesPath))
            {
                Directory.CreateDirectory(strategiesPath);
            }

            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                saveDialog.InitialDirectory = strategiesPath;
                saveDialog.FileName = "custom_strategy.txt";
                
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(saveDialog.FileName, txtGenerated.Text);
                        MessageBox.Show($"Стратегия сохранена в:\n{saveDialog.FileName}", "NeoZapret", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения: {ex.Message}", "NeoZapret", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
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
    }
}

