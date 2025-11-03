using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace NeoZapret
{
    public partial class ServiceManageForm : Form
    {
        private RichTextBox txtLog;
        private string appPath;
        private string binPath;
        private string listsPath;

        public ServiceManageForm()
        {
            InitializeComponent();
            InitializePaths();
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
                    Logger.Warning($"Пути не найдены в ServiceManageForm, startupPath: {startupPath}");
                }
                else
                {
                    Logger.Info($"Пути успешно инициализированы в ServiceManageForm: appPath={appPath}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка инициализации путей в ServiceManageForm", ex);
                appPath = Application.StartupPath ?? Environment.CurrentDirectory ?? ".";
                binPath = Path.Combine(appPath, "bin");
                listsPath = Path.Combine(appPath, "lists");
            }
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "Управление службой";
            this.Size = new Size(620, 540);
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
                Text = "Управление службой",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 220, 230),
                AutoSize = true,
                Location = new Point(30, 20)
            };

            Label lblAuthor = new Label
            {
                Text = "Автор: Soulxel | Тестровщик: Матвей Котов | ALPHA • В РАЗРАБОТКЕ",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 150, 160),
                AutoSize = true,
                Location = new Point(50, 490)
            };

            Color buttonColor = Color.FromArgb(50, 50, 58);
            
            Button btnInstall = CreateButton("Установить как службу Windows", new Point(50, 70), buttonColor, 500);
            btnInstall.Click += BtnInstall_Click;

            Button btnRemove = CreateButton("Удалить службу", new Point(50, 130), buttonColor, 500);
            btnRemove.Click += BtnRemove_Click;

            Button btnRestart = CreateButton("Перезапустить службу", new Point(50, 190), buttonColor, 500);
            btnRestart.Click += BtnRestart_Click;

            Panel logPanel = new Panel
            {
                Location = new Point(50, 250),
                Size = new Size(500, 150),
                BackColor = Color.FromArgb(38, 38, 44),
                BorderStyle = BorderStyle.None
            };
            logPanel.Paint += (s, e) =>
            {
                var rect = logPanel.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;
                using (var pen = new Pen(Color.FromArgb(60, 60, 70), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
                }
            };

            txtLog = new RichTextBox
            {
                Location = new Point(5, 5),
                Size = new Size(490, 140),
                BackColor = Color.FromArgb(24, 24, 30),
                ForeColor = Color.FromArgb(220, 220, 230),
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                BorderStyle = BorderStyle.None
            };
            logPanel.Controls.Add(txtLog);

            Button btnClose = CreateButton("Закрыть", new Point(50, 410), buttonColor, 500);
            btnClose.DialogResult = DialogResult.Cancel;

            this.Controls.Add(lblTitle);
            this.Controls.Add(btnInstall);
            this.Controls.Add(btnRemove);
            this.Controls.Add(btnRestart);
            this.Controls.Add(logPanel);
            this.Controls.Add(btnClose);
            this.Controls.Add(lblAuthor);
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

        private void Log(string message)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            using (var form = new StrategySelectForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    InstallService(form.SelectedStrategy);
                }
            }
        }

        private void InstallService(string strategy)
        {
            try
            {
                // Проверка и инициализация путей
                if (string.IsNullOrEmpty(binPath) || string.IsNullOrEmpty(listsPath))
                {
                    InitializePaths();
                }

                // Проверка существования файлов
                if (string.IsNullOrEmpty(binPath))
                {
                    Log("Ошибка: не удалось определить путь к папке bin!");
                    MessageBox.Show("Ошибка: не удалось определить путь к папке bin!\n\nУбедитесь, что приложение запущено из правильной директории.", 
                        "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var winwsPath = Path.Combine(binPath, "winws.exe");
                if (string.IsNullOrEmpty(winwsPath) || !File.Exists(winwsPath))
                {
                    Log($"Ошибка: файл winws.exe не найден: {winwsPath ?? "null"}");
                    MessageBox.Show($"Файл winws.exe не найден!\n\nПуть: {winwsPath}\n\nУбедитесь, что папка 'bin' находится рядом с приложением.", 
                        "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (string.IsNullOrEmpty(strategy))
                {
                    Log("Ошибка: стратегия не выбрана!");
                    MessageBox.Show("Ошибка: стратегия не выбрана!", "NeoZapret", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Log("Останавливаю существующую службу...");
                StopService();

                Log("Удаляю старую службу...");
                DeleteService();

                Log("Создаю новую службу...");

                var gameFilter = LoadGameFilter();
                var args = GenerateServiceArguments(strategy, gameFilter);
                
                if (string.IsNullOrWhiteSpace(args))
                {
                    Log("Ошибка: не удалось сгенерировать аргументы для стратегии!");
                    MessageBox.Show("Ошибка: не удалось сгенерировать аргументы для выбранной стратегии!", 
                        "NeoZapret", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Формируем команду для sc.exe
                // sc.exe требует специальный формат: binPath= "путь с пробелами"
                // Если в пути есть пробелы, он должен быть в кавычках
                var escapedExePath = winwsPath.Contains(" ") ? $"\"{winwsPath}\"" : winwsPath;
                var fullCommand = $"{escapedExePath} {args}";
                
                // Для sc.exe команда должна быть в кавычках целиком, если содержит пробелы
                var scCommand = fullCommand.Contains(" ") ? $"\"{fullCommand}\"" : fullCommand;

                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sc",
                        Arguments = $"create zapret binPath= {scCommand} DisplayName= \"zapret\" start= auto",
                        UseShellExecute = true,
                        Verb = "runas",
                        CreateNoWindow = true
                    }
                };

                proc.Start();
                proc.WaitForExit();

                if (proc.ExitCode == 0)
                {
                    Log("Добавляю описание службы...");
                    var descProc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "sc",
                            Arguments = "description zapret \"Zapret DPI bypass software\"",
                            UseShellExecute = true,
                            Verb = "runas",
                            CreateNoWindow = true
                        }
                    };
                    descProc.Start();
                    descProc.WaitForExit();

                    Log("Добавляю информацию о стратегии...");
                    try
                    {
                        using (var key = Registry.LocalMachine.CreateSubKey(@"System\CurrentControlSet\Services\zapret"))
                        {
                            key.SetValue("zapret-strategy", strategy);
                        }
                    }
                    catch { }

                    Log("Запускаю службу...");
                    StartService();

                    Log("Служба успешно установлена и запущена!");
                }
                else
                {
                    Log("Ошибка установки службы");
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка: {ex.Message}");
            }
        }

        private async void BtnRemove_Click(object sender, EventArgs e)
        {
            try
            {
                Log("Удаление службы...");
                StopService();
                await System.Threading.Tasks.Task.Delay(2000); // Асинхронная задержка без блокировки UI
                DeleteService();

                foreach (var process in Process.GetProcessesByName("winws"))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch { }
                }

                Log("Служба удалена");
            }
            catch (Exception ex)
            {
                Log($"Ошибка: {ex.Message}");
            }
        }

        private async void BtnRestart_Click(object sender, EventArgs e)
        {
            try
            {
                Log("Перезапуск службы...");
                StopService();
                await System.Threading.Tasks.Task.Delay(2000); // Асинхронная задержка без блокировки UI
                StartService();
                Log("Служба перезапущена");
            }
            catch (Exception ex)
            {
                Log($"Ошибка: {ex.Message}");
            }
        }

        private void StopService()
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = "stop zapret",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true
                }
            };
            proc.Start();
            proc.WaitForExit();
        }

        private void StartService()
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = "start zapret",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true
                }
            };
            proc.Start();
            proc.WaitForExit();
        }

        private void DeleteService()
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = "delete zapret",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true
                }
            };
            proc.Start();
            proc.WaitForExit();
        }

        private string LoadGameFilter()
        {
            try
            {
                if (string.IsNullOrEmpty(binPath))
                {
                    InitializePaths();
                }
                
                if (string.IsNullOrEmpty(binPath)) return "12";
                
            var flagFile = Path.Combine(binPath, "game_filter.enabled");
                if (string.IsNullOrEmpty(flagFile)) return "12";
                
            return File.Exists(flagFile) ? "1024-65535" : "12";
            }
            catch
            {
                return "12";
            }
        }

        private string GenerateServiceArguments(string strategy, string gameFilter)
        {
            try
            {
                // Проверка путей перед использованием
                if (string.IsNullOrEmpty(listsPath) || string.IsNullOrEmpty(binPath))
                {
                    InitializePaths();
                }

                if (string.IsNullOrEmpty(listsPath) || string.IsNullOrEmpty(binPath))
                {
                    Log("Ошибка: пути bin или lists не определены!");
                    Logger.Error("Пути bin или lists не определены в ServiceManageForm");
                    return "";
                }

                if (string.IsNullOrEmpty(strategy))
                {
                    Log("Ошибка: стратегия не указана!");
                    Logger.Warning("Стратегия не указана при генерации аргументов службы");
                    return "";
                }

                if (string.IsNullOrEmpty(gameFilter))
                {
                    gameFilter = "12";
                }

                // Используем централизованный генератор аргументов
                var args = StrategyArgumentsGenerator.GenerateBypassArguments(strategy, gameFilter, binPath, listsPath);
                
                if (string.IsNullOrEmpty(args))
                {
                    Log($"Ошибка генерации аргументов для стратегии '{strategy}'");
                    Logger.Error($"Не удалось сгенерировать аргументы для стратегии службы: {strategy}");
                }
                else
                {
                    Logger.Info($"Аргументы успешно сгенерированы для службы: {strategy}");
                }
                
                return args;
            }
            catch (Exception ex)
            {
                Log($"Ошибка генерации аргументов: {ex.Message}");
                Logger.Error("Ошибка генерации аргументов службы", ex);
                return "";
            }
        }
    }
}
