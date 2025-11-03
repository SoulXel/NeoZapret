using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.IO;

namespace NeoZapret
{
    public partial class AdvancedSettingsForm : Form
    {
        private TextBox txtDNSServer;
        private CheckBox chkUseCustomDNS;
        private NumericUpDown numUdpRepeats;
        private NumericUpDown numTcpRepeats;
        private ComboBox cmbCutoffMode;
        private TextBox txtProxyServer;

        public AdvancedSettingsForm()
        {
            InitializeComponent();
            LoadSavedSettings();
        }

        private void LoadSavedSettings()
        {
            try
            {
                txtDNSServer.Text = SettingsManager.LoadSetting("DNSServer", "1.1.1.1");
                chkUseCustomDNS.Checked = SettingsManager.LoadBoolSetting("UseCustomDNS", false);
                numUdpRepeats.Value = SettingsManager.LoadIntSetting("UdpRepeats", 3);
                numTcpRepeats.Value = SettingsManager.LoadIntSetting("TcpRepeats", 6);
                cmbCutoffMode.SelectedIndex = SettingsManager.LoadIntSetting("CutoffMode", 0);
                var proxy = SettingsManager.LoadSetting("ProxyServer", "");
                txtProxyServer.Text = string.IsNullOrEmpty(proxy) ? "socks5://127.0.0.1:1080" : proxy;
                if (!string.IsNullOrEmpty(proxy))
                {
                    txtProxyServer.ForeColor = Color.FromArgb(220, 220, 230);
                }
            }
            catch { }
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "Дополнительные настройки";
            this.Size = new Size(520, 430);
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
                Text = "Дополнительные настройки",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 220, 230),
                AutoSize = true,
                Location = new Point(30, 20)
            };

            Label lblAuthor = new Label
            {
                Text = "Разработчик: Soulxel | GitHub: soulxel | Telegram: @xeldi | Discord: Lu1ky | Тестеровщик: Матвей Котов",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 150, 160),
                AutoSize = false,
                Size = new Size(480, 35), // Фиксированная ширина для полного отображения
                Location = new Point(30, 380)
            };

            Label lblDNS = new Label
            {
                Text = "DNS сервер:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(200, 200, 210),
                AutoSize = true,
                Location = new Point(30, 60)
            };

            txtDNSServer = new TextBox
            {
                Text = "1.1.1.1",
                Location = new Point(30, 85),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(38, 38, 44),
                ForeColor = Color.FromArgb(220, 220, 230),
                BorderStyle = BorderStyle.None
            };
            txtDNSServer.Paint += (s, e) =>
            {
                var rect = txtDNSServer.ClientRectangle;
                using (var pen = new Pen(Color.FromArgb(60, 60, 70), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
                }
            };

            chkUseCustomDNS = new CheckBox
            {
                Text = "Использовать кастомный DNS",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(220, 220, 230),
                BackColor = Color.FromArgb(32, 32, 38),
                AutoSize = true,
                Location = new Point(250, 87),
                FlatStyle = FlatStyle.Flat
            };
            // Кастомная отрисовка чекбокса для темной темы
            chkUseCustomDNS.Paint += (s, e) =>
            {
                var chk = s as CheckBox;
                if (chk == null) return;
                
                e.Graphics.Clear(chk.BackColor);
                
                // Рамка чекбокса (темная)
                var checkBoxRect = new Rectangle(0, (chk.Height - 18) / 2, 18, 18);
                using (var borderPen = new Pen(Color.FromArgb(120, 120, 130), 2))
                {
                    e.Graphics.DrawRectangle(borderPen, checkBoxRect);
                }
                
                // Если отмечен - рисуем галочку
                if (chk.Checked)
                {
                    using (var checkBrush = new SolidBrush(Color.FromArgb(150, 200, 150)))
                    {
                        e.Graphics.FillRectangle(checkBrush, checkBoxRect);
                        
                        // Галочка
                        using (var checkPen = new Pen(Color.FromArgb(32, 32, 38), 2))
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
                var textRect = new Rectangle(24, 0, chk.Width - 24, chk.Height);
                TextRenderer.DrawText(e.Graphics, chk.Text, chk.Font, textRect, chk.ForeColor, chk.BackColor, 
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            };

            Label lblUdpRepeats = new Label
            {
                Text = "UDP повторы:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(200, 200, 210),
                AutoSize = true,
                Location = new Point(30, 130)
            };

            numUdpRepeats = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 20,
                Value = 3,
                Location = new Point(30, 155),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(38, 38, 44),
                ForeColor = Color.FromArgb(220, 220, 230),
                BorderStyle = BorderStyle.None
            };
            numUdpRepeats.Paint += (s, e) =>
            {
                var rect = numUdpRepeats.ClientRectangle;
                using (var pen = new Pen(Color.FromArgb(60, 60, 70), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
                }
            };

            Label lblTcpRepeats = new Label
            {
                Text = "TCP повторы:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(200, 200, 210),
                AutoSize = true,
                Location = new Point(150, 130)
            };

            numTcpRepeats = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 20,
                Value = 6,
                Location = new Point(150, 155),
                Size = new Size(100, 25),
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

            Label lblCutoff = new Label
            {
                Text = "Cutoff режим:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(200, 200, 210),
                AutoSize = true,
                Location = new Point(270, 130)
            };

            cmbCutoffMode = new ComboBox
            {
                Items = { "n1 (быстро)", "n2 (средне)", "n3 (медленно)" },
                SelectedIndex = 0,
                Location = new Point(270, 155),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(38, 38, 44),
                ForeColor = Color.FromArgb(220, 220, 230),
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            Label lblProxy = new Label
            {
                Text = "Прокси сервер (опционально):",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(200, 200, 210),
                AutoSize = true,
                Location = new Point(30, 200)
            };

            txtProxyServer = new TextBox
            {
                Location = new Point(30, 225),
                Size = new Size(440, 25),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(38, 38, 44),
                ForeColor = Color.FromArgb(150, 150, 160),
                BorderStyle = BorderStyle.None,
                Text = "socks5://127.0.0.1:1080"
            };
            txtProxyServer.Paint += (s, e) =>
            {
                var rect = txtProxyServer.ClientRectangle;
                using (var pen = new Pen(Color.FromArgb(60, 60, 70), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
                }
            };
            txtProxyServer.GotFocus += (s, e) => {
                if (txtProxyServer.ForeColor == Color.FromArgb(150, 150, 160))
                {
                    txtProxyServer.Text = "";
                    txtProxyServer.ForeColor = Color.FromArgb(220, 220, 230);
                }
            };
            txtProxyServer.LostFocus += (s, e) => {
                if (string.IsNullOrEmpty(txtProxyServer.Text))
                {
                    txtProxyServer.Text = "socks5://127.0.0.1:1080";
                    txtProxyServer.ForeColor = Color.FromArgb(150, 150, 160);
                }
            };

            Color buttonColor = Color.FromArgb(50, 50, 58);
            
            Button btnExport = CreateButton("Экспорт", new Point(30, 280), buttonColor, 140);
            btnExport.Click += BtnExport_Click;

            Button btnImport = CreateButton("Импорт", new Point(180, 280), buttonColor, 140);
            btnImport.Click += BtnImport_Click;

            Button btnSave = CreateButton("Сохранить", new Point(330, 280), buttonColor, 140);
            btnSave.Click += BtnSave_Click;

            Button btnCancel = CreateButton("Отмена", new Point(30, 330), buttonColor, 440);
            btnCancel.DialogResult = DialogResult.Cancel;

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblDNS);
            this.Controls.Add(txtDNSServer);
            this.Controls.Add(chkUseCustomDNS);
            this.Controls.Add(lblUdpRepeats);
            this.Controls.Add(numUdpRepeats);
            this.Controls.Add(lblTcpRepeats);
            this.Controls.Add(numTcpRepeats);
            this.Controls.Add(lblCutoff);
            this.Controls.Add(cmbCutoffMode);
            this.Controls.Add(lblProxy);
            this.Controls.Add(txtProxyServer);
            this.Controls.Add(btnExport);
            this.Controls.Add(btnImport);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
            this.Controls.Add(lblAuthor);
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

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Валидация DNS
                if (!string.IsNullOrEmpty(txtDNSServer.Text) && !System.Text.RegularExpressions.Regex.IsMatch(txtDNSServer.Text, @"^(\d{1,3}\.){3}\d{1,3}$"))
                {
                    MessageBox.Show("Неверный формат DNS сервера!\n\nИспользуйте формат: 1.1.1.1", 
                        "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtDNSServer.Focus();
                    return;
                }

                // Сохранение настроек
                SettingsManager.SaveSetting("DNSServer", txtDNSServer.Text);
                SettingsManager.SaveBoolSetting("UseCustomDNS", chkUseCustomDNS.Checked);
                SettingsManager.SaveIntSetting("UdpRepeats", (int)numUdpRepeats.Value);
                SettingsManager.SaveIntSetting("TcpRepeats", (int)numTcpRepeats.Value);
                SettingsManager.SaveIntSetting("CutoffMode", cmbCutoffMode.SelectedIndex);
                SettingsManager.SaveSetting("ProxyServer", txtProxyServer.Text == "socks5://127.0.0.1:1080" ? "" : txtProxyServer.Text);

                MessageBox.Show("Настройки успешно сохранены!\n\nИзменения вступят в силу при следующем запуске обхода.", 
                "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении настроек:\n\n{ex.Message}", 
                    "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            try
            {
                using (var saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "Файлы настроек (*.nzconfig)|*.nzconfig|Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                    saveDialog.FileName = $"NeoZapret_Settings_{DateTime.Now:yyyyMMdd_HHmmss}.nzconfig";
                    saveDialog.Title = "Экспорт настроек NeoZapret";

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        if (SettingsExporter.ExportSettings(saveDialog.FileName))
                        {
                            MessageBox.Show($"Настройки успешно экспортированы!\n\nФайл: {saveDialog.FileName}", 
                                "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Не удалось экспортировать настройки!", 
                                "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте настроек:\n\n{ex.Message}", 
                    "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnImport_Click(object sender, EventArgs e)
        {
            try
            {
                using (var openDialog = new OpenFileDialog())
                {
                    openDialog.Filter = "Файлы настроек (*.nzconfig)|*.nzconfig|Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                    openDialog.Title = "Импорт настроек NeoZapret";

                    if (openDialog.ShowDialog() == DialogResult.OK)
                    {
                        var result = MessageBox.Show(
                            "Внимание! Импорт настроек заменит текущие настройки.\n\nПродолжить?",
                            "NeoZapret", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            if (SettingsExporter.ImportSettings(openDialog.FileName))
                            {
                                MessageBox.Show("Настройки успешно импортированы!\n\nПерезапустите приложение для применения изменений.", 
                                    "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                
                                // Перезагружаем настройки
                                LoadSavedSettings();
                            }
                            else
                            {
                                MessageBox.Show("Не удалось импортировать настройки!", 
                                    "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при импорте настроек:\n\n{ex.Message}", 
                    "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
