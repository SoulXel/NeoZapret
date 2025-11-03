using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NeoZapret
{
    /// <summary>
    /// Форма для настройки шифрования трафика через прокси.
    /// </summary>
    public partial class EncryptionSettingsForm : Form
    {
        private ComboBox cmbProxyType;
        private TextBox txtProxyHost;
        private NumericUpDown numProxyPort;
        private CheckBox chkRequireAuth;
        private TextBox txtProxyUsername;
        private TextBox txtProxyPassword;
        private CheckBox chkEnableEncryption;
        private Button btnTestProxy;
        private Button btnSave;
        private Button btnCancel;
        private Label lblStatus;
        private RichTextBox txtInfo;

        public TrafficEncryption.ProxySettings Settings { get; private set; }
        public bool EncryptionEnabled { get; private set; }

        public EncryptionSettingsForm(TrafficEncryption.ProxySettings currentSettings, bool isEnabled)
        {
            EncryptionEnabled = isEnabled;
            Settings = currentSettings;
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "NeoZapret - Шифрование трафика";
            this.Size = new Size(560, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 30, 35);

            int startX = 30;
            int startY = 25;
            int labelWidth = 140;
            int inputWidth = 180;
            int portWidth = 80;
            int rowHeight = 35;
            int currentY = startY;

            // Заголовок
            var lblTitle = new Label
            {
                Text = "Настройка шифрования трафика",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 200, 150),
                AutoSize = true,
                Location = new Point(startX, currentY)
            };
            this.Controls.Add(lblTitle);
            currentY += 35;

            // Информационный блок
            txtInfo = new RichTextBox
            {
                Location = new Point(startX, currentY),
                Size = new Size(500, 55),
                BackColor = Color.FromArgb(40, 40, 45),
                ForeColor = Color.FromArgb(200, 200, 210),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Font = new Font("Segoe UI", 9),
                Text = "Шифрование трафика через прокси обеспечивает защиту от просмотра провайдером. Рекомендуется использовать SOCKS5 прокси с поддержкой шифрования."
            };
            this.Controls.Add(txtInfo);
            currentY += 70;

            // Чекбокс включения шифрования
            chkEnableEncryption = new CheckBox
            {
                Text = "Включить шифрование трафика",
                ForeColor = Color.FromArgb(220, 220, 230),
                BackColor = Color.FromArgb(30, 30, 35),
                Location = new Point(startX, currentY),
                AutoSize = true,
                Checked = EncryptionEnabled,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            // Кастомная отрисовка чекбокса для темной темы
            chkEnableEncryption.Paint += (s, e) =>
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
                        using (var checkPen = new Pen(Color.FromArgb(30, 30, 35), 2))
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
            chkEnableEncryption.CheckedChanged += ChkEnableEncryption_CheckedChanged;
            this.Controls.Add(chkEnableEncryption);
            currentY += rowHeight;

            // Тип прокси
            var lblProxyType = new Label
            {
                Text = "Тип прокси:",
                ForeColor = Color.FromArgb(220, 220, 230),
                Location = new Point(startX, currentY + 3),
                Size = new Size(labelWidth, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(lblProxyType);

            cmbProxyType = new ComboBox
            {
                Location = new Point(startX + labelWidth, currentY),
                Size = new Size(inputWidth, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(50, 50, 55),
                ForeColor = Color.FromArgb(220, 220, 230),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            cmbProxyType.Items.AddRange(new[] { "SOCKS5", "SOCKS4", "HTTP", "HTTPS" });
            cmbProxyType.SelectedIndex = 0;
            this.Controls.Add(cmbProxyType);
            currentY += rowHeight;

            // Хост прокси
            var lblHost = new Label
            {
                Text = "Хост:",
                ForeColor = Color.FromArgb(220, 220, 230),
                Location = new Point(startX, currentY + 3),
                Size = new Size(labelWidth, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(lblHost);

            txtProxyHost = new TextBox
            {
                Location = new Point(startX + labelWidth, currentY),
                Size = new Size(inputWidth, 26),
                BackColor = Color.FromArgb(50, 50, 55),
                ForeColor = Color.FromArgb(220, 220, 230),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(txtProxyHost);

            // Порт прокси
            var lblPort = new Label
            {
                Text = "Порт:",
                ForeColor = Color.FromArgb(220, 220, 230),
                Location = new Point(startX + labelWidth + inputWidth + 15, currentY + 3),
                Size = new Size(50, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(lblPort);

            numProxyPort = new NumericUpDown
            {
                Location = new Point(startX + labelWidth + inputWidth + 65, currentY),
                Size = new Size(portWidth, 26),
                Minimum = 1,
                Maximum = 65535,
                Value = 1080,
                BackColor = Color.FromArgb(50, 50, 55),
                ForeColor = Color.FromArgb(220, 220, 230),
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(numProxyPort);
            currentY += rowHeight;

            // Требуется авторизация
            chkRequireAuth = new CheckBox
            {
                Text = "Требуется авторизация",
                ForeColor = Color.FromArgb(220, 220, 230),
                BackColor = Color.FromArgb(30, 30, 35),
                Location = new Point(startX, currentY),
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            // Кастомная отрисовка чекбокса для темной темы
            chkRequireAuth.Paint += (s, e) =>
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
                        using (var checkPen = new Pen(Color.FromArgb(30, 30, 35), 2))
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
            chkRequireAuth.CheckedChanged += ChkRequireAuth_CheckedChanged;
            this.Controls.Add(chkRequireAuth);
            currentY += rowHeight;

            // Имя пользователя
            var lblUsername = new Label
            {
                Text = "Имя пользователя:",
                ForeColor = Color.FromArgb(220, 220, 230),
                Location = new Point(startX, currentY + 3),
                Size = new Size(labelWidth, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(lblUsername);

            txtProxyUsername = new TextBox
            {
                Location = new Point(startX + labelWidth, currentY),
                Size = new Size(inputWidth, 26),
                BackColor = Color.FromArgb(50, 50, 55),
                ForeColor = Color.FromArgb(220, 220, 230),
                BorderStyle = BorderStyle.FixedSingle,
                Enabled = false,
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(txtProxyUsername);

            // Пароль
            var lblPassword = new Label
            {
                Text = "Пароль:",
                ForeColor = Color.FromArgb(220, 220, 230),
                Location = new Point(startX + labelWidth + inputWidth + 15, currentY + 3),
                Size = new Size(50, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(lblPassword);

            txtProxyPassword = new TextBox
            {
                Location = new Point(startX + labelWidth + inputWidth + 65, currentY),
                Size = new Size(portWidth, 26),
                BackColor = Color.FromArgb(50, 50, 55),
                ForeColor = Color.FromArgb(220, 220, 230),
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = '●',
                Enabled = false,
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(txtProxyPassword);
            currentY += rowHeight + 10;

            // Статус
            lblStatus = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(150, 200, 150),
                Location = new Point(startX, currentY),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(lblStatus);
            currentY += 25;

            // Все кнопки одного тона - темно-серый
            Color buttonColor = Color.FromArgb(50, 50, 58); // Единый цвет для всех кнопок
            
            // Кнопки расположены равномерно внизу формы
            int buttonSpacing = 15;
            int buttonWidth = 150;
            int totalButtonsWidth = buttonWidth * 3 + buttonSpacing * 2;
            int buttonsStartX = (this.Width - totalButtonsWidth) / 2;
            int buttonsY = currentY + 20;

            // Кнопка тестирования
            btnTestProxy = UIHelper.CreateStyledButton(
                "Тестировать подключение",
                new Point(buttonsStartX, buttonsY),
                buttonColor,
                buttonWidth
            );
            btnTestProxy.Font = new Font("Segoe UI", 9);
            btnTestProxy.Click += BtnTestProxy_Click;
            this.Controls.Add(btnTestProxy);

            // Кнопка сохранить
            btnSave = UIHelper.CreateStyledButton(
                "Сохранить",
                new Point(buttonsStartX + buttonWidth + buttonSpacing, buttonsY),
                buttonColor,
                buttonWidth
            );
            btnSave.Font = new Font("Segoe UI", 9);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            // Кнопка отмена
            btnCancel = UIHelper.CreateStyledButton(
                "Отмена",
                new Point(buttonsStartX + (buttonWidth + buttonSpacing) * 2, buttonsY),
                buttonColor,
                buttonWidth
            );
            btnCancel.Font = new Font("Segoe UI", 9);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnCancel);

            UpdateControlsState();
        }

        private void LoadCurrentSettings()
        {
            if (Settings != null)
            {
                cmbProxyType.SelectedItem = Settings.Type.ToString();
                txtProxyHost.Text = Settings.Host;
                numProxyPort.Value = Settings.Port;
                chkRequireAuth.Checked = Settings.RequireAuth;
                txtProxyUsername.Text = Settings.Username ?? "";
                txtProxyPassword.Text = Settings.Password ?? "";
                chkEnableEncryption.Checked = EncryptionEnabled;
            }
        }

        private void ChkEnableEncryption_CheckedChanged(object sender, EventArgs e)
        {
            UpdateControlsState();
        }

        private void ChkRequireAuth_CheckedChanged(object sender, EventArgs e)
        {
            txtProxyUsername.Enabled = chkRequireAuth.Checked;
            txtProxyPassword.Enabled = chkRequireAuth.Checked;
        }

        private void UpdateControlsState()
        {
            bool enabled = chkEnableEncryption.Checked;
            cmbProxyType.Enabled = enabled;
            txtProxyHost.Enabled = enabled;
            numProxyPort.Enabled = enabled;
            chkRequireAuth.Enabled = enabled;
            txtProxyUsername.Enabled = enabled && chkRequireAuth.Checked;
            txtProxyPassword.Enabled = enabled && chkRequireAuth.Checked;
            btnTestProxy.Enabled = enabled;
        }

        private async void BtnTestProxy_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
                return;

            btnTestProxy.Enabled = false;
            lblStatus.Text = "Проверяю подключение...";
            lblStatus.ForeColor = Color.FromArgb(0, 120, 215);

            try
            {
                var settings = GetSettingsFromUI();
                if (settings == null)
                {
                    lblStatus.Text = "✗ Ошибка: настройки не заполнены";
                    lblStatus.ForeColor = Color.FromArgb(196, 43, 28);
                    return;
                }
                
                var encryption = new TrafficEncryption();
                bool result = await encryption.TestProxyConnection(settings);

                if (result)
                {
                    lblStatus.Text = "✓ Подключение успешно!";
                    lblStatus.ForeColor = Color.FromArgb(150, 200, 150);
                }
                else
                {
                    lblStatus.Text = "✗ Не удалось подключиться";
                    lblStatus.ForeColor = Color.FromArgb(196, 43, 28);
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Ошибка: {ex.Message}";
                lblStatus.ForeColor = Color.FromArgb(196, 43, 28);
            }
            finally
            {
                btnTestProxy.Enabled = true;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
                return;

            Settings = GetSettingsFromUI();
            EncryptionEnabled = chkEnableEncryption.Checked;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private bool ValidateInput()
        {
            if (!chkEnableEncryption.Checked)
                return true; // Если шифрование отключено, валидация не нужна

            if (string.IsNullOrWhiteSpace(txtProxyHost.Text))
            {
                MessageBox.Show("Укажите хост прокси сервера", "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtProxyHost.Focus();
                return false;
            }

            if (chkRequireAuth.Checked)
            {
                if (string.IsNullOrWhiteSpace(txtProxyUsername.Text))
                {
                    MessageBox.Show("Укажите имя пользователя", "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtProxyUsername.Focus();
                    return false;
                }
            }

            return true;
        }

        private TrafficEncryption.ProxySettings GetSettingsFromUI()
        {
            if (!chkEnableEncryption.Checked || string.IsNullOrWhiteSpace(txtProxyHost.Text))
                return null;

            var typeStr = cmbProxyType.SelectedItem?.ToString() ?? "SOCKS5";
            Enum.TryParse<TrafficEncryption.ProxyType>(typeStr, out var type);

            return new TrafficEncryption.ProxySettings
            {
                Type = type,
                Host = txtProxyHost.Text.Trim(),
                Port = (int)numProxyPort.Value,
                Username = chkRequireAuth.Checked ? txtProxyUsername.Text.Trim() : null,
                Password = chkRequireAuth.Checked ? txtProxyPassword.Text : null,
                RequireAuth = chkRequireAuth.Checked,
                UseEncryption = true
            };
        }
    }
}

