using System;
using System.IO;
using Microsoft.Win32;

namespace NeoZapret
{
    /// <summary>
    /// Менеджер автозапуска приложения.
    /// Настраивает автозапуск через реестр и службы Windows для максимальной надежности.
    /// </summary>
    public static class AutoStartManager
    {
        private const string REGISTRY_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string REGISTRY_VALUE_NAME = "NeoZapret";

        /// <summary>
        /// Проверяет, настроен ли автозапуск.
        /// </summary>
        public static bool IsAutoStartEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY))
                {
                    if (key == null)
                        return false;

                    var value = key.GetValue(REGISTRY_VALUE_NAME)?.ToString();
                    return !string.IsNullOrEmpty(value);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Ошибка проверки автозапуска", ex);
                return false;
            }
        }

        /// <summary>
        /// Включает автозапуск приложения.
        /// </summary>
        public static bool EnableAutoStart(string appPath)
        {
            try
            {
                if (string.IsNullOrEmpty(appPath) || !File.Exists(appPath))
                {
                    Logger.Error("Неверный путь к приложению для автозапуска");
                    return false;
                }

                // Получаем полный путь с кавычками для обработки пробелов
                var exePath = Path.GetFullPath(appPath);
                var startupCommand = $"\"{exePath}\"";

                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true))
                {
                    if (key == null)
                    {
                        // Создаем ключ, если его нет
                        using (var newKey = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY))
                        {
                            newKey?.SetValue(REGISTRY_VALUE_NAME, startupCommand);
                        }
                    }
                    else
                    {
                        key.SetValue(REGISTRY_VALUE_NAME, startupCommand);
                    }
                }

                Logger.Success("Автозапуск включен через реестр");
                
                // Также создаем ярлык в папке автозагрузки (дополнительный метод)
                CreateStartupShortcut(appPath);
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка включения автозапуска", ex);
                return false;
            }
        }

        /// <summary>
        /// Отключает автозапуск приложения.
        /// </summary>
        public static bool DisableAutoStart()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(REGISTRY_VALUE_NAME, false);
                    }
                }

                // Удаляем ярлык из папки автозагрузки
                RemoveStartupShortcut();

                Logger.Success("Автозапуск отключен");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка отключения автозапуска", ex);
                return false;
            }
        }

        /// <summary>
        /// Создает ярлык в папке автозагрузки (дополнительный метод для надежности).
        /// </summary>
        private static void CreateStartupShortcut(string appPath)
        {
            try
            {
                var startupFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                    "NeoZapret.lnk");

                // Используем PowerShell для создания ярлыка (более надежно, чем через COM)
                var script = $@"
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut('{startupFolder}')
$Shortcut.TargetPath = '{appPath}'
$Shortcut.WorkingDirectory = '{Path.GetDirectoryName(appPath)}'
$Shortcut.Description = 'NeoZapret - Обход DPI блокировок'
$Shortcut.Save()
";

                var proc = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                proc.Start();
                proc.WaitForExit(3000);
            }
            catch (Exception ex)
            {
                Logger.Debug($"Не удалось создать ярлык автозагрузки: {ex.Message}");
                // Не критично, так как реестр уже работает
            }
        }

        /// <summary>
        /// Удаляет ярлык из папки автозагрузки.
        /// </summary>
        private static void RemoveStartupShortcut()
        {
            try
            {
                var startupFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                    "NeoZapret.lnk");

                if (File.Exists(startupFolder))
                {
                    File.Delete(startupFolder);
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"Не удалось удалить ярлык автозагрузки: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверяет и исправляет автозапуск, если путь изменился.
        /// </summary>
        public static bool VerifyAndFixAutoStart(string currentAppPath)
        {
            try
            {
                if (!IsAutoStartEnabled())
                    return false;

                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY))
                {
                    if (key == null)
                        return false;

                    var currentValue = key.GetValue(REGISTRY_VALUE_NAME)?.ToString();
                    var expectedValue = $"\"{Path.GetFullPath(currentAppPath)}\"";

                    if (currentValue != expectedValue)
                    {
                        Logger.Info("Путь автозапуска устарел, обновляю...");
                        return EnableAutoStart(currentAppPath);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Warning("Ошибка проверки автозапуска", ex);
                return false;
            }
        }
    }
}

