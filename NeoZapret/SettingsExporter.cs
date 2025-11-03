using System;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace NeoZapret
{
    /// <summary>
    /// Класс для экспорта и импорта настроек приложения.
    /// Позволяет сохранять и загружать конфигурацию.
    /// </summary>
    public static class SettingsExporter
    {
        /// <summary>
        /// Экспортирует все настройки в файл.
        /// </summary>
        public static bool ExportSettings(string filePath)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# NeoZapret Settings Export");
                sb.AppendLine($"# Export Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();

                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\NeoZapret", false))
                {
                    if (key != null)
                    {
                        foreach (var valueName in key.GetValueNames())
                        {
                            var value = key.GetValue(valueName);
                            sb.AppendLine($"{valueName}={value}");
                        }
                    }
                }

                // Экспортируем дополнительные настройки
                sb.AppendLine();
                sb.AppendLine("# Additional Settings");
                sb.AppendLine($"AutoStart={IsAutoStartEnabled()}");
                
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                Logger.Success($"Настройки экспортированы в: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка экспорта настроек", ex);
                return false;
            }
        }

        /// <summary>
        /// Импортирует настройки из файла.
        /// </summary>
        public static bool ImportSettings(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Logger.Error($"Файл настроек не найден: {filePath}");
                    return false;
                }

                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                int imported = 0;

                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\NeoZapret", true))
                {
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                            continue;

                        var parts = line.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            var name = parts[0].Trim();
                            var value = parts[1].Trim();

                            if (name == "AutoStart")
                            {
                                if (bool.TryParse(value, out bool autoStart))
                                {
                                    SetAutoStart(autoStart);
                                }
                            }
                            else
                            {
                                key.SetValue(name, value);
                                imported++;
                            }
                        }
                    }
                }

                Logger.Success($"Настройки импортированы: {imported} параметров");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка импорта настроек", ex);
                return false;
            }
        }

        private static bool IsAutoStartEnabled()
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

        private static void SetAutoStart(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (enable)
                    {
                        key?.SetValue("NeoZapret", $"\"{System.Windows.Forms.Application.ExecutablePath}\"");
                    }
                    else
                    {
                        key?.DeleteValue("NeoZapret", false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Не удалось настроить автозапуск", ex);
            }
        }
    }
}

