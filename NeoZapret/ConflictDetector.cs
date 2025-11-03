using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace NeoZapret
{
    /// <summary>
    /// Класс для обнаружения конфликтов с антивирусами, VPN и другими программами.
    /// Помогает диагностировать проблемы со стабильностью работы.
    /// </summary>
    public static class ConflictDetector
    {
        /// <summary>
        /// Результат проверки конфликтов.
        /// </summary>
        public class ConflictResult
        {
            public bool HasConflicts { get; set; }
            public List<ConflictInfo> Conflicts { get; set; } = new List<ConflictInfo>();
            public List<string> Recommendations { get; set; } = new List<string>();
        }

        /// <summary>
        /// Информация о конфликте.
        /// </summary>
        public class ConflictInfo
        {
            public ConflictType Type { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public Severity Severity { get; set; }
            public string Recommendation { get; set; }
        }

        public enum ConflictType
        {
            Antivirus,
            VPN,
            Firewall,
            Proxy,
            Other
        }

        public enum Severity
        {
            Info,
            Warning,
            Critical
        }

        /// <summary>
        /// Проверяет наличие потенциальных конфликтов.
        /// </summary>
        public static async Task<ConflictResult> DetectConflicts()
        {
            var result = new ConflictResult();

            try
            {
                Logger.Info("Начинаю проверку конфликтов с антивирусами, VPN и другими программами...");

                // Проверяем антивирусы
                var antivirusConflicts = await DetectAntivirus();
                result.Conflicts.AddRange(antivirusConflicts);

                // Проверяем VPN
                var vpnConflicts = await DetectVPN();
                result.Conflicts.AddRange(vpnConflicts);

                // Проверяем файрволы
                var firewallConflicts = await DetectFirewall();
                result.Conflicts.AddRange(firewallConflicts);

                // Проверяем прокси
                var proxyConflicts = DetectProxy();
                result.Conflicts.AddRange(proxyConflicts);

                // Проверяем блокировку портов
                var portConflicts = await DetectPortBlocking();
                result.Conflicts.AddRange(portConflicts);

                result.HasConflicts = result.Conflicts.Any(c => c.Severity == Severity.Warning || c.Severity == Severity.Critical);
                
                // Генерируем рекомендации
                result.Recommendations = GenerateRecommendations(result.Conflicts);

                if (result.HasConflicts)
                {
                    Logger.Warning($"Обнаружено конфликтов: {result.Conflicts.Count(c => c.Severity == Severity.Warning || c.Severity == Severity.Critical)}");
                }
                else
                {
                    Logger.Success("Конфликтов не обнаружено");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при проверке конфликтов", ex);
            }

            return result;
        }

        /// <summary>
        /// Обнаруживает установленные антивирусы.
        /// </summary>
        private static async Task<List<ConflictInfo>> DetectAntivirus()
        {
            var conflicts = new List<ConflictInfo>();

            try
            {
                await Task.Run(() =>
                {
                    // Известные процессы антивирусов
                    var antivirusProcesses = new Dictionary<string, string>
                    {
                        { "avast", "Avast" },
                        { "avgnt", "Avira" },
                        { "avgcsrvx", "AVG" },
                        { "avgidsagent", "AVG" },
                        { "avgwdsvc", "AVG" },
                        { "kaspersky", "Kaspersky" },
                        { "kavfs", "Kaspersky" },
                        { "avp", "Kaspersky" },
                        { "bdagent", "BitDefender" },
                        { "vsserv", "BitDefender" },
                        { "mbam", "Malwarebytes" },
                        { "mbamtray", "Malwarebytes" },
                        { "msmpeng", "Windows Defender" },
                        { "smc", "Symantec" },
                        { "symantec", "Symantec" },
                        { "nod32krn", "ESET" },
                        { "egui", "ESET" },
                        { "ekrn", "ESET" },
                        { "mcshield", "McAfee" },
                        { "mcafee", "McAfee" },
                        { "norton", "Norton" },
                        { "sophos", "Sophos" }
                    };

                    foreach (var process in Process.GetProcesses())
                    {
                        try
                        {
                            var processName = process.ProcessName.ToLower();
                            if (antivirusProcesses.ContainsKey(processName))
                            {
                                var antivirusName = antivirusProcesses[processName];
                                conflicts.Add(new ConflictInfo
                                {
                                    Type = ConflictType.Antivirus,
                                    Name = antivirusName,
                                    Description = $"Обнаружен антивирус: {antivirusName}. Может блокировать сетевые соединения WinDivert.",
                                    Severity = Severity.Warning,
                                    Recommendation = $"Добавьте папку с winws.exe и NeoZapret.exe в исключения {antivirusName}. Также добавьте исключение для драйвера WinDivert."
                                });
                            }
                        }
                        catch { }
                        finally
                        {
                            process.Dispose();
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Warning("Ошибка при обнаружении антивирусов", ex);
            }

            return conflicts;
        }

        /// <summary>
        /// Обнаруживает активные VPN соединения.
        /// </summary>
        private static async Task<List<ConflictInfo>> DetectVPN()
        {
            var conflicts = new List<ConflictInfo>();

            try
            {
                await Task.Run(() =>
                {
                    // Проверяем сетевые адаптеры VPN
                    var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                    var vpnInterfaces = networkInterfaces.Where(ni => 
                        ni.NetworkInterfaceType == NetworkInterfaceType.Ppp ||
                        ni.Description.ToLower().Contains("vpn") ||
                        ni.Description.ToLower().Contains("tun") ||
                        ni.Description.ToLower().Contains("tap") ||
                        ni.Name.ToLower().Contains("vpn") ||
                        ni.Name.ToLower().Contains("tun") ||
                        ni.Name.ToLower().Contains("tap")
                    ).ToList();

                    if (vpnInterfaces.Any(ni => ni.OperationalStatus == OperationalStatus.Up))
                    {
                        conflicts.Add(new ConflictInfo
                        {
                            Type = ConflictType.VPN,
                            Name = "Активный VPN",
                            Description = "Обнаружено активное VPN соединение. NeoZapret может конфликтовать с VPN при работе одновременно.",
                            Severity = Severity.Warning,
                            Recommendation = "Рекомендуется использовать либо NeoZapret (для DPI обхода), либо VPN (для полного шифрования и IP-блокировок), но не одновременно. Для IP-блокировок используйте VPN."
                        });
                    }

                    // Проверяем процессы VPN
                    var vpnProcesses = new Dictionary<string, string>
                    {
                        { "openvpn", "OpenVPN" },
                        { "vpn", "VPN Client" },
                        { "nordvpn", "NordVPN" },
                        { "surfshark", "Surfshark" },
                        { "expressvpn", "ExpressVPN" },
                        { "protonvpn", "ProtonVPN" },
                        { "windscribe", "Windscribe" }
                    };

                    foreach (var process in Process.GetProcesses())
                    {
                        try
                        {
                            var processName = process.ProcessName.ToLower();
                            if (vpnProcesses.ContainsKey(processName))
                            {
                                var vpnName = vpnProcesses[processName];
                                conflicts.Add(new ConflictInfo
                                {
                                    Type = ConflictType.VPN,
                                    Name = vpnName,
                                    Description = $"Обнаружен VPN клиент: {vpnName}. NeoZapret работает на уровне DPI и может конфликтовать с VPN.",
                                    Severity = Severity.Info,
                                    Recommendation = "VPN уже обеспечивает обход блокировок и шифрование. NeoZapret больше подходит для случаев, когда VPN недоступен или медленный."
                                });
                            }
                        }
                        catch { }
                        finally
                        {
                            process.Dispose();
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Warning("Ошибка при обнаружении VPN", ex);
            }

            return conflicts;
        }

        /// <summary>
        /// Обнаруживает файрволы.
        /// </summary>
        private static async Task<List<ConflictInfo>> DetectFirewall()
        {
            var conflicts = new List<ConflictInfo>();

            try
            {
                await Task.Run(() =>
                {
                    // Проверяем Windows Firewall
                    try
                    {
                        var proc = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "netsh",
                                Arguments = "advfirewall show allprofiles state",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                CreateNoWindow = true
                            }
                        };

                        proc.Start();
                        var output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit();

                        if (output.Contains("ON"))
                        {
                            conflicts.Add(new ConflictInfo
                            {
                                Type = ConflictType.Firewall,
                                Name = "Windows Firewall",
                                Description = "Windows Firewall активен. Может блокировать работу WinDivert.",
                                Severity = Severity.Warning,
                                Recommendation = "Добавьте winws.exe и NeoZapret.exe в исключения Windows Firewall."
                            });
                        }
                    }
                    catch { }
                });
            }
            catch (Exception ex)
            {
                Logger.Warning("Ошибка при обнаружении файрволов", ex);
            }

            return conflicts;
        }

        /// <summary>
        /// Обнаруживает настройки прокси.
        /// </summary>
        private static List<ConflictInfo> DetectProxy()
        {
            var conflicts = new List<ConflictInfo>();

            try
            {
                using (var registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Internet Settings"))
                {
                    if (registryKey != null)
                    {
                        var proxyEnabled = (int)registryKey.GetValue("ProxyEnable", 0) == 1;
                        var proxyServer = registryKey.GetValue("ProxyServer")?.ToString();

                        if (proxyEnabled && !string.IsNullOrEmpty(proxyServer))
                        {
                            conflicts.Add(new ConflictInfo
                            {
                                Type = ConflictType.Proxy,
                                Name = "Системный прокси",
                                Description = $"Обнаружен системный прокси: {proxyServer}. NeoZapret работает на более низком уровне и может конфликтовать.",
                                Severity = Severity.Info,
                                Recommendation = "NeoZapret работает на уровне пакетов (WinDivert) и не зависит от системного прокси. Убедитесь, что приложения используют прямой доступ."
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Ошибка при обнаружении прокси", ex);
            }

            return conflicts;
        }

        /// <summary>
        /// Проверяет блокировку портов.
        /// </summary>
        private static async Task<List<ConflictInfo>> DetectPortBlocking()
        {
            var conflicts = new List<ConflictInfo>();

            try
            {
                await Task.Run(() =>
                {
                    // Проверяем доступность стандартных портов
                    var testPorts = new[] { 443, 80, 53 };
                    
                    foreach (var port in testPorts)
                    {
                        try
                        {
                            using (var tcpClient = new System.Net.Sockets.TcpClient())
                            {
                                var connectTask = tcpClient.BeginConnect("8.8.8.8", port, null, null);
                                var success = connectTask.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
                                
                                if (!success)
                                {
                                    conflicts.Add(new ConflictInfo
                                    {
                                        Type = ConflictType.Firewall,
                                        Name = $"Порт {port} заблокирован",
                                        Description = $"Порт {port} может быть заблокирован файрволом или провайдером.",
                                        Severity = Severity.Warning,
                                        Recommendation = "Проверьте настройки файрвола и добавьте исключения для NeoZapret."
                                    });
                                    break; // Достаточно одного примера
                                }
                                
                                if (tcpClient.Connected)
                                {
                                    tcpClient.EndConnect(connectTask);
                                }
                            }
                        }
                        catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Warning("Ошибка при проверке портов", ex);
            }

            return conflicts;
        }

        /// <summary>
        /// Генерирует рекомендации на основе обнаруженных конфликтов.
        /// </summary>
        private static List<string> GenerateRecommendations(List<ConflictInfo> conflicts)
        {
            var recommendations = new List<string>();

            if (conflicts.Any(c => c.Type == ConflictType.Antivirus && c.Severity == Severity.Warning))
            {
                recommendations.Add("Добавьте NeoZapret и winws.exe в исключения антивируса");
            }

            if (conflicts.Any(c => c.Type == ConflictType.VPN))
            {
                recommendations.Add("⚠ Для IP-блокировок используйте VPN. NeoZapret обходит только DPI, но не может обойти полную IP-блокировку.");
                recommendations.Add("NeoZapret не шифрует трафик - провайдер видит, куда вы заходите. Для полной анонимности используйте VPN.");
            }

            if (conflicts.Any(c => c.Type == ConflictType.Firewall && c.Severity == Severity.Warning))
            {
                recommendations.Add("Добавьте NeoZapret в исключения Windows Firewall");
            }

            if (!conflicts.Any(c => c.Type == ConflictType.VPN))
            {
                recommendations.Add("💡 Для IP-блокировок, полного шифрования и анонимности рекомендуется использовать VPN");
            }

            if (recommendations.Count == 0)
            {
                recommendations.Add("✓ Конфликтов не обнаружено. Система готова к работе.");
            }

            return recommendations;
        }
    }
}

