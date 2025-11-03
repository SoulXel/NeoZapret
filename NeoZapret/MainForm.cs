using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.ServiceProcess;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace NeoZapret
{
    public partial class MainForm : Form
    {
        private Button btnStartBypass;
        private Button btnStopBypass;
        private Button btnServiceManage;
        private Button btnDiagnostics;
        private Button btnCleanFiles;
        private Button btnSettings;
        private Button btnStrategyGenerator;
        private Button btnUpdateLists;
        private Button btnCheckUpdates;
        private Button btnEncryption;
        private DarkScrollbarRichTextBox txtLog;
        private Label lblTitle;
        private Label lblSubtitle;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private CheckBox chkAutoStart;
        private Panel leftPanel;
        private Panel rightPanel;
        private ToolTip toolTip;

        private string appPath;
        private string binPath;
        private string listsPath;
        private Process currentProcess;
        private BypassMonitor bypassMonitor;
        private TrafficStatistics trafficStatistics;
        private TrafficEncryption trafficEncryption;
        private System.Windows.Forms.Timer statusUpdateTimer;
        private double lastReportedSuccessRate = -1; // Для отслеживания изменений статуса

        public MainForm()
        {
            InitializeComponent();
            InitializeTrayIcon();
            InitializePaths();
            InitializeToolTips();
            LoadSettings();
            SetupHotKeys();
            InitializeMonitoring();
            InitializeTrafficStatistics();
            CheckForUpdatesOnStartup();
            CheckListsUpdateOnStartup();
            
            // Инициализация новых функций
            InitializeConflictDetection();
            InitializeAutoStart();
            InitializeSmartUpdater();
            InitializeTrafficEncryption();
            InitializeProviderDetection();
        }

        private void InitializeMonitoring()
        {
            bypassMonitor = new BypassMonitor();
            bypassMonitor.StatusChanged += BypassMonitor_StatusChanged;
            bypassMonitor.BestStrategyFound += BypassMonitor_BestStrategyFound;
            
            statusUpdateTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000 // Обновление каждые 5 секунд
            };
            statusUpdateTimer.Tick += StatusUpdateTimer_Tick;
            statusUpdateTimer.Start();
        }

        private void InitializeTrafficStatistics()
        {
            try
            {
                trafficStatistics = new TrafficStatistics(appPath);
                Logger.Info("Статистика трафика инициализирована");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка инициализации статистики трафика", ex);
            }
        }

        private async void CheckForUpdatesOnStartup()
        {
            try
            {
                // Даем форме время загрузиться
                await System.Threading.Tasks.Task.Delay(2000);
                
                // Проверяем обновления в фоне при запуске (silent mode)
                var updateInfo = await UpdateChecker.CheckForUpdates(silent: true);
                
                if (updateInfo.IsUpdateAvailable && !this.IsDisposed && this.IsHandleCreated)
                {
                    // Показываем уведомление в статус-баре
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() =>
                        {
                            if (!this.IsDisposed && statusStrip != null)
                            {
                                var updateLabel = new ToolStripStatusLabel
                                {
                                    Text = $"⚠ Доступна версия {updateInfo.LatestVersion}",
                                    ForeColor = Color.FromArgb(255, 200, 70),
                                    IsLink = true
                                };
                                updateLabel.Click += (s, e) => BtnCheckUpdates_Click(btnCheckUpdates, EventArgs.Empty);
                                statusStrip.Items.Insert(0, updateLabel);
                            }
                        }));
                    }
                    else
                    {
                        var updateLabel = new ToolStripStatusLabel
                        {
                            Text = $"⚠ Доступна версия {updateInfo.LatestVersion}",
                            ForeColor = Color.FromArgb(255, 200, 70),
                            IsLink = true
                        };
                        updateLabel.Click += (s, e) => BtnCheckUpdates_Click(btnCheckUpdates, EventArgs.Empty);
                        statusStrip.Items.Insert(0, updateLabel);
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибки проверки при старте, но не блокируем запуск
                Logger.Warning($"Ошибка проверки обновлений при старте: {ex.Message}");
            }
        }

        private void CheckListsUpdateOnStartup()
        {
            try
            {
                if (string.IsNullOrEmpty(listsPath))
                    return;

                // Проверяем, нужно ли обновлять списки (старше 7 дней)
                if (ListUpdater.NeedsUpdate(listsPath, 7))
                {
                    Logger.Info("Списки доменов устарели, рекомендуется обновление");
                    
                    // Показываем уведомление в статус-баре
                    if (statusStrip != null && !this.IsDisposed)
                    {
                        var listsLabel = new ToolStripStatusLabel
                        {
                            Text = "Обновить списки",
                            ForeColor = Color.FromArgb(100, 200, 255),
                            IsLink = true
                        };
                        listsLabel.Click += (s, e) => BtnUpdateLists_Click(btnUpdateLists, EventArgs.Empty);
                        statusStrip.Items.Insert(0, listsLabel);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Ошибка проверки устаревания списков", ex);
            }
        }

        private void StatusUpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateStatus();
        }

        private void BypassMonitor_StatusChanged(object sender, BypassStatusChangedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<object, BypassStatusChangedEventArgs>(BypassMonitor_StatusChanged), sender, e);
                return;
            }
            
            // Выводим сообщение только при изменении статуса (изменении категории успешности)
            double currentCategory = GetSuccessCategory(e.SuccessRate);
            double lastCategory = GetSuccessCategory(lastReportedSuccessRate);
            
            if (Math.Abs(currentCategory - lastCategory) > 0.1) // Категория изменилась
            {
                if (e.SuccessRate >= 80)
                {
                    LogSuccess($"✓ Обход работает отлично: {e.SuccessCount}/{e.TotalCount} сайтов доступно ({e.SuccessRate:F1}%)");
                }
                else if (e.SuccessRate >= 50)
                {
                    LogWarning($"⚠ Обход работает частично: {e.SuccessCount}/{e.TotalCount} сайтов доступно ({e.SuccessRate:F1}%)");
                }
                else
                {
                    LogError($"✗ Проблемы с обходом: {e.SuccessCount}/{e.TotalCount} сайтов доступно ({e.SuccessRate:F1}%)");
                }
                
                lastReportedSuccessRate = e.SuccessRate;
            }

            // Обновляем статистику трафика
            try
            {
                trafficStatistics?.UpdateAvailability(e.SuccessCount, e.TotalCount, e.SuccessRate);
            }
            catch (Exception ex)
            {
                Logger.Warning("Ошибка обновления статистики трафика", ex);
            }
        }
        
        private double GetSuccessCategory(double successRate)
        {
            if (successRate >= 80) return 3; // Отлично
            if (successRate >= 50) return 2; // Частично
            return 1; // Проблемы
        }

        private async void BypassMonitor_BestStrategyFound(object sender, string bestStrategy)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<object, string>(BypassMonitor_BestStrategyFound), sender, bestStrategy);
                return;
            }
            
            var result = MessageBox.Show(
                $"Текущая стратегия работает неэффективно.\n\nРекомендуется переключиться на: {GetStrategyDisplayName(bestStrategy)}\n\nПереключить сейчас?",
                "NeoZapret - Рекомендация", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                StopBypass();
                await System.Threading.Tasks.Task.Delay(1000); // Асинхронная задержка без блокировки UI
                await StartBypass(bestStrategy, false); // Используем стандартное значение для игрового фильтра
            }
        }

        private void InitializeToolTips()
        {
            toolTip = new ToolTip
            {
                BackColor = Color.FromArgb(38, 38, 44),
                ForeColor = Color.FromArgb(220, 220, 230),
                IsBalloon = false,
                ToolTipTitle = "NeoZapret"
            };

            toolTip.SetToolTip(btnStartBypass, "Запустить обход блокировок (Ctrl+S)\nОткроется окно выбора стратегии");
            toolTip.SetToolTip(btnStopBypass, "Остановить текущий обход (Ctrl+T)");
            toolTip.SetToolTip(btnServiceManage, "Управление службой Windows\nУстановка и настройка автозапуска");
            toolTip.SetToolTip(btnDiagnostics, "Проверка состояния системы\nBFE, WinDivert, TCP timestamps");
            toolTip.SetToolTip(btnCleanFiles, "Очистка временных файлов и логов");
            toolTip.SetToolTip(btnSettings, "Дополнительные настройки\nDNS, прокси, параметры обхода");
            toolTip.SetToolTip(btnStrategyGenerator, "Генератор и тестирование стратегий\nСоздание и проверка кастомных стратегий");
            toolTip.SetToolTip(btnUpdateLists, "Обновление списков доменов и IP\nЗагрузка актуальных списков из интернета");
            toolTip.SetToolTip(btnCheckUpdates, "Проверка обновлений приложения\nПоиск новых версий на GitHub");
            toolTip.SetToolTip(chkAutoStart, "Автоматический запуск при загрузке Windows");
        }

        private void SetupHotKeys()
        {
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.S:
                        if (btnStartBypass.Enabled)
                            BtnStartBypass_Click(btnStartBypass, EventArgs.Empty);
                        e.Handled = true;
                        break;
                    case Keys.T:
                        if (btnStopBypass.Enabled)
                            BtnStopBypass_Click(btnStopBypass, EventArgs.Empty);
                        e.Handled = true;
                        break;
                    case Keys.D:
                        BtnDiagnostics_Click(btnDiagnostics, EventArgs.Empty);
                        e.Handled = true;
                        break;
                }
            }
        }

        private void InitializePaths()
        {
            try
            {
                var startupPath = Application.StartupPath;
                if (string.IsNullOrEmpty(startupPath))
                {
                    startupPath = AppDomain.CurrentDomain.BaseDirectory;
                }
                
                // Используем PathHelper для инициализации путей
                bool success = PathHelper.InitializePaths(startupPath, out appPath, out binPath, out listsPath);
                
                if (!success)
                {
                    LogWarning("Не удалось найти папки bin и lists. Используются пути по умолчанию.");
                    Logger.Warning($"Пути не найдены, startupPath: {startupPath}");
                }
                else
                {
                    LogInfo($"Пути инициализированы: appPath={appPath}, binPath={binPath}");
                    Logger.Info($"Пути успешно инициализированы: appPath={appPath}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Критическая ошибка инициализации путей: {ex.Message}");
                Logger.Error("Ошибка инициализации путей", ex);
                appPath = Environment.CurrentDirectory ?? ".";
                binPath = Path.Combine(appPath, "bin");
                listsPath = Path.Combine(appPath, "lists");
            }
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "NeoZapret - Обход блокировок РФ 2025";
            this.Size = new Size(900, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(32, 32, 38); // Темный фон

            // Градиентный фон
            this.Paint += MainForm_Paint;

            // Левый панель (логотип и информация) - темный стиль
            leftPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(300, 800),
                BackColor = Color.FromArgb(28, 28, 34)
            };
            leftPanel.Paint += (s, e) =>
            {
                var rect = leftPanel.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;
                
                // Темный градиентный фон
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    rect,
                    Color.FromArgb(28, 28, 34),
                    Color.FromArgb(24, 24, 30),
                    180f))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
                
                // Тонкая серая линия-разделитель
                using (var pen = new Pen(Color.FromArgb(60, 60, 70), 1))
                {
                    e.Graphics.DrawLine(pen, leftPanel.Width - 1, 0, leftPanel.Width - 1, leftPanel.Height);
                }
            };

            // Логотип и заголовок - полный текст, центрированный
            lblTitle = new Label
            {
                Text = "NEOZAPRET",
                Font = new Font("Segoe UI", 28, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 240, 245),
                AutoSize = false,
                Size = new Size(280, 45),
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblTitle.Location = new Point((leftPanel.Width - lblTitle.Width) / 2, 35);

            // Улучшенный подзаголовок - жирный и центрированный
            lblSubtitle = new Label
            {
                Text = "Обход блокировок РФ 2025",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 220, 230),
                AutoSize = false,
                Size = new Size(280, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblSubtitle.Location = new Point((leftPanel.Width - lblSubtitle.Width) / 2, 90);

            // Блок ALPHA удален по запросу пользователя

            // Панель с информацией о разработчике и тестеровщике - две симметричные колонки
            Panel infoContainerPanel = new Panel
            {
                Size = new Size(280, 200), // Уменьшена высота, так как убраны статусы
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None
            };
            infoContainerPanel.Location = new Point((leftPanel.Width - infoContainerPanel.Width) / 2, leftPanel.Height - infoContainerPanel.Height - 20);

            // === КОЛОНКА РАЗРАБОТЧИКА (слева) ===
            Panel developerPanel = new Panel
            {
                Size = new Size(135, 200),
                Location = new Point(0, 0),
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None
            };
            developerPanel.Paint += (s, e) =>
            {
                var rect = developerPanel.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;
                
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Темно-серый фон
                using (var brush = new SolidBrush(Color.FromArgb(45, 45, 52)))
                {
                    // Закругленные углы
                    using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        int radius = 12;
                        path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                        path.AddArc(rect.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                        path.AddArc(rect.Width - radius * 2, rect.Height - radius * 2, radius * 2, radius * 2, 0, 90);
                        path.AddArc(0, rect.Height - radius * 2, radius * 2, radius * 2, 90, 90);
                        path.CloseAllFigures();
                        e.Graphics.FillPath(brush, path);
                        
                        // Темная рамка
                        using (var borderPen = new Pen(Color.FromArgb(65, 65, 75), 1f))
                        {
                            e.Graphics.DrawPath(borderPen, path);
                        }
                    }
                }
            };

            Label lblDeveloperTitle = new Label
            {
                Text = "Разработчик",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 220, 230),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblDeveloperTitle.Location = new Point((developerPanel.Width - lblDeveloperTitle.Width) / 2, 18);

            Label lblGitHub = new Label
            {
                Text = "GitHub: soulxel",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(160, 160, 170),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblGitHub.Location = new Point((developerPanel.Width - lblGitHub.Width) / 2, 50);

            // Кликабельная ссылка на Telegram
            LinkLabel lblTelegram = new LinkLabel
            {
                Text = "Telegram: @xeldi",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 180, 255), // Светло-синий для ссылки
                ActiveLinkColor = Color.FromArgb(120, 200, 255),
                VisitedLinkColor = Color.FromArgb(100, 180, 255),
                LinkColor = Color.FromArgb(100, 180, 255),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            lblTelegram.Location = new Point((developerPanel.Width - lblTelegram.Width) / 2, 70);
            lblTelegram.LinkClicked += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start("https://t.me/xeldi");
                }
                catch { }
            };

            Label lblDiscord = new Label
            {
                Text = "Discord: Lu1ky",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(160, 160, 170),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblDiscord.Location = new Point((developerPanel.Width - lblDiscord.Width) / 2, 90);

            // Версия - четко и заметно внизу блока
            Label lblVersion = new Label
            {
                Text = "v3.1.0",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 220, 230),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblVersion.Location = new Point((developerPanel.Width - lblVersion.Width) / 2, 155);

            developerPanel.Controls.Add(lblDeveloperTitle);
            developerPanel.Controls.Add(lblGitHub);
            developerPanel.Controls.Add(lblTelegram);
            developerPanel.Controls.Add(lblDiscord);
            developerPanel.Controls.Add(lblVersion);

            // === КОЛОНКА ТЕСТЕРОВЩИКА (справа) ===
            Panel testerPanel = new Panel
            {
                Size = new Size(135, 200),
                Location = new Point(145, 0),
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None
            };
            testerPanel.Paint += (s, e) =>
            {
                var rect = testerPanel.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;
                
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Темно-серый фон (такой же как у разработчика)
                using (var brush = new SolidBrush(Color.FromArgb(45, 45, 52)))
                {
                    // Закругленные углы
                    using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        int radius = 12;
                        path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                        path.AddArc(rect.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                        path.AddArc(rect.Width - radius * 2, rect.Height - radius * 2, radius * 2, radius * 2, 0, 90);
                        path.AddArc(0, rect.Height - radius * 2, radius * 2, radius * 2, 90, 90);
                        path.CloseAllFigures();
                        e.Graphics.FillPath(brush, path);
                        
                        // Темная рамка
                        using (var borderPen = new Pen(Color.FromArgb(65, 65, 75), 1f))
                        {
                            e.Graphics.DrawPath(borderPen, path);
                        }
                    }
                }
            };

            Label lblTesterTitle = new Label
            {
                Text = "Тестровщик",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 220, 230),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblTesterTitle.Location = new Point((testerPanel.Width - lblTesterTitle.Width) / 2, 18);

            Label lblTesterName = new Label
            {
                Text = "Матвей Котов",
                Font = new Font("Segoe UI", 12, FontStyle.Bold), // Немного уменьшен размер для полного отображения
                ForeColor = Color.FromArgb(240, 240, 245),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };
            // Центрируем имя тестеровщика по вертикали для визуальной симметрии
            lblTesterName.Location = new Point((testerPanel.Width - lblTesterName.Width) / 2, 80);

            testerPanel.Controls.Add(lblTesterTitle);
            testerPanel.Controls.Add(lblTesterName);

            infoContainerPanel.Controls.Add(developerPanel);
            infoContainerPanel.Controls.Add(testerPanel);

            // Кнопка автозапуска - красивая карточка в темно-сером стиле
            Panel autoStartPanel = new Panel
            {
                Location = new Point((leftPanel.Width - 260) / 2, 160),
                Size = new Size(260, 55),
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None,
                Cursor = Cursors.Hand
            };
            
            // Скрытый чекбокс для функциональности (невидимый, только для логики)
            chkAutoStart = new CheckBox
            {
                Visible = false,
                AutoSize = false,
                Size = new Size(0, 0),
                Location = new Point(-1000, -1000)
            };
            chkAutoStart.CheckedChanged += ChkAutoStart_CheckedChanged;
            
            // Обновляем состояние при загрузке
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("NeoZapret");
                        chkAutoStart.Checked = value != null && value.ToString() == Application.ExecutablePath;
                    }
                }
            }
            catch { }
            
            bool autoStartHovered = false;
            autoStartPanel.MouseEnter += (s, e) => { autoStartHovered = true; autoStartPanel.Invalidate(); };
            autoStartPanel.MouseLeave += (s, e) => { autoStartHovered = false; autoStartPanel.Invalidate(); };
            autoStartPanel.Click += (s, e) => 
            { 
                chkAutoStart.Checked = !chkAutoStart.Checked; 
                autoStartPanel.Invalidate();
            };
            
            // Добавляем обработчик для обновления отрисовки при изменении состояния
            chkAutoStart.CheckedChanged += (s, e) => { autoStartPanel.Invalidate(); };
            
            // Единый Paint обработчик без белого квадрата чекбокса
            autoStartPanel.Paint += (s, e) =>
            {
                var rect = autoStartPanel.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;
                
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                Color panelColor = autoStartHovered 
                    ? Color.FromArgb(58, 58, 66) 
                    : Color.FromArgb(45, 45, 52);
                
                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    int radius = 10;
                    path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                    path.AddArc(rect.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                    path.AddArc(rect.Width - radius * 2, rect.Height - radius * 2, radius * 2, radius * 2, 0, 90);
                    path.AddArc(0, rect.Height - radius * 2, radius * 2, radius * 2, 90, 90);
                    path.CloseAllFigures();
                    
                    using (var brush = new SolidBrush(panelColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    
                    using (var pen = new Pen(Color.FromArgb(65, 65, 75), 1f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
                
                // Рисуем текст без эмодзи
                string text = chkAutoStart.Checked ? "✓ Запуск с Windows" : "Запуск с Windows";
                
                using (var textBrush = new SolidBrush(Color.FromArgb(230, 230, 240)))
                {
                    var textFormat = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    e.Graphics.DrawString(text, new Font("Segoe UI", 10, FontStyle.Regular), textBrush, rect, textFormat);
                }
            };
            
            // Добавляем скрытый чекбокс в форму (не в панель)
            this.Controls.Add(chkAutoStart);

            leftPanel.Controls.Add(lblTitle);
            leftPanel.Controls.Add(lblSubtitle);
            leftPanel.Controls.Add(autoStartPanel);
            leftPanel.Controls.Add(infoContainerPanel);

            // Правая панель (основной функционал)
            rightPanel = new Panel
            {
                Location = new Point(300, 0),
                Size = new Size(600, 800),
                BackColor = Color.Transparent
            };

            // Кнопки - улучшенный темно-серый дизайн
            Color buttonColor = Color.FromArgb(50, 50, 58); // Более темный серый
            
            btnStartBypass = CreateStyledButton("Запустить обход", new Point(30, 30), buttonColor, 540, false);
            btnStartBypass.Click += BtnStartBypass_Click;

            btnStopBypass = CreateStyledButton("Остановить обход", new Point(30, 90), buttonColor, 540, false);
            btnStopBypass.Click += BtnStopBypass_Click;

            btnServiceManage = CreateStyledButton("Управление службой", new Point(30, 150), buttonColor, 260, false);
            btnServiceManage.Click += BtnServiceManage_Click;

            btnDiagnostics = CreateStyledButton("Диагностика", new Point(310, 150), buttonColor, 260, false);
            btnDiagnostics.Click += BtnDiagnostics_Click;

            btnCleanFiles = CreateStyledButton("Очистка", new Point(30, 210), buttonColor, 260, false);
            btnCleanFiles.Click += BtnCleanFiles_Click;

            btnSettings = CreateStyledButton("Доп. настройки", new Point(310, 210), buttonColor, 260, false);
            btnSettings.Click += BtnSettings_Click;

            btnEncryption = CreateStyledButton("Шифрование трафика", new Point(30, 270), buttonColor, 260, false);
            btnEncryption.Click += BtnEncryptionSettings_Click;

            btnStrategyGenerator = CreateStyledButton("Генератор стратегий", new Point(310, 270), buttonColor, 260, false);
            btnStrategyGenerator.Click += BtnStrategyGenerator_Click;

            btnUpdateLists = CreateStyledButton("Обновить списки", new Point(30, 330), buttonColor, 260, false);
            btnUpdateLists.Click += BtnUpdateLists_Click;

            btnCheckUpdates = CreateStyledButton("Проверить обновления", new Point(310, 330), buttonColor, 260, false);
            btnCheckUpdates.Click += BtnCheckUpdates_Click;

            // Лог - расширенный до низа формы, не заходит за кнопки
            // Кнопки на Y=330, высота кнопки ~50, отступ 10, начало лога на Y=390
            // Форма 800px высотой, статус-бар ~30px, отступ снизу 5px
            // Доступная высота: 800 - 390 - 30 - 5 = 375px
            Panel logPanel = new Panel
            {
                Location = new Point(30, 390),
                Size = new Size(540, 375), // Точный расчет с учетом статус-бара
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None
            };
            logPanel.Paint += (s, e) =>
            {
                var rect = logPanel.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;
                
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Темно-серый фон
                using (var brush = new SolidBrush(Color.FromArgb(45, 45, 52)))
                {
                    // Закругленные углы
                    using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        int radius = 10;
                        path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                        path.AddArc(rect.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                        path.AddArc(rect.Width - radius * 2, rect.Height - radius * 2, radius * 2, radius * 2, 0, 90);
                        path.AddArc(0, rect.Height - radius * 2, radius * 2, radius * 2, 90, 90);
                        path.CloseAllFigures();
                        e.Graphics.FillPath(brush, path);
                        
                        // Темная рамка
                        using (var pen = new Pen(Color.FromArgb(65, 65, 75), 1f))
                        {
                            e.Graphics.DrawPath(pen, path);
                        }
                    }
                }
            };
            
            // Заголовок лога
            Label lblLogTitle = new Label
            {
                Text = "Журнал операций",
                Location = new Point(15, 10),
                Size = new Size(510, 25),
                ForeColor = Color.FromArgb(230, 230, 240),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            
            txtLog = new DarkScrollbarRichTextBox
            {
                Location = new Point(10, 35),
                Size = new Size(520, 330), // Высота = высота панели (375) - заголовок (25) - отступ (10) = 340, делаем 330 для безопасности
                BackColor = Color.FromArgb(32, 32, 38),
                ForeColor = Color.FromArgb(230, 230, 240),
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                DetectUrls = false,
                WordWrap = false
            };
            
            // Настраиваем отступы для лучшей видимости текста
            txtLog.Margin = new Padding(8, 8, 8, 20); // Увеличенный отступ внизу для полной видимости последней строки
            
            txtLog.Paint += (s, e) =>
            {
                var rect = txtLog.ClientRectangle;
                using (var pen = new Pen(Color.FromArgb(55, 55, 65), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
                }
            };
            
            logPanel.Controls.Add(lblLogTitle);
            logPanel.Controls.Add(txtLog);

            // Status bar - темно-серый стиль
            statusStrip = new StatusStrip
            {
                BackColor = Color.FromArgb(45, 45, 52),
                Dock = DockStyle.Bottom,
                RenderMode = ToolStripRenderMode.Professional
            };
            statusStrip.Paint += (s, e) =>
            {
                var rect = statusStrip.ClientRectangle;
                using (var brush = new SolidBrush(Color.FromArgb(45, 45, 52)))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
                // Верхняя граница
                using (var pen = new Pen(Color.FromArgb(65, 65, 75), 1))
                {
                    e.Graphics.DrawLine(pen, 0, 0, rect.Width, 0);
                }
            };
            
            statusLabel = new ToolStripStatusLabel
            {
                Text = "✓ Готов к работе",
                ForeColor = Color.FromArgb(120, 220, 120),
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };
            
            statusStrip.Items.Add(statusLabel);

            rightPanel.Controls.Add(btnStartBypass);
            rightPanel.Controls.Add(btnStopBypass);
            rightPanel.Controls.Add(btnServiceManage);
            rightPanel.Controls.Add(btnDiagnostics);
            rightPanel.Controls.Add(btnCleanFiles);
            rightPanel.Controls.Add(btnSettings);
            rightPanel.Controls.Add(btnEncryption);
            rightPanel.Controls.Add(btnStrategyGenerator);
            rightPanel.Controls.Add(btnUpdateLists);
            rightPanel.Controls.Add(btnCheckUpdates);
            rightPanel.Controls.Add(logPanel);

            this.Controls.Add(leftPanel);
            this.Controls.Add(rightPanel);
            this.Controls.Add(statusStrip);

            this.Resize += MainForm_Resize;
            UpdateStatus();
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            var rect = this.ClientRectangle;
            if (rect.Width <= 0 || rect.Height <= 0) return;
            
            // Темный градиентный фон
            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect, 
                Color.FromArgb(32, 32, 38), 
                Color.FromArgb(28, 28, 34), 
                135f))
            {
                e.Graphics.FillRectangle(brush, rect);
            }
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            
            // Стильное черное меню Windows 11
            trayMenu.BackColor = Color.FromArgb(32, 32, 32);
            trayMenu.ForeColor = Color.FromArgb(240, 240, 240);
            trayMenu.Font = new Font("Segoe UI", 10);
            trayMenu.RenderMode = ToolStripRenderMode.Professional;
            
            // Кастомный рендерер для красивого меню
            trayMenu.Renderer = new ModernMenuRenderer();
            
            var item1 = new ToolStripMenuItem("Открыть NeoZapret");
            item1.Click += TrayShow_Click;
            item1.Font = new Font("Segoe UI", 10);
            item1.ForeColor = Color.FromArgb(240, 240, 240);
            trayMenu.Items.Add(item1);
            
            var item2 = new ToolStripMenuItem("Запустить обход");
            item2.Click += TrayStart_Click;
            item2.Font = new Font("Segoe UI", 10);
            item2.ForeColor = Color.FromArgb(240, 240, 240);
            trayMenu.Items.Add(item2);
            
            var item3 = new ToolStripMenuItem("Остановить обход");
            item3.Click += TrayStop_Click;
            item3.Font = new Font("Segoe UI", 10);
            item3.ForeColor = Color.FromArgb(240, 240, 240);
            trayMenu.Items.Add(item3);
            
            trayMenu.Items.Add(new ToolStripSeparator());
            
            var item4 = new ToolStripMenuItem("Выход");
            item4.Click += TrayExit_Click;
            item4.Font = new Font("Segoe UI", 10);
            item4.ForeColor = Color.FromArgb(255, 100, 100);
            trayMenu.Items.Add(item4);

            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "NeoZapret",
                ContextMenuStrip = trayMenu
            };
            trayIcon.DoubleClick += TrayIcon_DoubleClick;
        }
        
        // Кастомный рендерер для красивого темного меню
        private class ModernMenuRenderer : ToolStripProfessionalRenderer
        {
            public ModernMenuRenderer() : base(new ModernColorTable()) { }
            
            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (e.Item.Selected)
                {
                    var rect = new Rectangle(0, 0, e.Item.Width, e.Item.Height);
                    using (var brush = new SolidBrush(Color.FromArgb(60, 60, 60)))
                    {
                        e.Graphics.FillRectangle(brush, rect);
                    }
                }
                else
                {
                    base.OnRenderMenuItemBackground(e);
                }
            }
            
            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                var rect = new Rectangle(0, 0, e.Item.Width, e.Item.Height);
                using (var pen = new Pen(Color.FromArgb(80, 80, 80), 1))
                {
                    e.Graphics.DrawLine(pen, 20, rect.Height / 2, rect.Width - 5, rect.Height / 2);
                }
            }
        }
        
        private class ModernColorTable : ProfessionalColorTable
        {
            public override Color MenuItemBorder => Color.FromArgb(60, 60, 60);
            public override Color MenuItemSelected => Color.FromArgb(60, 60, 60);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(60, 60, 60);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(60, 60, 60);
            public override Color MenuItemPressedGradientBegin => Color.FromArgb(80, 80, 80);
            public override Color MenuItemPressedGradientEnd => Color.FromArgb(80, 80, 80);
            public override Color ToolStripDropDownBackground => Color.FromArgb(32, 32, 32);
            public override Color ImageMarginGradientBegin => Color.FromArgb(32, 32, 32);
            public override Color ImageMarginGradientMiddle => Color.FromArgb(32, 32, 32);
            public override Color ImageMarginGradientEnd => Color.FromArgb(32, 32, 32);
            public override Color SeparatorDark => Color.FromArgb(80, 80, 80);
            public override Color SeparatorLight => Color.FromArgb(80, 80, 80);
        }

        private void LoadSettings()
        {
            try
            {
                chkAutoStart.Checked = IsAutoStartEnabled();
            }
            catch { }
        }

        private bool IsAutoStartEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                {
                    return key?.GetValue("NeoZapret") != null;
                }
            }
            catch
            {
                return false;
            }
        }

        private void InitializeConflictDetection()
        {
            // Проверяем конфликты при запуске (асинхронно, чтобы не блокировать UI) - fire and forget
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(3000); // Даем время на полную загрузку UI
                var conflicts = await ConflictDetector.DetectConflicts();
                
                if (conflicts.HasConflicts)
                {
                    this.Invoke(new Action(() =>
                    {
                        ShowConflictNotification(conflicts);
                    }));
                }
            });
        }

        private void InitializeAutoStart()
        {
            try
            {
                // Проверяем и исправляем автозапуск при старте
                AutoStartManager.VerifyAndFixAutoStart(Application.ExecutablePath);
                
                // Обновляем состояние чекбокса
                chkAutoStart.Checked = AutoStartManager.IsAutoStartEnabled();
            }
            catch (Exception ex)
            {
                Logger.Warning("Ошибка инициализации автозапуска", ex);
            }
        }

        private void InitializeSmartUpdater()
        {
            try
            {
                if (!string.IsNullOrEmpty(listsPath) && Directory.Exists(listsPath))
                {
                    SmartUpdater.Start(listsPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Ошибка запуска умного обновлятора", ex);
            }
        }

        private void ShowConflictNotification(ConflictDetector.ConflictResult conflicts)
        {
            try
            {
                var criticalConflicts = conflicts.Conflicts.Where(c => c.Severity == ConflictDetector.Severity.Critical).ToList();
                var warnings = conflicts.Conflicts.Where(c => c.Severity == ConflictDetector.Severity.Warning).ToList();

                if (criticalConflicts.Count > 0 || warnings.Count > 3)
                {
                    // Показываем форму с рекомендациями
                    using (var form = new ConflictRecommendationsForm(conflicts))
                    {
                        form.ShowDialog();
                    }
                }
                else if (conflicts.Recommendations.Any(r => r.Contains("VPN") || r.Contains("IP-блокировок")))
                {
                    // Показываем только уведомление о VPN
                    var message = string.Join("\n", conflicts.Recommendations.Where(r => r.Contains("VPN") || r.Contains("IP")));
                    MessageBox.Show(
                        $"💡 Важная информация:\n\n{message}\n\nДля получения подробных рекомендаций используйте кнопку 'Диагностика'.",
                        "NeoZapret - Рекомендации",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка отображения уведомления о конфликтах", ex);
            }
        }

        private void ToggleAutoStart(bool enable)
        {
            try
            {
                if (enable)
                {
                    AutoStartManager.EnableAutoStart(Application.ExecutablePath);
                    LogSuccess("✓ Автозапуск включен (через реестр и ярлык)");
                }
                else
                {
                    AutoStartManager.DisableAutoStart();
                    LogInfo("○ Автозапуск отключен");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка переключения автозапуска", ex);
                LogError($"Ошибка: {ex.Message}");
            }
        }

        private void ChkAutoStart_CheckedChanged(object sender, EventArgs e)
        {
            ToggleAutoStart(chkAutoStart.Checked);
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            // Всегда сворачиваем в трей при минимизации
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                trayIcon.Visible = true;
            }
            
            // Пересчитываем размер журнала при изменении размера формы
            try
            {
                if (txtLog != null && !txtLog.IsDisposed)
                {
                    txtLog.Invalidate();
                }
            }
            catch { }
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void TrayShow_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void TrayStart_Click(object sender, EventArgs e)
        {
            using (var form = new StrategySelectForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    StartBypass(form.SelectedStrategy, form.UseGameFilter);
                }
            }
        }

        private void TrayStop_Click(object sender, EventArgs e)
        {
            StopBypass();
        }

        private void TrayExit_Click(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

        private Button CreateStyledButton(string text, Point location, Color baseColor, int width, bool isPrimary = false)
        {
            var btn = new Button
            {
                Text = text,
                Location = location,
                Size = new Size(width, 50),
                FlatStyle = FlatStyle.Flat,
                BackColor = baseColor,
                ForeColor = Color.FromArgb(240, 240, 245),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 0;
            
            bool isHovered = false;
            bool isPressed = false;
            
            btn.MouseEnter += (s, e) => { isHovered = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { isHovered = false; btn.Invalidate(); };
            btn.MouseDown += (s, e) => { isPressed = true; btn.Invalidate(); };
            btn.MouseUp += (s, e) => { isPressed = false; btn.Invalidate(); };
            
            // Улучшенный темно-серый дизайн
            btn.Paint += (s, e) =>
            {
                var rect = btn.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;
                
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                
                // Закругленный путь
                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    int radius = 8;
                    path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                    path.AddArc(rect.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                    path.AddArc(rect.Width - radius * 2, rect.Height - radius * 2, radius * 2, radius * 2, 0, 90);
                    path.AddArc(0, rect.Height - radius * 2, radius * 2, radius * 2, 90, 90);
                    path.CloseAllFigures();
                    
                    // Определяем цвет кнопки в зависимости от состояния
                    Color btnColor;
                    if (isPressed)
                    {
                        btnColor = Color.FromArgb(
                            Math.Max(0, baseColor.R - 8), 
                            Math.Max(0, baseColor.G - 8), 
                            Math.Max(0, baseColor.B - 8));
                    }
                    else if (isHovered)
                    {
                        btnColor = Color.FromArgb(
                            Math.Min(255, baseColor.R + 8), 
                            Math.Min(255, baseColor.G + 8), 
                            Math.Min(255, baseColor.B + 8));
                    }
                    else
                    {
                        btnColor = baseColor;
                    }
                    
                    // Фон кнопки
                    using (var brush = new SolidBrush(btnColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    
                    // Тонкая темная рамка
                    using (var borderPen = new Pen(Color.FromArgb(80, 80, 90), 1f))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }
                    
                    btn.Region = new Region(path);
                    
                    // Текст
                    using (var textBrush = new SolidBrush(Color.FromArgb(240, 240, 245)))
                    {
                        var textFormat = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        };
                        e.Graphics.DrawString(text, btn.Font, textBrush, rect, textFormat);
                    }
                }
            };
            
            return btn;
        }

        private void Log(string message, Color color)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => Log(message, color)));
                return;
            }

            try
            {
                txtLog.SelectionStart = txtLog.TextLength;
                txtLog.SelectionLength = 0;
                txtLog.SelectionColor = color;
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                txtLog.SelectionColor = txtLog.ForeColor;
                
                // Убеждаемся, что текст виден полностью - прокручиваем вниз с запасом
                txtLog.SelectionStart = txtLog.TextLength;
                txtLog.ScrollToCaret();
                
                // Дополнительная прокрутка вниз для полной видимости последней строки
                try
                {
                    if (txtLog.IsHandleCreated && txtLog.Lines.Length > 0)
                    {
                        // Используем WinAPI для более надежной прокрутки вниз (увеличенное количество строк)
                        Win32.ScrollDown(txtLog, 8);
                        
                        // Дополнительно прокручиваем до конца документа несколько раз
                        for (int i = 0; i < 3; i++)
                        {
                            txtLog.SelectionStart = txtLog.TextLength;
                            txtLog.ScrollToCaret();
                        }
                        
                        // Финальная прокрутка для гарантированной видимости
                        Win32.SendMessage(txtLog.Handle, Win32.WM_VSCROLL, Win32.SB_LINEDOWN, 0);
                        Win32.SendMessage(txtLog.Handle, Win32.WM_VSCROLL, Win32.SB_LINEDOWN, 0);
                    }
                }
                catch
                {
                    // Если WinAPI не работает, используем стандартный метод
                    txtLog.SelectionStart = txtLog.TextLength;
                    txtLog.ScrollToCaret();
                }
            }
            catch
            {
                // Игнорируем ошибки логирования, чтобы не нарушать работу приложения
            }
        }

        private void LogError(string message)
        {
            Log(message, Color.FromArgb(196, 43, 28)); // Красный
            Logger.Error(message);
        }

        private void LogWarning(string message)
        {
            Log(message, Color.FromArgb(247, 99, 12)); // Оранжевый
            Logger.Warning(message);
        }

        private void LogInfo(string message)
        {
            Log(message, Color.FromArgb(0, 120, 215)); // Синий
            Logger.Info(message);
        }

        private void LogSuccess(string message)
        {
            Log(message, Color.FromArgb(150, 200, 150)); // Зеленый
            Logger.Success(message);
        }

        private async void BtnStartBypass_Click(object sender, EventArgs e)
        {
            try
            {
                // Проверка и инициализация путей
                if (string.IsNullOrEmpty(binPath))
                {
                    InitializePaths();
                }

                var winwsPath = Path.Combine(binPath, "winws.exe");
                if (!File.Exists(winwsPath))
                {
                    MessageBox.Show($"Файл winws.exe не найден!\n\nОжидаемый путь: {winwsPath}\n\nУбедитесь, что папка 'bin' находится рядом с приложением.", 
                        "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    LogError($"Файл winws.exe не найден: {winwsPath}");
                    return;
                }

                // Выбираем стратегию и игровой фильтр
            using (var form = new StrategySelectForm())
            {
                    if (form.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(form.SelectedStrategy))
                {
                    await StartBypass(form.SelectedStrategy, form.UseGameFilter);
                }
                    else
                    {
                        LogInfo("Выбор стратегии отменен");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при запуске обхода: {ex.Message}");
                MessageBox.Show($"Ошибка при запуске обхода:\n\n{ex.Message}\n\nДетали: {ex}", "NeoZapret", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStopBypass_Click(object sender, EventArgs e)
        {
            StopBypass();
        }

        private void StopBypass()
        {
            try
            {
                LogInfo("Останавливаю обход...");
                bypassMonitor?.StopMonitoring();
                trafficStatistics?.EndSession();
                StopOldProcesses();
                LogSuccess("✓ Обход остановлен");
                statusLabel.Text = "Остановлен";
                statusLabel.ForeColor = Color.FromArgb(247, 99, 12);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка остановки: {ex.Message}");
            }
        }

        private async Task StartBypass(string strategy, bool useGameFilter = false)
        {
            try
            {
                // Проверка на null
                if (string.IsNullOrWhiteSpace(strategy))
                {
                    LogError("Ошибка: стратегия не выбрана!");
                    MessageBox.Show("Пожалуйста, выберите стратегию обхода!", "NeoZapret", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Проверка и инициализация путей
                if (string.IsNullOrEmpty(appPath) || string.IsNullOrEmpty(binPath) || string.IsNullOrEmpty(listsPath))
                {
                    InitializePaths();
                    
                    if (string.IsNullOrEmpty(appPath))
                    {
                        LogError("Ошибка: не удалось определить путь приложения!");
                        MessageBox.Show("Критическая ошибка: не удалось определить путь приложения!", "NeoZapret", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                LogInfo($"Запускаю стратегию: {strategy}...");

                StopOldProcesses();

                // Проверка существования файла winws.exe
                var winwsPath = Path.Combine(binPath, "winws.exe");
                if (string.IsNullOrEmpty(winwsPath) || !File.Exists(winwsPath))
                {
                    LogError($"Файл не найден: {winwsPath ?? "null"}");
                    LogError("Убедитесь, что папка 'bin' с файлом winws.exe находится рядом с приложением!");
                    MessageBox.Show($"Файл winws.exe не найден!\n\nОжидаемый путь: {winwsPath}\n\nУбедитесь, что папка 'bin' находится рядом с приложением.", 
                        "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    statusLabel.Text = "Ошибка: файл не найден";
                    statusLabel.ForeColor = Color.Red;
                    return;
                }

                // Проверка и инициализация путей (если еще не инициализированы)
                if (string.IsNullOrEmpty(appPath) || string.IsNullOrEmpty(binPath) || string.IsNullOrEmpty(listsPath))
                {
                    InitializePaths();
                }

                // Преобразуем bool в string для gameFilter
                string gameFilterStr = useGameFilter ? "1024-65535" : "12";
                string args;
                
                // Проверяем, это кастомная стратегия (начинается с "custom:" или путь к файлу)?
                if (strategy.StartsWith("custom:") || strategy.Contains("\\") || strategy.Contains("/"))
                {
                    // Это кастомная стратегия
                    if (strategy.StartsWith("custom:"))
                    {
                        args = strategy.Substring(7); // Убираем префикс "custom:"
                        if (string.IsNullOrWhiteSpace(args))
                        {
                            LogError("Ошибка: кастомная стратегия пуста!");
                            return;
                        }
                    }
                    else
                    {
                        // Загружаем из файла
                        var filePath = strategy;
                        if (!Path.IsPathRooted(filePath))
                        {
                            var strategiesPath = Path.Combine(appPath, "strategies");
                            filePath = Path.Combine(strategiesPath, filePath);
                        }
                        
                        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                        {
                            LogError($"Файл стратегии не найден: {filePath ?? "null"}");
                            MessageBox.Show($"Файл стратегии не найден!\n\nПуть: {filePath}", "NeoZapret", 
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        
                        args = File.ReadAllText(filePath);
                        if (string.IsNullOrWhiteSpace(args))
                        {
                            LogError("Ошибка: файл стратегии пуст!");
                            MessageBox.Show("Файл стратегии пуст или содержит недопустимые данные!", "NeoZapret", 
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    
                    // Заменяем относительные пути на абсолютные (с проверкой на null)
                    if (!string.IsNullOrEmpty(binPath))
                        args = args.Replace("bin\\", $"{binPath}\\").Replace("bin/", $"{binPath}/");
                    if (!string.IsNullOrEmpty(listsPath))
                        args = args.Replace("lists\\", $"{listsPath}\\").Replace("lists/", $"{listsPath}/");
                }
                else
                {
                    // Стандартная стратегия - используем централизованный генератор
                    if (string.IsNullOrEmpty(binPath) || string.IsNullOrEmpty(listsPath))
                    {
                        InitializePaths();
                    }
                    
                    args = StrategyArgumentsGenerator.GenerateBypassArguments(strategy, gameFilterStr, binPath, listsPath);
                    if (string.IsNullOrWhiteSpace(args))
                    {
                        LogError($"Ошибка: не удалось сгенерировать аргументы для стратегии '{strategy}'!");
                        Logger.Error($"Не удалось сгенерировать аргументы. Стратегия: {strategy}, binPath: {binPath}, listsPath: {listsPath}");
                        MessageBox.Show($"Ошибка: не удалось сгенерировать аргументы для стратегии '{strategy}'!\n\n" +
                            "Проверьте:\n" +
                            $"1. Путь bin: {binPath}\n" +
                            $"2. Путь lists: {listsPath}\n" +
                            "3. Логи приложения для подробностей",
                            "NeoZapret - Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                // Проверка рабочей директории
                if (string.IsNullOrEmpty(binPath))
                {
                    LogError("Ошибка: путь bin не определен!");
                    MessageBox.Show("Ошибка: не удалось определить путь к папке bin!", "NeoZapret", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!Directory.Exists(binPath))
                {
                    LogError($"Папка bin не найдена: {binPath}");
                    MessageBox.Show($"Папка 'bin' не найдена!\n\nПуть: {binPath}\n\nУбедитесь, что папка 'bin' находится рядом с приложением.", "NeoZapret", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Проверка аргументов перед запуском
                if (string.IsNullOrWhiteSpace(args))
                {
                    LogError("Ошибка: аргументы запуска пусты!");
                    MessageBox.Show("Ошибка: не удалось сформировать аргументы для запуска обхода!", "NeoZapret", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Проверка длины аргументов (Windows имеет ограничение ~8191 символ)
                if (args.Length > 8000)
                {
                    LogWarning($"⚠ Длина аргументов очень большая: {args.Length} символов");
                    LogWarning("Возможно, некоторые файлы или пути слишком длинные");
                }

                // Проверка наличия необходимых файлов в аргументах
                try
                {
                    LogInfo("Проверка наличия файлов из аргументов...");
                    var requiredFiles = new[]
                    {
                        "quic_initial_www_google_com.bin",
                        "tls_clienthello_4pda_to.bin",
                        "tls_clienthello_www_google_com.bin"
                    };

                    foreach (var file in requiredFiles)
                    {
                        var filePath = Path.Combine(binPath, file);
                        if (!File.Exists(filePath))
                        {
                            LogError($"✗ Не найден файл: {file}");
                            MessageBox.Show($"Критическая ошибка: не найден файл!\n\n" +
                                $"Файл: {file}\n" +
                                $"Ожидаемый путь: {filePath}\n\n" +
                                $"Убедитесь, что все необходимые файлы находятся в папке bin.",
                                "NeoZapret - Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    // Проверка файлов списков
                    var listFiles = new[]
                    {
                        "list-general.txt",
                        "list-exclude.txt",
                        "list-google.txt",
                        "ipset-exclude.txt",
                        "ipset-all.txt"
                    };

                    var missingLists = new List<string>();
                    foreach (var listFile in listFiles)
                    {
                        var listPath = Path.Combine(listsPath, listFile);
                        if (!File.Exists(listPath))
                        {
                            missingLists.Add(listFile);
                            LogWarning($"⚠ Не найден файл списка: {listFile}");
                        }
                    }

                    if (missingLists.Count > 0)
                    {
                        LogWarning($"⚠ Отсутствуют файлы списков ({missingLists.Count}): {string.Join(", ", missingLists)}");
                        LogWarning("Попробуйте обновить списки через меню или вручную");
                        // Не блокируем запуск, но предупреждаем
                    }

                    LogSuccess("✓ Все необходимые файлы найдены");
                }
                catch (Exception fileCheckEx)
                {
                    LogError($"Ошибка при проверке файлов: {fileCheckEx.Message}");
                    // Продолжаем выполнение, но логируем ошибку
                }

                try
                {
                    // Логируем аргументы для диагностики
                    Logger.Info($"Запуск стратегии: {strategy}");
                    LogInfo($"Длина аргументов: {args.Length} символов");
                    Logger.Debug($"Аргументы (первые 500 символов): {args.Substring(0, Math.Min(500, args.Length))}...");
                    
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = winwsPath,
                            Arguments = args,
                            WorkingDirectory = binPath,
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        }
                    };

                    // Собираем ошибки для диагностики
                    var errorOutput = new System.Text.StringBuilder();
                    var standardOutput = new System.Text.StringBuilder();
                    
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            errorOutput.AppendLine(e.Data);
                            Logger.Error($"winws.exe stderr: {e.Data}");
                        }
                    };
                    
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            standardOutput.AppendLine(e.Data);
                            Logger.Debug($"winws.exe stdout: {e.Data}");
                        }
                    };

                    try
                    {
                        process.Start();
                        process.BeginErrorReadLine();
                        process.BeginOutputReadLine();
                        
                        // Даем процессу время на запуск и проверяем, не завершился ли он сразу
                        // Ждем дольше и проверяем несколько раз для надежности
                        for (int check = 0; check < 3; check++)
                        {
                            await System.Threading.Tasks.Task.Delay(1500);
                            
                            // Проверяем, что процесс запустился (не завершился сразу)
                            try
                            {
                                if (process.HasExited)
                                {
                                    // Даем время на завершение записи ошибок
                                    await System.Threading.Tasks.Task.Delay(500);
                                    
                                    var exitCode = process.ExitCode;
                                    var errors = errorOutput.ToString();
                                    var output = standardOutput.ToString();
                                    
                                    LogError($"Процесс завершился с ошибкой. Код выхода: {exitCode}");
                                    
                                    // Логируем детали
                                    if (!string.IsNullOrEmpty(errors))
                                    {
                                        LogError($"Ошибки winws.exe:\n{errors.Trim()}");
                                    }
                                    else
                                    {
                                        LogWarning("⚠ winws.exe не вывел ошибок в stderr");
                                    }
                                    
                                    if (!string.IsNullOrEmpty(output))
                                    {
                                        LogInfo($"Вывод winws.exe:\n{output.Trim()}");
                                    }
                                    
                                    // Проверяем аргументы на наличие потенциальных проблем
                                    var argsPreview = args.Length > 1000 ? args.Substring(0, 1000) + "..." : args;
                                    LogInfo($"Аргументы (первые 1000 символов):\n{argsPreview}");
                                    
                                    var errorMessage = $"Процесс обхода завершился с ошибкой!\n\n" +
                                        $"Код выхода: {exitCode}\n" +
                                        $"Стратегия: {GetStrategyDisplayName(strategy)}\n" +
                                        $"Длина аргументов: {args.Length} символов\n\n";
                                    
                                    if (!string.IsNullOrEmpty(errors))
                                    {
                                        var shortErrors = errors.Length > 500 ? errors.Substring(0, 500) + "..." : errors;
                                        errorMessage += $"Ошибки:\n{shortErrors.Trim()}\n\n";
                                    }
                                    else
                                    {
                                        errorMessage += "winws.exe не вывел сообщений об ошибках.\n\n";
                                    }
                                    
                                    errorMessage += "Возможные причины:\n" +
                                        "1. Некорректные аргументы командной строки\n" +
                                        "2. Отсутствуют необходимые файлы\n" +
                                        "3. Нет прав администратора (для WinDivert)\n" +
                                        "4. Конфликт с антивирусом или VPN\n" +
                                        "5. Повреждены файлы в папке bin\n\n" +
                                        "Проверьте логи приложения для подробностей.";
                                    
                                    MessageBox.Show(errorMessage, "NeoZapret - Ошибка запуска", 
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    
                                    process.Dispose();
                                    return;
                                }
                                else
                                {
                                    // Процесс работает, выходим из цикла проверки
                                    break;
                                }
                            }
                            catch (Exception checkEx)
                            {
                                // Процесс может быть недоступен, но это нормально если он работает
                                Logger.Debug($"Проверка процесса (попытка {check + 1}): {checkEx.Message}");
                            }
                        }
                    }
                    catch (Exception procEx)
                    {
                        LogError($"Ошибка при запуске процесса: {procEx.Message}");
                        MessageBox.Show($"Не удалось запустить процесс обхода!\n\nОшибка: {procEx.Message}", 
                            "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        process.Dispose();
                        return;
                    }
                    
                    // Сохраняем ссылку на процесс
                    currentProcess = process;
                    
                LogSuccess($"Обход успешно запущен! Стратегия: {GetStrategyDisplayName(strategy)}");
                statusLabel.Text = $"Работает: {GetStrategyDisplayName(strategy)}";
                statusLabel.ForeColor = Color.FromArgb(16, 124, 16);
                
                // Сбрасываем последний статус при запуске нового обхода
                lastReportedSuccessRate = -1;
                
                // Начинаем новую сессию статистики
                trafficStatistics?.StartSession(strategy);
                
                // Запускаем мониторинг эффективности
                bypassMonitor.StartMonitoring(strategy);
                }
                catch (System.ComponentModel.Win32Exception winEx)
                {
                    LogError($"Ошибка запуска процесса: {winEx.Message}");
                    if (winEx.NativeErrorCode == 2)
                    {
                        MessageBox.Show($"Файл не найден или нет прав на выполнение!\n\nПуть: {winwsPath}\n\nОшибка: {winEx.Message}", 
                            "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка запуска процесса:\n\n{winEx.Message}", "NeoZapret", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    throw;
                }
            }
            catch (ArgumentNullException ex)
            {
                LogError($"Ошибка: параметр не может быть null. {ex.ParamName}");
                MessageBox.Show($"Ошибка: параметр '{ex.ParamName}' не может быть null.\n\nДетали: {ex.Message}", 
                    "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Ошибка запуска";
                statusLabel.ForeColor = Color.Red;
            }
            catch (Exception ex)
            {
                LogError($"Ошибка запуска: {ex.Message}");
                MessageBox.Show($"Ошибка при запуске обхода:\n\n{ex.Message}\n\nТип ошибки: {ex.GetType().Name}", 
                    "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Ошибка запуска";
                statusLabel.ForeColor = Color.Red;
            }
        }
        
        private void StartBypassCustom(string customArgs)
        {
            try
            {
                // Проверка на null
                if (string.IsNullOrWhiteSpace(customArgs))
                {
                    LogError("Ошибка: кастомная стратегия пуста!");
                    MessageBox.Show("Кастомная стратегия пуста или содержит недопустимые данные!", "NeoZapret", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Проверка и инициализация путей
                if (string.IsNullOrEmpty(appPath) || string.IsNullOrEmpty(binPath) || string.IsNullOrEmpty(listsPath))
                {
                    InitializePaths();
                    
                    if (string.IsNullOrEmpty(appPath))
                    {
                        LogError("Ошибка: не удалось определить путь приложения!");
                        MessageBox.Show("Критическая ошибка: не удалось определить путь приложения!", "NeoZapret", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                LogInfo("Запускаю кастомную стратегию...");

                StopOldProcesses();

                // Проверка существования файла winws.exe
                var winwsPath = Path.Combine(binPath, "winws.exe");
                if (string.IsNullOrEmpty(winwsPath) || !File.Exists(winwsPath))
                {
                    LogError($"Файл не найден: {winwsPath ?? "null"}");
                    MessageBox.Show($"Файл winws.exe не найден!\n\nОжидаемый путь: {winwsPath}\n\nУбедитесь, что папка 'bin' находится рядом с приложением.", 
                        "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    statusLabel.Text = "Ошибка: файл не найден";
                    statusLabel.ForeColor = Color.Red;
                    return;
                }

                // Заменяем относительные пути на абсолютные (с проверкой на null)
                string args = customArgs;
                if (!string.IsNullOrEmpty(binPath))
                    args = args.Replace("bin\\", $"{binPath}\\").Replace("bin/", $"{binPath}/");
                if (!string.IsNullOrEmpty(listsPath))
                    args = args.Replace("lists\\", $"{listsPath}\\").Replace("lists/", $"{listsPath}/");

                // Проверка рабочей директории
                if (string.IsNullOrEmpty(binPath) || !Directory.Exists(binPath))
                {
                    LogError($"Папка bin не найдена: {binPath ?? "null"}");
                    MessageBox.Show($"Папка 'bin' не найдена!\n\nПуть: {binPath}", "NeoZapret", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Логируем аргументы для диагностики
                Logger.Info("Запуск winws.exe с кастомными аргументами");
                Logger.Debug($"Полная команда: {winwsPath} {args}");
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = winwsPath,
                        Arguments = args,
                        WorkingDirectory = binPath,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                // Асинхронное чтение ошибок для диагностики
                var errorBuilder = new System.Text.StringBuilder();
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        errorBuilder.AppendLine(e.Data);
                        Logger.Error($"winws.exe stderr: {e.Data}");
                    }
                };

                process.Start();
                process.BeginErrorReadLine();
                
                // Сохраняем ссылку на процесс
                currentProcess = process;
                
                LogSuccess("Обход успешно запущен! Кастомная стратегия применена");
                statusLabel.Text = "Работает: Кастомная стратегия";
                statusLabel.ForeColor = Color.FromArgb(16, 124, 16);
            }
            catch (ArgumentNullException ex)
            {
                LogError($"Ошибка: параметр не может быть null. {ex.ParamName}");
                MessageBox.Show($"Ошибка: параметр '{ex.ParamName}' не может быть null.\n\nДетали: {ex.Message}", 
                    "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Ошибка запуска";
                statusLabel.ForeColor = Color.Red;
            }
            catch (Exception ex)
            {
                LogError($"Ошибка запуска: {ex.Message}");
                MessageBox.Show($"Ошибка при запуске обхода:\n\n{ex.Message}\n\nТип ошибки: {ex.GetType().Name}", 
                    "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Ошибка запуска";
                statusLabel.ForeColor = Color.Red;
            }
        }

        private string LoadGameFilter()
        {
            try
            {
                if (string.IsNullOrEmpty(binPath))
                {
                    InitializePaths();
                }
                
                if (string.IsNullOrEmpty(binPath))
                {
                    return "12"; // Значение по умолчанию
                }
                
            var flagFile = Path.Combine(binPath, "game_filter.enabled");
            return File.Exists(flagFile) ? "1024-65535" : "12";
            }
            catch
            {
                return "12"; // Значение по умолчанию при ошибке
            }
        }

        private string GenerateBypassArguments(string strategy, string gameFilter)
        {
            // Проверка путей перед использованием
            if (string.IsNullOrEmpty(binPath) || string.IsNullOrEmpty(listsPath))
            {
                InitializePaths();
            }

            if (string.IsNullOrEmpty(binPath) || string.IsNullOrEmpty(listsPath))
            {
                LogError("Ошибка: пути bin или lists не определены!");
                Logger.Error("Пути bin или lists не определены при генерации аргументов");
                return "";
            }

            // Используем централизованный генератор аргументов
            var args = StrategyArgumentsGenerator.GenerateBypassArguments(strategy, gameFilter, binPath, listsPath);
            
            if (string.IsNullOrEmpty(args))
            {
                LogError($"Ошибка: не удалось сгенерировать аргументы для стратегии '{strategy}'!");
                Logger.Error($"Не удалось сгенерировать аргументы для стратегии: {strategy}");
            }
            else
            {
                Logger.Info($"Аргументы успешно сгенерированы для стратегии: {strategy}");
            }
            
            return args;
        }

        private void StopOldProcesses()
        {
            try
            {
                // Останавливаем сохраненный процесс
                if (currentProcess != null && !currentProcess.HasExited)
                {
                    try
                    {
                        currentProcess.Kill();
                        currentProcess.WaitForExit(1000);
                    }
                    catch { }
                    finally
                    {
                        currentProcess.Dispose();
                        currentProcess = null;
                    }
                }

                // Останавливаем все процессы winws
                var processes = Process.GetProcessesByName("winws");
                foreach (var process in processes)
                {
                    try
                    {
                        if (process != null && !process.HasExited)
                        {
                            process.Kill();
                            process.WaitForExit(1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Ошибка остановки процесса winws (PID: {process?.Id}): {ex.Message}");
                    }
                    finally
                    {
                        process?.Dispose();
                    }
                }
            }
            catch { }
        }

        private void BtnServiceManage_Click(object sender, EventArgs e)
        {
            using (var form = new ServiceManageForm())
            {
                form.ShowDialog();
            }
        }

        private async void BtnDiagnostics_Click(object sender, EventArgs e)
        {
            LogInfo("Запуск расширенной диагностики системы...");
            await RunDiagnosticsAsync();
        }

        private async Task RunDiagnosticsAsync()
        {
            int success = 0, warnings = 0, errors = 0;
            var fixableIssues = new List<string>();

            LogInfo("════════════════════════════════════════");
            LogInfo("🔍 ЗАПУСК РАСШИРЕННОЙ ДИАГНОСТИКИ");
            LogInfo("════════════════════════════════════════");
            LogInfo("");

            // Проверка прав администратора
            try
            {
                bool isAdmin = IsAdministrator();
                if (isAdmin)
                {
                    LogSuccess("✓ Приложение запущено с правами администратора");
                    success++;
                }
                else
                {
                    LogWarning("⚠ Приложение запущено без прав администратора (некоторые функции могут не работать)");
                    warnings++;
                }
            }
            catch
            {
                LogWarning("⚠ Не удалось проверить права администратора");
                warnings++;
            }

            // Проверка интернет-соединения
            try
            {
                LogInfo("Проверка интернет-соединения...");
                using (var ping = new System.Net.NetworkInformation.Ping())
                {
                    var reply = await ping.SendPingAsync("8.8.8.8", 3000);
                    if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                    {
                        LogSuccess($"✓ Интернет-соединение активно (ping: {reply.RoundtripTime}мс)");
                        success++;
                    }
                    else
                    {
                        LogWarning($"⚠ Проблемы с интернет-соединением: {reply.Status}");
                        warnings++;
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning($"⚠ Не удалось проверить интернет-соединение: {ex.Message}");
                warnings++;
            }

            // Проверка сетевых адаптеров
            try
            {
                var adapters = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                var activeAdapters = adapters.Where(a => 
                    a.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                    a.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback).ToList();
                
                if (activeAdapters.Count > 0)
                {
                    LogSuccess($"✓ Обнаружено активных сетевых адаптеров: {activeAdapters.Count}");
                    success++;
                }
                else
                {
                    LogError("✗ Нет активных сетевых адаптеров");
                    errors++;
                }
            }
            catch (Exception ex)
            {
                LogWarning($"⚠ Ошибка проверки сетевых адаптеров: {ex.Message}");
                warnings++;
            }

            // BFE
            try
            {
                var service = new ServiceController("BFE");
                if (service.Status == ServiceControllerStatus.Running)
                {
                    LogSuccess("✓ Base Filtering Engine работает");
                    success++;
                }
                else
                {
                    LogError("✗ Base Filtering Engine не запущен");
                    fixableIssues.Add("BFE");
                    errors++;
                }
            }
            catch
            {
                LogError("✗ Base Filtering Engine недоступен");
                errors++;
            }

            // WinDivert
            try
            {
                var service = new ServiceController("WinDivert");
                LogWarning("⚠ Обнаружена служба WinDivert");
                warnings++;
            }
            catch
            {
                LogSuccess("✓ Конфликтов WinDivert не обнаружено");
                success++;
            }

            // TCP Timestamps
            var tcpCheckResult = await CheckTcpTimestampsAsync();
            if (tcpCheckResult.IsOk)
            {
                LogSuccess("✓ TCP timestamps успешно включены");
                success++;
            }
            else
            {
                if (tcpCheckResult.IsDisabled)
                {
                    LogWarning("⚠ TCP timestamps отключены");
                    warnings++;
                    fixableIssues.Add("TCPTimestamps");
                }
                else
                {
                    LogWarning("⚠ TCP timestamps имеют неоптимальные настройки");
                    warnings++;
                }
            }

            // Проверка файлов (расширенная)
            var fileCheck = DiagnosticsAutoFix.CheckRequiredFiles(binPath);
            if (fileCheck.Success)
            {
                LogSuccess("✓ Все необходимые файлы присутствуют");
                
                // Дополнительная проверка размера и целостности файлов
                try
                {
                    var winwsPath = Path.Combine(binPath, "winws.exe");
                    if (File.Exists(winwsPath))
                    {
                        var fileInfo = new FileInfo(winwsPath);
                        if (fileInfo.Length == 0)
                        {
                            LogError("✗ Файл winws.exe пустой (0 байт)");
                            errors++;
                            fixableIssues.Add("WinwsCorrupted");
                        }
                        else if (fileInfo.Length < 1000)
                        {
                            LogWarning($"⚠ Файл winws.exe имеет подозрительно маленький размер: {fileInfo.Length} байт");
                            warnings++;
                        }
                        else
                        {
                            LogSuccess($"✓ winws.exe в порядке (размер: {fileInfo.Length / 1024} KB)");
                            success++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"⚠ Ошибка проверки целостности файлов: {ex.Message}");
                    warnings++;
                }
            }
            else
            {
                LogError($"✗ {fileCheck.Message}");
                errors++;
                fixableIssues.Add("MissingFiles");
            }

            // Проверка прав доступа к bin (расширенная)
            try
            {
                if (!string.IsNullOrEmpty(binPath) && Directory.Exists(binPath))
                {
                    var winwsPath = Path.Combine(binPath, "winws.exe");
                    if (File.Exists(winwsPath))
                    {
                        var fileInfo = new FileInfo(winwsPath);
                        bool hasIssues = false;
                        
                        if ((fileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            LogWarning("⚠ Файл winws.exe имеет атрибут ReadOnly");
                            warnings++;
                            fixableIssues.Add("WinwsReadOnly");
                            hasIssues = true;
                        }
                        
                        // Проверяем права на выполнение
                        try
                        {
                            // Пытаемся открыть файл на чтение и запись
                            using (var stream = fileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                            {
                                // Файл доступен
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            LogWarning("⚠ Нет прав на запись в файл winws.exe");
                            warnings++;
                            fixableIssues.Add("WinwsPermissions");
                            hasIssues = true;
                        }
                        
                        if (!hasIssues)
                        {
                            LogSuccess("✓ Права доступа к папке bin в порядке");
                            success++;
                        }
                    }
                    else
                    {
                        LogWarning("⚠ Файл winws.exe не найден");
                        warnings++;
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning($"⚠ Ошибка проверки прав доступа: {ex.Message}");
                warnings++;
            }

            // Проверка процессов winws (расширенная)
            try
            {
                var bypassProcesses = Process.GetProcessesByName("winws");
                if (bypassProcesses.Length > 0)
                {
                    var process = bypassProcesses[0];
                    try
                    {
                        var memoryMB = process.WorkingSet64 / 1024 / 1024;
                        var uptime = DateTime.Now - process.StartTime;
                        LogSuccess($"✓ Процесс обхода работает (PID: {process.Id}, память: {memoryMB}MB, работает: {uptime.TotalMinutes:F1} мин)");
                        success++;
                    }
                    catch
                    {
                        LogSuccess($"✓ Процесс обхода работает (PID: {process.Id})");
                        success++;
                    }
                    
                    // Освобождаем процессы
                    foreach (var p in bypassProcesses)
                    {
                        try { p.Dispose(); } catch { }
                    }
                }
                else
                {
                    LogWarning("○ Процесс обхода не запущен (это нормально, если обход не активен)");
                    // Не считаем это ошибкой, если пользователь просто не запустил обход
                }
            }
            catch (Exception ex)
            {
                LogWarning($"⚠ Ошибка проверки процессов: {ex.Message}");
                warnings++;
            }

            // Проверка списков блокировок
            if (!string.IsNullOrEmpty(listsPath) && Directory.Exists(listsPath))
            {
                bool needsUpdate = ListUpdater.NeedsUpdate(listsPath, 7);
                if (needsUpdate)
                {
                    LogWarning("⚠ Списки блокировок устарели (старше 7 дней)");
                    fixableIssues.Add("ListsUpdate");
                    warnings++;
                }
                else
                {
                    LogSuccess("✓ Списки блокировок актуальны");
                    success++;
                }
            }

            // Проверка конфликтов
            try
            {
                LogInfo("Проверка конфликтов с другими программами...");
                var conflicts = await ConflictDetector.DetectConflicts();
                if (conflicts.HasConflicts)
                {
                    var criticalConflicts = conflicts.Conflicts.Count(c => c.Severity == ConflictDetector.Severity.Critical);
                    var warningConflicts = conflicts.Conflicts.Count(c => c.Severity == ConflictDetector.Severity.Warning);
                    
                    if (criticalConflicts > 0)
                    {
                        LogError($"✗ Обнаружено критических конфликтов: {criticalConflicts}");
                        errors += criticalConflicts;
                    }
                    if (warningConflicts > 0)
                    {
                        LogWarning($"⚠ Обнаружено предупреждений: {warningConflicts}");
                        warnings += warningConflicts;
                    }
                }
                else
                {
                    LogSuccess("✓ Конфликтов не обнаружено");
                    success++;
                }
            }
            catch (Exception ex)
            {
                LogWarning($"⚠ Ошибка проверки конфликтов: {ex.Message}");
                warnings++;
            }

            // Тестирование и выбор быстрого DNS сервера (как в DNS Jumper)
            // Проверяем, не был ли уже применен DNS сервер ранее (при повторной диагностике пропускаем)
            bool dnsAlreadyApplied = SettingsManager.LoadBoolSetting("UseCustomDNS", false);
            string savedDnsServer = SettingsManager.LoadSetting("DNSServer", "");
            
            if (!dnsAlreadyApplied || string.IsNullOrEmpty(savedDnsServer))
            {
                // Тестируем только если DNS еще не был применен
                try
                {
                    LogInfo("");
                    LogInfo("🌐 Тестирование DNS серверов для выбора самого быстрого...");
                    var progress = new Progress<string>(msg => LogInfo($"  {msg}"));
                    
                    var fastestDns = await DnsTester.FindFastestDnsServer(progress);
                    
                    if (fastestDns != null && fastestDns.IsAvailable)
                    {
                        LogSuccess($"✓ Самый быстрый DNS: {fastestDns.Name} ({fastestDns.PrimaryServer}) - {fastestDns.ResponseTime}мс");
                        success++;
                        
                        // Предлагаем применить быстрый DNS
                        var applyDnsResult = MessageBox.Show(
                            $"Обнаружен самый быстрый DNS сервер:\n\n" +
                            $"📊 {fastestDns.Name}\n" +
                            $"⚡ Время отклика: {fastestDns.ResponseTime}мс\n" +
                            $"📍 Основной: {fastestDns.PrimaryServer}\n" +
                            (!string.IsNullOrEmpty(fastestDns.SecondaryServer) && fastestDns.SecondaryServer != fastestDns.PrimaryServer ? $"📍 Дополнительный: {fastestDns.SecondaryServer}\n" : "") +
                            $"\nПрименить этот DNS сервер?\n\n(Требуются права администратора)",
                            "NeoZapret - Быстрый DNS",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question
                        );

                        if (applyDnsResult == DialogResult.Yes)
                        {
                            LogInfo("Применение быстрого DNS сервера...");
                            var applied = await DnsTester.ApplyDnsServer(fastestDns.PrimaryServer, fastestDns.SecondaryServer);
                            
                            if (applied)
                            {
                                LogSuccess($"✅ DNS сервер {fastestDns.PrimaryServer} успешно применен!");
                                
                                // Сохраняем в настройки
                                SettingsManager.SaveSetting("DNSServer", fastestDns.PrimaryServer);
                                SettingsManager.SaveBoolSetting("UseCustomDNS", true);
                            }
                            else
                            {
                                LogWarning("⚠ Не удалось применить DNS сервер. Возможно, требуются права администратора.");
                                warnings++;
                            }
                        }
                    }
                    else
                    {
                        LogWarning("⚠ Не удалось найти доступные DNS серверы");
                        warnings++;
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"⚠ Ошибка тестирования DNS: {ex.Message}");
                    warnings++;
                }
            }
            else
            {
                // DNS уже был применен - пропускаем тестирование при повторной диагностике
                LogInfo("");
                LogInfo($"ℹ DNS сервер уже применен: {savedDnsServer}");
                LogInfo("  (Тестирование DNS пропущено при повторной диагностике)");
                success++;
            }

            LogInfo("");
            LogInfo("════════════════════════════════════════");
            LogSuccess($"ИТОГОВАЯ СТАТИСТИКА:");
            LogInfo($"- Успешных проверок: {success}");
            if (warnings > 0) LogWarning($"- Предупреждений: {warnings}");
            if (errors > 0) LogError($"- Ошибок: {errors}");
            LogInfo("════════════════════════════════════════");

            // Предложение автоисправления
            if (fixableIssues.Count > 0)
            {
                LogInfo("");
                LogInfo("🔧 Обнаружены проблемы, которые можно исправить автоматически:");
                
                foreach (var issue in fixableIssues)
                {
                    switch (issue)
                    {
                        case "BFE":
                            LogWarning("  • Base Filtering Engine не запущен");
                            break;
                        case "TCPTimestamps":
                            LogWarning("  • TCP timestamps не включены");
                            break;
                        case "ListsUpdate":
                            LogWarning("  • Списки блокировок устарели");
                            break;
                        case "WinwsReadOnly":
                            LogWarning("  • Файл winws.exe имеет атрибут ReadOnly");
                            break;
                        case "WinwsPermissions":
                            LogWarning("  • Нет прав на запись в файл winws.exe");
                            break;
                        case "MissingFiles":
                            LogWarning("  • Отсутствуют необходимые файлы");
                            break;
                    }
                }

                LogInfo("");
                var result = MessageBox.Show(
                    $"Обнаружено {fixableIssues.Count} проблем(ы), которые можно исправить автоматически.\n\n" +
                    "Выполнить автоматическое исправление?",
                    "NeoZapret - Автоисправление",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    await AutoFixIssues(fixableIssues);
                }
            }
            else if (errors == 0 && warnings == 0)
            {
                LogSuccess("✅ ВСЕ ПРОВЕРКИ ПРОЙДЕНЫ УСПЕШНО!");
            }
        }

        private async Task AutoFixIssues(List<string> issues)
        {
            LogInfo("");
            LogInfo("🔧 Начало автоматического исправления...");
            int fixedCount = 0;

            foreach (var issue in issues)
            {
                try
                {
                    DiagnosticsAutoFix.FixResult result = null;

                    switch (issue)
                    {
                        case "BFE":
                            LogInfo("Запуск службы BFE...");
                            result = await DiagnosticsAutoFix.StartBFE();
                            
                            // Повторная проверка после исправления
                            if (result.Success)
                            {
                                await Task.Delay(1000);
                                try
                                {
                                    var service = new ServiceController("BFE");
                                    service.Refresh();
                                    if (service.Status == ServiceControllerStatus.Running)
                                    {
                                        LogSuccess("✓ Проверка: BFE запущена и работает");
                                    }
                                    else
                                    {
                                        LogWarning("⚠ Проверка: BFE все еще не запущена");
                                        result.Success = false;
                                        result.Message = "BFE не удалось запустить (проверка не прошла)";
                                    }
                                }
                                catch
                                {
                                    LogWarning("⚠ Не удалось проверить статус BFE после исправления");
                                }
                            }
                            break;

                        case "TCPTimestamps":
                            LogInfo("Включение TCP timestamps...");
                            LogInfo("⚠ ВНИМАНИЕ: Будет показано окно UAC - подтвердите запрос прав администратора!");
                            LogInfo("  Если окно UAC не появилось, запустите приложение от имени администратора");
                            
                            // Даем пользователю время прочитать сообщение
                            await Task.Delay(2000);
                            
                            result = await DiagnosticsAutoFix.EnableTcpTimestamps();
                            
                            // Расширенная повторная проверка после исправления
                            if (result.Success)
                            {
                                LogInfo("Ожидание применения изменений...");
                                
                                // Проверяем несколько раз с задержками
                                bool verified = false;
                                for (int attempt = 0; attempt < 5; attempt++)
                                {
                                    await Task.Delay(2000); // 2 секунды между попытками
                                    
                                    var tcpCheckResult = await CheckTcpTimestampsAsync();
                                    if (tcpCheckResult.IsOk)
                                    {
                                        verified = true;
                                        LogSuccess($"✓ Проверка #{attempt + 1}: TCP timestamps успешно включены!");
                                        break;
                                    }
                                    else
                                    {
                                        LogInfo($"  Попытка #{attempt + 1}: проверка не прошла, повтор через 2 сек...");
                                    }
                                }
                                
                                if (!verified)
                                {
                                    LogWarning("⚠ После всех проверок: TCP timestamps все еще не включены");
                                    LogWarning("  Возможные причины:");
                                    LogWarning("  1. Изменения еще не применились (попробуйте перезагрузить систему)");
                                    LogWarning("  2. Требуется ручной запуск: netsh interface tcp set global autotuninglevel=normal");
                                    LogWarning("  3. Политики группы могут блокировать изменение");
                                    result.Success = false;
                                    result.Message = "TCP timestamps не удалось включить (требуется ручное подтверждение или перезагрузка)";
                                }
                            }
                            break;

                        case "ListsUpdate":
                            LogInfo("Обновление списков блокировок...");
                            result = await DiagnosticsAutoFix.UpdateListsIfStale(listsPath);
                            
                            // Повторная проверка после обновления
                            if (result.Success && !string.IsNullOrEmpty(listsPath) && Directory.Exists(listsPath))
                            {
                                await Task.Delay(1000);
                                bool needsUpdate = ListUpdater.NeedsUpdate(listsPath, 7);
                                if (!needsUpdate)
                                {
                                    LogSuccess("✓ Проверка: Списки блокировок актуальны");
                                }
                                else
                                {
                                    LogWarning("⚠ Проверка: Списки все еще требуют обновления");
                                }
                            }
                            break;

                        case "WinwsReadOnly":
                            LogInfo("Исправление атрибутов winws.exe...");
                            result = DiagnosticsAutoFix.FixWinwsAttributes(binPath);
                            break;

                        case "WinwsPermissions":
                            LogInfo("Исправление прав доступа к winws.exe...");
                            result = DiagnosticsAutoFix.FixWinwsPermissions(binPath);
                            break;

                        case "MissingFiles":
                            LogWarning("⚠ Отсутствуют файлы - требуется ручное добавление");
                            LogInfo("Проверьте наличие всех необходимых файлов в папке bin");
                            break;
                    }

                    if (result != null)
                    {
                        if (result.Success)
                        {
                            LogSuccess($"✓ {result.Message}");
                            fixedCount++;
                        }
                        else
                        {
                            LogError($"✗ {result.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"✗ Ошибка исправления {issue}: {ex.Message}");
                }
            }

            LogInfo("");
            if (fixedCount > 0)
            {
                LogSuccess($"✅ Автоисправление завершено! Исправлено проблем: {fixedCount}");
                
                // Предлагаем повторную диагностику
                var retry = MessageBox.Show(
                    "Хотите запустить повторную диагностику для проверки исправлений?",
                    "NeoZapret",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (retry == DialogResult.Yes)
                {
                    await RunDiagnosticsAsync();
                }
            }
            else
            {
                LogWarning("⚠ Не удалось исправить обнаруженные проблемы. Возможно, требуются права администратора.");
            }
        }

        /// <summary>
        /// Результат проверки TCP timestamps.
        /// </summary>
        private class TcpTimestampsCheckResult
        {
            public bool IsOk { get; set; }
            public bool IsDisabled { get; set; }
            public string Message { get; set; }
        }

        private bool IsAdministrator()
        {
            try
            {
                System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private async Task<TcpTimestampsCheckResult> CheckTcpTimestampsAsync()
        {
            try
            {
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "interface tcp show global",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                proc.Start();
                var output = await proc.StandardOutput.ReadToEndAsync();
                await System.Threading.Tasks.Task.Run(() => proc.WaitForExit());

                if (output.IndexOf("Autotuning Level", StringComparison.OrdinalIgnoreCase) >= 0 && 
                    (output.IndexOf("normal", StringComparison.OrdinalIgnoreCase) >= 0 || 
                     output.IndexOf("highlyrestricted", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return new TcpTimestampsCheckResult 
                    { 
                        IsOk = true, 
                        IsDisabled = false,
                        Message = "TCP timestamps успешно включены" 
                    };
                }
                else if (output.IndexOf("disabled", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return new TcpTimestampsCheckResult 
                    { 
                        IsOk = false, 
                        IsDisabled = true,
                        Message = "TCP timestamps отключены" 
                    };
                }
                else
                {
                    return new TcpTimestampsCheckResult 
                    { 
                        IsOk = false, 
                        IsDisabled = false,
                        Message = "TCP timestamps имеют неоптимальные настройки" 
                    };
                }
            }
            catch
            {
                return new TcpTimestampsCheckResult 
                { 
                    IsOk = false, 
                    IsDisabled = false,
                    Message = "Не удалось проверить TCP timestamps" 
                };
            }
        }

        private void BtnCleanFiles_Click(object sender, EventArgs e)
        {
            LogInfo("Выполняется очистка...");
            CleanFiles();
        }

        private void CleanFiles()
        {
            int deleted = 0;

            try
            {
                // Проверка и инициализация путей
                if (string.IsNullOrEmpty(appPath) || string.IsNullOrEmpty(binPath))
                {
                    InitializePaths();
                }

                // Очистка логов (старше 7 дней)
                var logsDir = Path.Combine(appPath, "logs");
                if (Directory.Exists(logsDir))
                {
                    foreach (var file in Directory.GetFiles(logsDir, "*.*", SearchOption.AllDirectories))
                    {
                        try
                    {
                        var fileInfo = new FileInfo(file);
                            if (DateTime.Now - fileInfo.LastWriteTime > TimeSpan.FromDays(7))
                        {
                            File.Delete(file);
                            deleted++;
                        }
                        }
                        catch { }
                    }
                }

                // Очистка временных файлов в bin
                if (!string.IsNullOrEmpty(binPath) && Directory.Exists(binPath))
                {
                    foreach (var pattern in new[] { "*.tmp", "*.temp", "*.bak", "*.log" })
                    {
                        try
                        {
                            foreach (var file in Directory.GetFiles(binPath, pattern, SearchOption.TopDirectoryOnly))
                        {
                            File.Delete(file);
                            deleted++;
                        }
                    }
                        catch { }
                    }
                }

                // Очистка временных файлов в корне приложения
                if (!string.IsNullOrEmpty(appPath))
                {
                    foreach (var pattern in new[] { "*.tmp", "*.temp", "*.bak", "~$*" })
                    {
                        try
                        {
                            foreach (var file in Directory.GetFiles(appPath, pattern, SearchOption.TopDirectoryOnly))
                            {
                                File.Delete(file);
                                deleted++;
                            }
                        }
                        catch { }
                    }
                }

                // Очистка зависших процессов
                try
                {
                    var oldCount = Process.GetProcessesByName("winws").Length;
                    StopOldProcesses();
                    var newCount = Process.GetProcessesByName("winws").Length;
                    if (oldCount > newCount)
                    {
                        LogInfo($"Остановлено зависших процессов: {oldCount - newCount}");
                    }
                }
                catch { }

                if (deleted > 0)
                {
                    LogSuccess($"Удалено файлов: {deleted}");
                }
                else
                {
                    LogSuccess("Нет файлов для удаления");
                }
            }
            catch (Exception ex)
            {
                LogError($"Ошибка очистки: {ex.Message}");
                MessageBox.Show($"Ошибка при очистке:\n\n{ex.Message}", "NeoZapret", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            using (var form = new AdvancedSettingsForm())
            {
                form.ShowDialog();
            }
        }

        private void InitializeTrafficEncryption()
        {
            try
            {
                trafficEncryption = new TrafficEncryption();
                
                // Загружаем сохраненные настройки шифрования
                LoadEncryptionSettings();
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка инициализации шифрования трафика", ex);
            }
        }

        private void InitializeProviderDetection()
        {
            // Определяем провайдера асинхронно в фоне (не блокируем запуск приложения)
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    await System.Threading.Tasks.Task.Delay(5000); // Даем время на загрузку UI
                    
                    var provider = await ProviderDetector.DetectProviderAsync();
                    var providerName = ProviderDetector.GetProviderName(provider);
                    
                    if (provider != ProviderDetector.ProviderType.Unknown)
                    {
                        Logger.Success($"Определен провайдер: {providerName}");
                        
                        // Предлагаем оптимальную стратегию для провайдера
                        var recommendedStrategy = ProviderDetector.GetRecommendedStrategy(provider);
                        
                        // Сохраняем в настройки для будущего использования
                        SettingsManager.SaveSetting("DetectedProvider", provider.ToString());
                        SettingsManager.SaveSetting("RecommendedStrategy", recommendedStrategy);
                        
                        // Показываем уведомление в UI (если форма еще не закрыта)
                        if (!this.IsDisposed && this.IsHandleCreated)
                        {
                            this.Invoke(new Action(() =>
                            {
                                LogInfo($"🌐 Определен провайдер: {providerName}");
                                LogInfo($"💡 Рекомендуемая стратегия для вашего провайдера: {GetStrategyDisplayName(recommendedStrategy)}");
                            }));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Ошибка определения провайдера: {ex.Message}");
                }
            });
        }

        private void LoadEncryptionSettings()
        {
            try
            {
                var encryptionEnabled = SettingsManager.LoadBoolSetting("EncryptionEnabled", false);
                if (!encryptionEnabled)
                    return;

                var proxyTypeStr = SettingsManager.LoadSetting("ProxyType", "None");
                Enum.TryParse<TrafficEncryption.ProxyType>(proxyTypeStr, out var proxyType);

                if (proxyType == TrafficEncryption.ProxyType.None)
                    return;

                var settings = new TrafficEncryption.ProxySettings
                {
                    Type = proxyType,
                    Host = SettingsManager.LoadSetting("ProxyHost", ""),
                    Port = SettingsManager.LoadIntSetting("ProxyPort", 1080),
                    Username = SettingsManager.LoadSetting("ProxyUsername", ""),
                    Password = SettingsManager.LoadSetting("ProxyPassword", ""),
                    RequireAuth = SettingsManager.LoadBoolSetting("ProxyRequireAuth", false),
                    UseEncryption = true
                };

                if (string.IsNullOrEmpty(settings.Host))
                    return;

                // Применяем настройки асинхронно (fire and forget)
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    await trafficEncryption.EnableEncryption(settings);
                });
            }
            catch (Exception ex)
            {
                Logger.Warning("Ошибка загрузки настроек шифрования", ex);
            }
        }

        private async void BtnEncryptionSettings_Click(object sender, EventArgs e)
        {
            try
            {
                var currentSettings = trafficEncryption?.GetCurrentSettings();
                var isEnabled = trafficEncryption?.IsEncryptionEnabled() ?? false;

                using (var form = new EncryptionSettingsForm(currentSettings, isEnabled))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        if (form.EncryptionEnabled && form.Settings != null)
                        {
                            // Сохраняем настройки
                            SettingsManager.SaveBoolSetting("EncryptionEnabled", true);
                            SettingsManager.SaveSetting("ProxyType", form.Settings.Type.ToString());
                            SettingsManager.SaveSetting("ProxyHost", form.Settings.Host);
                            SettingsManager.SaveIntSetting("ProxyPort", form.Settings.Port);
                            SettingsManager.SaveSetting("ProxyUsername", form.Settings.Username ?? "");
                            SettingsManager.SaveSetting("ProxyPassword", form.Settings.Password ?? "");
                            SettingsManager.SaveBoolSetting("ProxyRequireAuth", form.Settings.RequireAuth);

                            // Применяем настройки
                            bool success = await trafficEncryption.EnableEncryption(form.Settings);
                            if (success)
                            {
                                LogSuccess("✓ Шифрование трафика включено");
                                MessageBox.Show(
                                    "Шифрование трафика включено!\n\nТеперь весь трафик будет маршрутизироваться через прокси сервер.\n\n" +
                                    "⚠️ Важно: Убедитесь, что прокси сервер доступен, иначе могут возникнуть проблемы с подключением.",
                                    "NeoZapret - Шифрование",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information
                                );
                            }
                            else
                            {
                                LogError("Не удалось включить шифрование");
                            }
                        }
                        else
                        {
                            // Отключаем шифрование
                            trafficEncryption?.DisableEncryption();
                            SettingsManager.SaveBoolSetting("EncryptionEnabled", false);
                            LogInfo("Шифрование трафика отключено");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Ошибка настройки шифрования: {ex.Message}");
                MessageBox.Show($"Ошибка при настройке шифрования:\n\n{ex.Message}", 
                    "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStatus()
        {
            try
            {
                using (var service = new ServiceController("zapret"))
                {
                    var status = service.Status;
                    
                    if (status == ServiceControllerStatus.Running)
                    {
                        statusLabel.Text = "Служба работает";
                        statusLabel.ForeColor = Color.FromArgb(16, 124, 16);
                    }
                    else
                    {
                        statusLabel.Text = "Служба остановлена";
                        statusLabel.ForeColor = Color.FromArgb(247, 99, 12);
                    }
                }
            }
            catch
            {
                statusLabel.Text = "Служба не установлена";
                statusLabel.ForeColor = Color.Gray;
            }

            try
            {
                var processes = Process.GetProcessesByName("winws");
                if (processes.Length > 0)
                {
                    statusLabel.Text = $"Обход активен (PID: {processes[0].Id})";
                    statusLabel.ForeColor = Color.FromArgb(16, 124, 16);
                    // Правильно освобождаем ресурсы
                    foreach (var proc in processes)
                    {
                        proc.Dispose();
                    }
                }
                else
                {
                    if (statusLabel.Text == "Служба не установлена" || statusLabel.Text.IndexOf("Служба остановлена", StringComparison.Ordinal) >= 0)
                        statusLabel.Text = "Готов к работе";
                }
            }
            catch
            {
                // Игнорируем ошибки получения процессов
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Всегда сворачиваем в трей при закрытии
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
            }
            else
            {
                // Освобождаем ресурсы при завершении работы
                try
                {
                    statusUpdateTimer?.Stop();
                    statusUpdateTimer?.Dispose();
                    bypassMonitor?.StopMonitoring();
                    trafficStatistics?.EndSession();
                    trafficEncryption?.DisableEncryption(); // Отключаем шифрование при выходе
                    SmartUpdater.Stop(); // Останавливаем умный обновлятор
                    Logger.Flush(); // Принудительно записываем логи
                    trayIcon?.Dispose();
                }
                catch { }
            }
            base.OnFormClosing(e);
        }

        private void BtnStrategyGenerator_Click(object sender, EventArgs e)
        {
            using (var form = new StrategyGeneratorTesterForm())
            {
                if (form.ShowDialog() == DialogResult.OK && form.StrategyApplied)
                {
                    // Применяем сгенерированную стратегию
                    StartBypassCustom(form.GeneratedStrategy);
                }
            }
        }

        private async void BtnUpdateLists_Click(object sender, EventArgs e)
        {
            try
            {
                btnUpdateLists.Enabled = false;
                LogInfo("Начинаю обновление списков доменов и IP...");

                if (string.IsNullOrEmpty(listsPath))
                {
                    InitializePaths();
                }

                if (string.IsNullOrEmpty(listsPath) || !Directory.Exists(listsPath))
                {
                    LogError("Папка lists не найдена!");
                    btnUpdateLists.Enabled = true;
                    return;
                }

                var progress = new Progress<string>(message => LogInfo(message));
                
                var result = await ListUpdater.UpdateAllLists(listsPath, progress);

                if (result.Success)
                {
                    LogSuccess($"✓ Списки успешно обновлены: {string.Join(", ", result.UpdatedFiles)}");
                    MessageBox.Show($"Списки успешно обновлены!\n\nОбновлено файлов: {result.UpdatedFiles.Count}\n\n{string.Join("\n", result.UpdatedFiles)}", 
                        "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    LogError($"✗ Ошибка обновления списков: {result.ErrorMessage}");
                    MessageBox.Show($"Не удалось обновить списки.\n\nОшибка: {result.ErrorMessage}", 
                        "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                LogError($"Ошибка обновления списков: {ex.Message}");
                Logger.Error("Ошибка обновления списков", ex);
                MessageBox.Show($"Ошибка при обновлении списков:\n\n{ex.Message}", 
                    "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnUpdateLists.Enabled = true;
            }
        }

        private async void BtnCheckUpdates_Click(object sender, EventArgs e)
        {
            try
            {
                btnCheckUpdates.Enabled = false;
                LogInfo("Проверяю наличие обновлений...");

                var updateInfo = await UpdateChecker.CheckForUpdates();

                if (updateInfo.IsUpdateAvailable)
                {
                    var message = $"Доступна новая версия NeoZapret!\n\n" +
                                  $"Текущая версия: {updateInfo.CurrentVersion}\n" +
                                  $"Новая версия: {updateInfo.LatestVersion}\n\n" +
                                  $"Хотите открыть страницу загрузки?";
                    
                    var result = MessageBox.Show(message, "NeoZapret - Обновление доступно", 
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = updateInfo.DownloadUrl ?? UpdateChecker.GitHubReleasesUrl,
                                UseShellExecute = true
                            });
                            LogInfo($"Открыта страница загрузки: {updateInfo.DownloadUrl}");
                        }
                        catch (Exception ex)
                        {
                            LogError($"Не удалось открыть страницу загрузки: {ex.Message}");
                            Logger.Error("Ошибка открытия страницы загрузки", ex);
                        }
                    }
                }
                else
                {
                    LogSuccess("✓ Установлена последняя версия приложения");
                    MessageBox.Show("У вас установлена последняя версия NeoZapret!", 
                        "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LogError($"Ошибка проверки обновлений: {ex.Message}");
                Logger.Error("Ошибка проверки обновлений", ex);
            }
            finally
            {
                btnCheckUpdates.Enabled = true;
            }
        }

        private string GetStrategyDisplayName(string strategy)
        {
            if (strategy.StartsWith("custom:"))
            {
                return "Кастомная стратегия";
            }
            if (strategy.Contains("\\") || strategy.Contains("/"))
            {
                return $"Кастомная: {Path.GetFileNameWithoutExtension(strategy)}";
            }
            
            switch (strategy.ToLowerInvariant())
            {
                case "fast": return "Быстрая";
                case "recommended": return "Рекомендуемая";
                case "max": return "Максимальная защита";
                case "aggressive": return "Агрессивная (для жестких блокировок)";
                case "games": return "Игры";
                
                // Стратегии для провайдеров
                case "rostelecom": return "Ростелеком";
                case "mts": return "МТС";
                case "beeline": return "Билайн";
                case "megafon": return "Мегафон";
                case "tele2": return "Теле2";
                case "ttk": return "ТТК";
                case "ertelecom": return "Эр-Телеком";
                
                default: return strategy;
            }
        }
    }
}
