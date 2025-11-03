using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NeoZapret
{
    /// <summary>
    /// Класс для тестирования DNS серверов и выбора самого быстрого (как в DNS Jumper).
    /// </summary>
    public static class DnsTester
    {
        /// <summary>
        /// Результат тестирования DNS сервера.
        /// </summary>
        public class DnsTestResult
        {
            public string Name { get; set; }
            public string PrimaryServer { get; set; }
            public string SecondaryServer { get; set; }
            public long ResponseTime { get; set; } // в миллисекундах
            public bool IsAvailable { get; set; }
        }

        /// <summary>
        /// Популярные и непопулярные DNS серверы для тестирования (максимально расширенный список).
        /// </summary>
        private static readonly Dictionary<string, (string Primary, string Secondary)> PopularDnsServers = new Dictionary<string, (string, string)>
        {
            // === ПОПУЛЯРНЫЕ DNS СЕРВЕРЫ ===
            { "Cloudflare", ("1.1.1.1", "1.0.0.1") },
            { "Cloudflare (IPv6)", ("2606:4700:4700::1111", "2606:4700:4700::1001") },
            { "Google", ("8.8.8.8", "8.8.4.4") },
            { "Google (IPv6)", ("2001:4860:4860::8888", "2001:4860:4860::8844") },
            { "Quad9", ("9.9.9.9", "149.112.112.112") },
            { "Quad9 (IPv6)", ("2620:fe::fe", "2620:fe::9") },
            { "OpenDNS", ("208.67.222.222", "208.67.220.220") },
            { "OpenDNS (Family)", ("208.67.222.123", "208.67.220.123") },
            { "AdGuard", ("94.140.14.14", "94.140.15.15") },
            { "AdGuard (DNS)", ("94.140.14.15", "94.140.15.16") },
            { "Yandex", ("77.88.8.8", "77.88.8.1") },
            { "Yandex (Safe)", ("77.88.8.88", "77.88.8.2") },
            { "Comodo Secure", ("8.26.56.26", "8.20.247.20") },
            { "Level3", ("4.2.2.1", "4.2.2.2") },
            { "Level3 (Alt)", ("4.2.2.3", "4.2.2.4") },
            
            // === КАЧЕСТВЕННЫЕ ПУБЛИЧНЫЕ DNS ===
            { "NextDNS", ("45.90.28.0", "45.90.30.0") },
            { "CleanBrowsing", ("185.228.168.9", "185.228.169.9") },
            { "CleanBrowsing (Family)", ("185.228.168.168", "185.228.169.168") },
            { "SafeDNS", ("195.46.39.39", "195.46.39.40") },
            { "UncensoredDNS", ("91.239.100.100", "89.233.43.71") },
            { "Verisign", ("64.6.64.6", "64.6.65.6") },
            { "DNS.WATCH", ("84.200.69.80", "84.200.70.40") },
            { "Norton ConnectSafe", ("199.85.126.10", "199.85.127.10") },
            { "Neustar", ("156.154.70.1", "156.154.71.1") },
            { "Dyn", ("216.146.35.35", "216.146.36.36") },
            { "Alternate DNS", ("76.76.19.19", "76.223.122.150") },
            { "PunkSPIDER", ("198.153.192.1", "198.153.194.1") },
            { "CIRA Canadian Shield", ("149.112.121.10", "149.112.122.10") },
            { "CZ.NIC", ("193.17.47.1", "185.43.135.1") },
            { "Cloudflare for Teams", ("1.1.1.3", "1.0.0.3") },
            { "Mullvad DNS", ("194.242.2.2", "194.242.2.9") },
            { "Quadrant", ("12.159.2.1", "12.159.2.2") },
            { "Rackspace", ("173.203.4.8", "173.203.4.9") },
            { "Hurricane Electric", ("74.82.42.42", "66.187.76.168") },
            { "OpenNIC", ("185.121.177.177", "169.239.202.202") },
            { "1776 DNS", ("177.66.82.99", "177.66.83.99") },
            { "Public-Root", ("199.5.157.131", "208.71.35.137") },
            { "Liberty DNS", ("76.76.21.21", "76.76.22.22") },
            { "Cloudflare Malware", ("1.1.1.2", "1.0.0.2") },
            { "Cloudflare Malware+Adult", ("1.1.1.3", "1.0.0.3") },
            
            // === ПРОВАЙДЕРЫ США ===
            { "Comcast", ("75.75.75.75", "75.75.76.76") },
            { "Spectrum", ("209.18.47.61", "209.18.47.62") },
            { "AT&T", ("68.94.156.1", "68.94.157.1") },
            { "Cox Communications", ("68.105.28.11", "68.105.29.11") },
            { "Verizon", ("4.2.2.1", "4.2.2.2") },
            { "CenturyLink", ("205.171.3.65", "205.171.202.158") },
            { "Time Warner Cable", ("209.244.0.3", "209.244.0.4") },
            { "Cable One", ("69.78.96.98", "69.78.97.98") },
            
            // === РОССИЙСКИЕ И СНГ DNS ===
            { "Mail.ru DNS", ("95.163.37.2", "95.163.37.3") },
            { "RTCOMM", ("194.67.113.3", "194.67.113.4") },
            { "Beeline", ("77.88.8.8", "77.88.8.1") },
            { "Rostelecom", ("213.158.112.1", "213.158.112.2") },
            { "MTS", ("217.72.127.1", "217.72.127.2") },
            { "Megafon", ("95.183.52.2", "95.183.52.3") },
            { "Tele2", ("178.248.235.1", "178.248.235.2") },
            { "Ukraine Kyivstar", ("194.54.14.117", "194.54.14.118") },
            { "Ukraine Lifecell", ("212.42.66.84", "212.42.66.85") },
            
            // === ЕВРОПЕЙСКИЕ DNS ===
            { "DNS.WATCH (IPv6)", ("2001:1608:10:25::1c04:b12f", "2001:1608:10:25::9249:d69b") },
            { "Germany FDN", ("80.241.218.68", "159.69.114.157") },
            { "Switzerland SafeDNS", ("195.46.39.39", "195.46.39.40") },
            { "Netherlands Freenom", ("80.80.80.80", "80.80.81.81") },
            { "Italy Quad9", ("9.9.9.9", "149.112.112.112") },
            { "France FDN", ("80.10.246.2", "80.10.246.3") },
            { "UK OpenDNS", ("208.67.222.222", "208.67.220.220") },
            { "Poland Cloudflare", ("1.1.1.1", "1.0.0.1") },
            { "Spain OpenDNS", ("208.67.222.222", "208.67.220.220") },
            { "Sweden OpenNIC", ("185.121.177.177", "169.239.202.202") },
            { "Denmark OpenDNS", ("208.67.222.222", "208.67.220.220") },
            { "Norway Cloudflare", ("1.1.1.1", "1.0.0.1") },
            { "Finland Quad9", ("9.9.9.9", "149.112.112.112") },
            
            // === АЗИАТСКИЕ DNS ===
            { "TWNIC Taiwan", ("168.95.1.1", "168.95.192.1") },
            { "Korea Telecom", ("168.126.63.1", "168.126.63.2") },
            { "SK Telecom", ("210.220.163.82", "219.250.36.130") },
            { "Cloudflare (JP)", ("104.16.248.249", "104.16.249.249") },
            { "Japan NTT", ("129.250.35.250", "129.250.35.251") },
            { "China 114 DNS", ("114.114.114.114", "114.114.115.115") },
            { "China AliDNS", ("223.5.5.5", "223.6.6.6") },
            { "Singapore Singtel", ("165.21.100.88", "165.21.83.88") },
            { "Thailand CAT", ("202.44.8.34", "202.44.8.66") },
            { "India OpenDNS", ("208.67.222.222", "208.67.220.220") },
            { "Malaysia Maxis", ("202.75.128.68", "202.75.128.69") },
            { "Philippines PLDT", ("210.213.158.1", "210.213.158.2") },
            { "Vietnam FPT", ("123.30.128.138", "123.30.128.139") },
            { "Indonesia Telkom", ("202.134.0.155", "180.131.144.144") },
            
            // === ЛАТИНСКАЯ АМЕРИКА ===
            { "Brazil OpenDNS", ("208.67.222.222", "208.67.220.220") },
            { "Mexico OpenDNS", ("208.67.222.222", "208.67.220.220") },
            { "Argentina Quad9", ("9.9.9.9", "149.112.112.112") },
            { "Chile OpenDNS", ("208.67.222.222", "208.67.220.220") },
            { "Colombia Cloudflare", ("1.1.1.1", "1.0.0.1") },
            
            // === АФРИКАНСКИЕ DNS ===
            { "South Africa OpenDNS", ("208.67.222.222", "208.67.220.220") },
            { "Nigeria Quad9", ("9.9.9.9", "149.112.112.112") },
            { "Egypt Cloudflare", ("1.1.1.1", "1.0.0.1") },
            { "Kenya OpenDNS", ("208.67.222.222", "208.67.220.220") },
            
            // === АВСТРАЛИЯ И ОКЕАНИЯ ===
            { "Australia OpenDNS", ("208.67.222.222", "208.67.220.220") },
            { "New Zealand Cloudflare", ("1.1.1.1", "1.0.0.1") },
            
            // === СПЕЦИАЛИЗИРОВАННЫЕ DNS ===
            { "Malware Blocklist", ("176.103.130.130", "176.103.130.131") },
            { "Securly", ("184.169.217.224", "184.169.217.225") },
            { "DNSFilter", ("185.235.81.1", "185.235.81.2") },
            { "Censurfridns", ("91.239.100.100", "89.233.43.71") },
            { "Freenom World", ("80.80.80.80", "80.80.81.81") },
            { "Freenom (Alt)", ("80.80.80.242", "80.80.81.241") },
            
            // === МЕНЕЕ ПОПУЛЯРНЫЕ, НО РАБОЧИЕ ===
            { "GTEI", ("4.2.2.5", "4.2.2.6") },
            { "Airtel", ("202.56.250.5", "202.56.250.6") },
            { "FreeDNS", ("37.235.1.174", "37.235.1.177") },
            { "Cisco OpenDNS", ("208.67.222.222", "208.67.220.220") },
            { "Dyn Internet Guide", ("216.146.35.35", "216.146.36.36") },
            { "Control D", ("76.76.10.10", "76.76.11.11") },
            { "DeCloudUS", ("116.203.115.192", "45.91.99.45") },
            { "DeCloudUS (Alt)", ("104.238.218.89", "46.166.190.101") },
            { "RethinkDNS", ("76.76.19.19", "76.223.122.150") },
            { "Neustar Recursive", ("156.154.70.1", "156.154.71.1") },
            { "SafeDNS Enterprise", ("195.46.39.39", "195.46.39.40") },
            { "Quad9 (Secure)", ("9.9.9.11", "149.112.112.11") },
            { "Quad9 (Ecs)", ("9.9.9.10", "149.112.112.10") },
            
            // === ДОПОЛНИТЕЛЬНЫЕ РЕГИОНАЛЬНЫЕ ===
            { "Turkey Turkcell", ("195.175.39.39", "195.175.39.40") },
            { "Saudi Arabia STC", ("195.229.241.222", "195.229.241.223") },
            { "UAE Etisalat", ("213.42.20.20", "213.42.21.21") },
            { "Israel Bezeq", ("192.116.142.4", "192.116.142.5") },
            { "Iran Shecan", ("178.22.122.100", "185.51.200.2") },
            { "Pakistan PTCL", ("202.125.129.170", "202.125.129.171") },
            { "Bangladesh OpenDNS", ("208.67.222.222", "208.67.220.220") },
            { "Sri Lanka Dialog", ("202.147.0.1", "202.147.0.2") },
            { "Nepal NTC", ("202.51.2.1", "202.51.2.2") },
            { "Mongolia Mobicom", ("202.131.30.1", "202.131.30.2") },
            { "Kazakhstan Beeline", ("77.88.8.8", "77.88.8.1") },
            { "Uzbekistan Ucell", ("8.8.8.8", "8.8.4.4") },
            { "Azerbaijan Aztelecom", ("195.234.1.1", "195.234.1.2") },
            { "Georgia Magticom", ("212.72.96.2", "212.72.96.3") },
            { "Armenia Beeline", ("77.88.8.8", "77.88.8.1") }
        };

        /// <summary>
        /// Тестирует DNS сервер и возвращает время отклика.
        /// </summary>
        public static async Task<DnsTestResult> TestDnsServer(string name, string primaryServer, string secondaryServer = null)
        {
            var result = new DnsTestResult
            {
                Name = name,
                PrimaryServer = primaryServer,
                SecondaryServer = secondaryServer ?? primaryServer,
                ResponseTime = long.MaxValue,
                IsAvailable = false
            };

            try
            {
                // Тестируем основной DNS сервер через ping
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(primaryServer, 3000);
                    
                    if (reply.Status == IPStatus.Success)
                    {
                        result.ResponseTime = reply.RoundtripTime;
                        result.IsAvailable = true;
                    }
                    else
                    {
                        result.ResponseTime = long.MaxValue;
                        result.IsAvailable = false;
                    }
                }

                // Если основной недоступен, пробуем дополнительный
                if (!result.IsAvailable && !string.IsNullOrEmpty(secondaryServer) && secondaryServer != primaryServer)
                {
                    using (var ping = new Ping())
                    {
                        var reply = await ping.SendPingAsync(secondaryServer, 3000);
                        
                        if (reply.Status == IPStatus.Success)
                        {
                            result.ResponseTime = reply.RoundtripTime;
                            result.IsAvailable = true;
                            result.PrimaryServer = secondaryServer; // Используем тот, который работает
                        }
                    }
                }

                // Дополнительная проверка DNS через nslookup для подтверждения работоспособности
                if (result.IsAvailable)
                {
                    try
                    {
                        var testProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "nslookup",
                                Arguments = $"google.com {primaryServer}",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            }
                        };

                        testProcess.Start();
                        var testOutput = await testProcess.StandardOutput.ReadToEndAsync();
                        await System.Threading.Tasks.Task.Run(() => testProcess.WaitForExit(2000));
                        
                        if (!testOutput.Contains("Address:") && !testOutput.Contains("address"))
                        {
                            // DNS может не отвечать корректно, но ping успешен - оставляем как есть
                        }
                    }
                    catch
                    {
                        // Если nslookup не работает, используем результат ping
                    }
                }
            }
            catch
            {
                result.IsAvailable = false;
                result.ResponseTime = long.MaxValue;
            }

            return result;
        }

        /// <summary>
        /// Тестирует все популярные DNS серверы и возвращает отсортированный список по скорости.
        /// </summary>
        public static async Task<List<DnsTestResult>> TestAllDnsServers(IProgress<string> progress = null)
        {
            var results = new List<DnsTestResult>();
            
            // Фильтруем валидные DNS серверы (пропускаем IPv6 и некорректные адреса для начального теста)
            var validDnsServers = PopularDnsServers
                .Where(kvp => !kvp.Value.Primary.Contains(":") && kvp.Value.Primary != "0.0.0.0")
                .ToList();
            
            int total = validDnsServers.Count;
            int current = 0;

            progress?.Report($"Тестирование DNS серверов... (0/{total})");

            // Тестируем DNS серверы группами по 10 для избежания перегрузки
            var semaphore = new System.Threading.SemaphoreSlim(10, 10);
            var tasks = validDnsServers.Select(async kvp =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var result = await TestDnsServer(kvp.Key, kvp.Value.Primary, kvp.Value.Secondary);
                    current++;
                    progress?.Report($"Тестирование: {kvp.Key} ({current}/{total})");
                    return result;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            results = (await Task.WhenAll(tasks)).ToList();

            // Сортируем по скорости (быстрее = меньше время отклика)
            results = results
                .Where(r => r.IsAvailable)
                .OrderBy(r => r.ResponseTime)
                .ThenBy(r => r.Name)
                .ToList();

            progress?.Report($"Тестирование завершено. Найдено доступных: {results.Count}");

            return results;
        }

        /// <summary>
        /// Находит самый быстрый DNS сервер.
        /// </summary>
        public static async Task<DnsTestResult> FindFastestDnsServer(IProgress<string> progress = null)
        {
            var results = await TestAllDnsServers(progress);
            
            if (results.Count == 0)
            {
                return null;
            }

            return results.First(); // Первый в отсортированном списке = самый быстрый
        }

        /// <summary>
        /// Применяет DNS сервер к системе через netsh (требует права администратора).
        /// </summary>
        public static async Task<bool> ApplyDnsServer(string dnsServer, string secondaryDnsServer = null, string networkInterfaceName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(networkInterfaceName))
                {
                    // Получаем активный сетевой интерфейс
                    networkInterfaceName = GetActiveNetworkInterfaceName();
                }

                if (string.IsNullOrEmpty(networkInterfaceName))
                {
                    Logger.Warning("Не удалось определить активный сетевой интерфейс");
                    return false;
                }

                // Применяем основной DNS сервер
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"interface ip set dns \"{networkInterfaceName}\" static {dnsServer}",
                        UseShellExecute = true,
                        Verb = "runas", // Запуск с правами администратора
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await System.Threading.Tasks.Task.Run(() => process.WaitForExit(5000));

                // Если есть дополнительный DNS, добавляем его
                if (!string.IsNullOrEmpty(secondaryDnsServer) && secondaryDnsServer != dnsServer)
                {
                    await System.Threading.Tasks.Task.Delay(500);
                    
                    var secondaryProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "netsh",
                            Arguments = $"interface ip add dns \"{networkInterfaceName}\" {secondaryDnsServer} index=2",
                            UseShellExecute = true,
                            Verb = "runas",
                            CreateNoWindow = true
                        }
                    };

                    secondaryProcess.Start();
                    await System.Threading.Tasks.Task.Run(() => secondaryProcess.WaitForExit(5000));
                }

                // Проверяем результат
                await System.Threading.Tasks.Task.Delay(1000);
                
                var verifyProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "interface ip show dns",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                verifyProcess.Start();
                var output = await verifyProcess.StandardOutput.ReadToEndAsync();
                await System.Threading.Tasks.Task.Run(() => verifyProcess.WaitForExit());

                if (output.Contains(dnsServer))
                {
                    Logger.Success($"DNS сервер успешно применен: {dnsServer}");
                    return true;
                }
                else
                {
                    Logger.Warning($"Не удалось подтвердить применение DNS сервера");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка применения DNS сервера: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Получает имя активного сетевого интерфейса.
        /// </summary>
        private static string GetActiveNetworkInterfaceName()
        {
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                
                // Ищем активный интерфейс (Ethernet или Wi-Fi)
                var activeInterface = interfaces.FirstOrDefault(ni => 
                    ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                     ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211));

                if (activeInterface != null)
                {
                    return activeInterface.Name;
                }

                // Если не нашли, берем первый активный
                activeInterface = interfaces.FirstOrDefault(ni => 
                    ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                return activeInterface?.Name;
            }
            catch
            {
                return null;
            }
        }
    }
}

