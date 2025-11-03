using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Linq;

namespace NeoZapret
{
    /// <summary>
    /// Класс для определения интернет-провайдера пользователя.
    /// Используется для автоматического выбора оптимальной стратегии обхода.
    /// </summary>
    public static class ProviderDetector
    {
        /// <summary>
        /// Типы провайдеров в России.
        /// </summary>
        public enum ProviderType
        {
            Unknown,
            Rostelecom,    // Ростелеком
            MTS,           // МТС
            Beeline,       // Билайн
            Megafon,       // Мегафон
            Tele2,         // Теле2
            TTK,           // ТТК (ТрансТелеКом)
            ERTelecom,     // Эр-Телеком
            Akado,         // Акадо
            DomRu,         // Дом.ру
            Yota,          // Yota
            Other          // Другие провайдеры
        }

        /// <summary>
        /// IP-диапазоны и ASN для определения провайдеров (основные в России).
        /// </summary>
        private static readonly (string[] Ranges, ProviderType Provider)[] ProviderRanges = new[]
        {
            // Ростелеком
            (new[] { "123.231.", "188.64.", "188.186.", "46.17.", "95.153." }, ProviderType.Rostelecom),
            
            // МТС
            (new[] { "178.176.", "178.154.", "213.87.", "217.66.", "31.129.", "37.9.", "88.200." }, ProviderType.MTS),
            
            // Билайн
            (new[] { "31.31.", "37.139.", "37.220.", "62.109.", "87.250.", "185.32." }, ProviderType.Beeline),
            
            // Мегафон
            (new[] { "31.40.", "31.41.", "178.18.", "178.19.", "178.20.", "178.21.", "188.93." }, ProviderType.Megafon),
            
            // Теле2
            (new[] { "31.148.", "37.228.", "46.151.", "95.24.", "178.172." }, ProviderType.Tele2),
            
            // ТТК
            (new[] { "178.159.", "188.64.", "195.98." }, ProviderType.TTK),
            
            // Эр-Телеком
            (new[] { "188.162.", "94.19." }, ProviderType.ERTelecom),
        };

        /// <summary>
        /// Определяет провайдера на основе внешнего IP адреса.
        /// </summary>
        public static async Task<ProviderType> DetectProviderAsync()
        {
            try
            {
                // Получаем внешний IP через несколько сервисов
                string externalIp = await GetExternalIpAsync();
                
                if (string.IsNullOrEmpty(externalIp))
                {
                    Logger.Warning("Не удалось определить внешний IP");
                    return ProviderType.Unknown;
                }

                Logger.Info($"Внешний IP: {externalIp}");

                // Проверяем по известным диапазонам
                foreach (var (ranges, provider) in ProviderRanges)
                {
                    foreach (var range in ranges)
                    {
                        if (externalIp.StartsWith(range, StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.Success($"Определен провайдер: {provider} по IP {externalIp}");
                            return provider;
                        }
                    }
                }

                // Альтернативный метод: проверка по имени сетевого адаптера
                var providerFromAdapter = DetectProviderFromAdapter();
                if (providerFromAdapter != ProviderType.Unknown)
                {
                    return providerFromAdapter;
                }

                Logger.Info("Провайдер не определен, используется Unknown");
                return ProviderType.Unknown;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Ошибка определения провайдера: {ex.Message}");
                return ProviderType.Unknown;
            }
        }

        /// <summary>
        /// Получает внешний IP адрес через публичные сервисы.
        /// </summary>
        private static async Task<string> GetExternalIpAsync()
        {
            var services = new[]
            {
                "https://api.ipify.org",
                "https://icanhazip.com",
                "https://ifconfig.me/ip",
                "https://ipecho.net/plain"
            };

            foreach (var service in services)
            {
                try
                {
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(5);
                        var response = await client.GetStringAsync(service);
                        var ip = response.Trim();
                        
                        if (IPAddress.TryParse(ip, out _))
                        {
                            return ip;
                        }
                    }
                }
                catch
                {
                    // Пробуем следующий сервис
                    continue;
                }
            }

            return null;
        }

        /// <summary>
        /// Определяет провайдера по имени сетевого адаптера (резервный метод).
        /// </summary>
        private static ProviderType DetectProviderFromAdapter()
        {
            try
            {
                var adapters = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(a => a.OperationalStatus == OperationalStatus.Up &&
                               a.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .ToList();

                foreach (var adapter in adapters)
                {
                    var description = adapter.Description.ToLower();
                    
                    if (description.Contains("rostelecom") || description.Contains("rt"))
                        return ProviderType.Rostelecom;
                    
                    if (description.Contains("mts"))
                        return ProviderType.MTS;
                    
                    if (description.Contains("beeline") || description.Contains("вымпел"))
                        return ProviderType.Beeline;
                    
                    if (description.Contains("megafon") || description.Contains("мегафон"))
                        return ProviderType.Megafon;
                    
                    if (description.Contains("tele2"))
                        return ProviderType.Tele2;
                    
                    if (description.Contains("ttk") || description.Contains("ттк"))
                        return ProviderType.TTK;
                    
                    if (description.Contains("er-telecom") || description.Contains("эр-телеком"))
                        return ProviderType.ERTelecom;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Ошибка определения провайдера по адаптеру: {ex.Message}");
            }

            return ProviderType.Unknown;
        }

        /// <summary>
        /// Получает рекомендуемую стратегию для указанного провайдера.
        /// </summary>
        public static string GetRecommendedStrategy(ProviderType provider)
        {
            switch (provider)
            {
                case ProviderType.Rostelecom:
                    return "rostelecom"; // Ростелеком часто требует multisplit
                case ProviderType.MTS:
                    return "mts"; // МТС хорошо работает с fake+fakedsplit
                case ProviderType.Beeline:
                    return "beeline"; // Билайн требует комбинированные методы
                case ProviderType.Megafon:
                    return "megafon"; // Мегафон использует жесткие блокировки
                case ProviderType.Tele2:
                    return "tele2"; // Теле2 требует агрессивные методы
                case ProviderType.TTK:
                    return "ttk"; // ТТК требует специальную настройку
                case ProviderType.ERTelecom:
                    return "ertelecom"; // Эр-Телеком
                default:
                    return "recommended"; // По умолчанию
            }
        }

        /// <summary>
        /// Получает название провайдера для отображения.
        /// </summary>
        public static string GetProviderName(ProviderType provider)
        {
            switch (provider)
            {
                case ProviderType.Rostelecom: return "Ростелеком";
                case ProviderType.MTS: return "МТС";
                case ProviderType.Beeline: return "Билайн";
                case ProviderType.Megafon: return "Мегафон";
                case ProviderType.Tele2: return "Теле2";
                case ProviderType.TTK: return "ТТК";
                case ProviderType.ERTelecom: return "Эр-Телеком";
                case ProviderType.Akado: return "Акадо";
                case ProviderType.DomRu: return "Дом.ру";
                case ProviderType.Yota: return "Yota";
                case ProviderType.Unknown: return "Неизвестный";
                default: return "Другой";
            }
        }
    }
}
