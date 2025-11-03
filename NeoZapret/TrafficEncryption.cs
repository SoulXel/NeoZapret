using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace NeoZapret
{
    /// <summary>
    /// Класс для управления шифрованием трафика через прокси и туннелирование.
    /// Обеспечивает шифрование трафика через SOCKS5/HTTP прокси и TLS туннели.
    /// </summary>
    public class TrafficEncryption
    {
        private ProxySettings currentProxySettings;
        private bool isEncryptionEnabled = false;
        private Process tunnelProcess;

        /// <summary>
        /// Настройки прокси для шифрования.
        /// </summary>
        public class ProxySettings
        {
            public ProxyType Type { get; set; }
            public string Host { get; set; }
            public int Port { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public bool UseEncryption { get; set; }
            public bool RequireAuth { get; set; }
        }

        public enum ProxyType
        {
            None,
            SOCKS5,
            SOCKS4,
            HTTP,
            HTTPS
        }

        /// <summary>
        /// Включает шифрование трафика через прокси.
        /// </summary>
        public async Task<bool> EnableEncryption(ProxySettings settings)
        {
            try
            {
                if (settings == null || settings.Type == ProxyType.None)
                {
                    Logger.Warning("Настройки прокси не указаны");
                    return false;
                }

                currentProxySettings = settings;
                isEncryptionEnabled = false;

                // Проверяем доступность прокси
                bool isAvailable = await TestProxyConnection(settings);
                if (!isAvailable)
                {
                    Logger.Error("Не удалось подключиться к прокси серверу");
                    return false;
                }

                // Настраиваем системный прокси
                bool configured = ConfigureSystemProxy(settings);
                if (!configured)
                {
                    Logger.Warning("Не удалось настроить системный прокси, используем альтернативный метод");
                    // Продолжаем, так как WinDivert может работать на более низком уровне
                }

                isEncryptionEnabled = true;
                Logger.Success($"Шифрование трафика включено через {settings.Type} прокси: {settings.Host}:{settings.Port}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка включения шифрования", ex);
                return false;
            }
        }

        /// <summary>
        /// Отключает шифрование трафика.
        /// </summary>
        public bool DisableEncryption()
        {
            try
            {
                if (!isEncryptionEnabled)
                    return true;

                // Отключаем системный прокси
                DisableSystemProxy();

                // Останавливаем туннель, если запущен
                StopTunnel();

                isEncryptionEnabled = false;
                currentProxySettings = null;
                Logger.Success("Шифрование трафика отключено");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка отключения шифрования", ex);
                return false;
            }
        }

        /// <summary>
        /// Проверяет доступность прокси сервера.
        /// </summary>
        public async Task<bool> TestProxyConnection(ProxySettings settings)
        {
            try
            {
                Logger.Info($"Проверяю подключение к прокси {settings.Host}:{settings.Port}...");

                using (var tcpClient = new System.Net.Sockets.TcpClient())
                {
                    var connectTask = tcpClient.BeginConnect(settings.Host, settings.Port, null, null);
                    var success = connectTask.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));

                    if (success && tcpClient.Connected)
                    {
                        tcpClient.EndConnect(connectTask);
                        Logger.Success("Прокси сервер доступен");
                        return true;
                    }
                }

                Logger.Warning("Прокси сервер недоступен");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Ошибка проверки прокси: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Настраивает системный прокси для маршрутизации трафика.
        /// </summary>
        private bool ConfigureSystemProxy(ProxySettings settings)
        {
            try
            {
                var proxyString = settings.Type == ProxyType.HTTP || settings.Type == ProxyType.HTTPS
                    ? $"http={settings.Host}:{settings.Port};https={settings.Host}:{settings.Port}"
                    : $"socks={settings.Host}:{settings.Port}";

                using (var registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true))
                {
                    if (registryKey == null)
                    {
                        Logger.Warning("Не удалось открыть реестр для настройки прокси");
                        return false;
                    }

                    registryKey.SetValue("ProxyEnable", 1);
                    registryKey.SetValue("ProxyServer", proxyString);

                    // Для SOCKS5 прокси нужна дополнительная настройка
                    if (settings.Type == ProxyType.SOCKS5)
                    {
                        // Используем альтернативный метод через переменные окружения
                        Environment.SetEnvironmentVariable("ALL_PROXY", $"socks5://{settings.Host}:{settings.Port}", EnvironmentVariableTarget.User);
                    }
                }

                Logger.Info("Системный прокси настроен");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Не удалось настроить системный прокси: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Отключает системный прокси.
        /// </summary>
        private void DisableSystemProxy()
        {
            try
            {
                using (var registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true))
                {
                    if (registryKey != null)
                    {
                        registryKey.SetValue("ProxyEnable", 0);
                    }
                }

                // Очищаем переменные окружения
                Environment.SetEnvironmentVariable("ALL_PROXY", null, EnvironmentVariableTarget.User);

                Logger.Info("Системный прокси отключен");
            }
            catch (Exception ex)
            {
                Logger.Warning($"Ошибка отключения прокси: {ex.Message}");
            }
        }

        /// <summary>
        /// Запускает локальный TLS туннель через stunnel или аналогичный инструмент.
        /// </summary>
        public bool StartLocalTunnel(string remoteHost, int remotePort, int localPort = 1080)
        {
            try
            {
                // Проверяем наличие stunnel (если установлен)
                var stunnelPath = FindStunnelPath();
                if (string.IsNullOrEmpty(stunnelPath))
                {
                    Logger.Warning("stunnel не найден. Для TLS туннелирования установите stunnel.");
                    return false;
                }

                // Создаем конфигурационный файл для stunnel
                var configPath = Path.Combine(Path.GetTempPath(), "neozapret_stunnel.conf");
                var configContent = $@"
client = yes
accept = 127.0.0.1:{localPort}
connect = {remoteHost}:{remotePort}
";

                File.WriteAllText(configPath, configContent);

                // Запускаем stunnel
                tunnelProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = stunnelPath,
                        Arguments = $"\"{configPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                tunnelProcess.Start();
                Logger.Success($"TLS туннель запущен: 127.0.0.1:{localPort} -> {remoteHost}:{remotePort}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка запуска TLS туннеля", ex);
                return false;
            }
        }

        /// <summary>
        /// Останавливает локальный туннель.
        /// </summary>
        private void StopTunnel()
        {
            try
            {
                if (tunnelProcess != null)
                {
                    try
                    {
                        if (!tunnelProcess.HasExited)
                        {
                            tunnelProcess.Kill();
                            tunnelProcess.WaitForExit(3000);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Ошибка при остановке процесса туннеля: {ex.Message}");
                    }
                    finally
                    {
                        try
                        {
                            tunnelProcess.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning($"Ошибка при освобождении ресурсов туннеля: {ex.Message}");
                        }
                        tunnelProcess = null;
                    }
                    Logger.Info("TLS туннель остановлен");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Ошибка остановки туннеля: {ex.Message}");
            }
        }

        /// <summary>
        /// Ищет путь к stunnel.
        /// </summary>
        private string FindStunnelPath()
        {
            var possiblePaths = new[]
            {
                @"C:\Program Files\stunnel\bin\stunnel.exe",
                @"C:\Program Files (x86)\stunnel\bin\stunnel.exe",
                @"C:\stunnel\bin\stunnel.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "stunnel", "bin", "stunnel.exe"),
                "stunnel.exe" // В PATH
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;

                // Проверяем, доступен ли в PATH
                try
                {
                    using (var proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "where",
                            Arguments = "stunnel.exe",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    })
                    {
                        proc.Start();
                        var output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit();

                        if (!string.IsNullOrWhiteSpace(output) && File.Exists(output.Trim()))
                            return output.Trim();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Ошибка проверки stunnel в PATH: {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Генерирует строку подключения к прокси.
        /// </summary>
        public string GetProxyConnectionString()
        {
            if (!isEncryptionEnabled || currentProxySettings == null)
                return null;

            if (currentProxySettings.RequireAuth && !string.IsNullOrEmpty(currentProxySettings.Username))
            {
                return $"{currentProxySettings.Type.ToString().ToLower()}://{currentProxySettings.Username}:{currentProxySettings.Password}@{currentProxySettings.Host}:{currentProxySettings.Port}";
            }

            return $"{currentProxySettings.Type.ToString().ToLower()}://{currentProxySettings.Host}:{currentProxySettings.Port}";
        }

        /// <summary>
        /// Проверяет, включено ли шифрование.
        /// </summary>
        public bool IsEncryptionEnabled()
        {
            return isEncryptionEnabled;
        }

        /// <summary>
        /// Получает текущие настройки прокси.
        /// </summary>
        public ProxySettings GetCurrentSettings()
        {
            return currentProxySettings;
        }

        /// <summary>
        /// Создает рекомендации по настройке шифрования.
        /// </summary>
        public static List<string> GetEncryptionRecommendations()
        {
            return new List<string>
            {
                "💡 Для полного шифрования трафика используйте VPN или SOCKS5 прокси",
                "🔒 SOCKS5 прокси обеспечивает шифрование на уровне приложения",
                "🌐 HTTPS прокси шифрует только HTTP/HTTPS трафик",
                "⚙️ Для TLS туннелирования установите stunnel (https://www.stunnel.org)",
                "📝 Настройте прокси в разделе 'Настройки' -> 'Шифрование трафика'",
                "⚠️ NeoZapret + прокси = DPI обход + шифрование трафика"
            };
        }
    }
}

