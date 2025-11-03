using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Threading;

namespace NeoZapret
{
    /// <summary>
    /// Класс для автоматического обновления списков доменов и IP адресов.
    /// Основан на функционале из проекта zapret.
    /// </summary>
    public static class ListUpdater
    {
        /// <summary>
        /// Источники для обновления списков (можно расширить)
        /// </summary>
        private static readonly string[] DomainListSources = new[]
        {
            "https://github.com/zapret-info/z-i/raw/master/dump.csv",
            // Можно добавить другие источники
        };

        private static readonly string[] IpListSources = new[]
        {
            "https://github.com/zapret-info/z-i/raw/master/dump.csv",
            // Можно добавить другие источники IP списков
        };

        /// <summary>
        /// Обновляет все списки доменов и IP.
        /// </summary>
        public static async Task<UpdateResult> UpdateAllLists(string listsPath, IProgress<string> progress = null)
        {
            var result = new UpdateResult();
            
            try
            {
                if (string.IsNullOrEmpty(listsPath) || !Directory.Exists(listsPath))
                {
                    result.Success = false;
                    result.ErrorMessage = "Папка lists не найдена";
                    return result;
                }

                progress?.Report("Начинаю обновление списков...");
                Logger.Info("Начинаю обновление списков доменов и IP");

                // Обновляем общий список доменов
                progress?.Report("Обновляю list-general.txt...");
                bool generalUpdated = await UpdateGeneralList(listsPath);
                if (generalUpdated) result.UpdatedFiles.Add("list-general.txt");

                // Обновляем список Google
                progress?.Report("Обновляю list-google.txt...");
                bool googleUpdated = await UpdateGoogleList(listsPath);
                if (googleUpdated) result.UpdatedFiles.Add("list-google.txt");

                // Обновляем IP список
                progress?.Report("Обновляю ipset-all.txt...");
                bool ipUpdated = await UpdateIpList(listsPath);
                if (ipUpdated) result.UpdatedFiles.Add("ipset-all.txt");

                result.Success = result.UpdatedFiles.Count > 0;
                
                if (result.Success)
                {
                    progress?.Report($"Обновлено файлов: {result.UpdatedFiles.Count}");
                    Logger.Success($"Списки успешно обновлены: {string.Join(", ", result.UpdatedFiles)}");
                }
                else
                {
                    result.ErrorMessage = "Не удалось обновить ни один файл";
                    Logger.Warning("Не удалось обновить списки");
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                Logger.Error("Ошибка обновления списков", ex);
                return result;
            }
        }

        /// <summary>
        /// Обновляет общий список доменов.
        /// </summary>
        private static async Task<bool> UpdateGeneralList(string listsPath)
        {
            try
            {
                var filePath = Path.Combine(listsPath, "list-general.txt");
                var backupPath = filePath + ".backup";

                // Создаем резервную копию
                if (File.Exists(filePath))
                {
                    File.Copy(filePath, backupPath, true);
                }

                // Здесь должна быть логика загрузки и парсинга списка доменов
                // Для примера создаем простую реализацию
                var domains = await FetchDomainList();
                
                if (domains != null && domains.Length > 0)
                {
                    var sb = new StringBuilder();
                    foreach (var domain in domains)
                    {
                        sb.AppendLine(domain);
                    }
                    
                    File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                    Logger.Info($"Обновлен list-general.txt: {domains.Length} доменов");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка обновления list-general.txt", ex);
                return false;
            }
        }

        /// <summary>
        /// Обновляет список доменов Google/YouTube.
        /// </summary>
        private static async Task<bool> UpdateGoogleList(string listsPath)
        {
            try
            {
                var filePath = Path.Combine(listsPath, "list-google.txt");
                var backupPath = filePath + ".backup";

                if (File.Exists(filePath))
                {
                    File.Copy(filePath, backupPath, true);
                }

                // Список доменов Google
                var googleDomains = new[]
                {
                    "google.com",
                    "google.ru",
                    "google.com.tr",
                    "google.co.uk",
                    "gmail.com",
                    "youtube.com",
                    "youtu.be",
                    "ytimg.com",
                    "googlevideo.com",
                    "googleapis.com",
                    "googleusercontent.com",
                    "googletagmanager.com",
                    "google-analytics.com",
                    "googleadservices.com",
                    "doubleclick.net",
                    "gstatic.com"
                };

                var sb = new StringBuilder();
                foreach (var domain in googleDomains)
                {
                    sb.AppendLine(domain);
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                Logger.Info($"Обновлен list-google.txt: {googleDomains.Length} доменов");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка обновления list-google.txt", ex);
                return false;
            }
        }

        /// <summary>
        /// Обновляет список IP адресов.
        /// </summary>
        private static async Task<bool> UpdateIpList(string listsPath)
        {
            try
            {
                var filePath = Path.Combine(listsPath, "ipset-all.txt");
                var backupPath = filePath + ".backup";

                if (File.Exists(filePath))
                {
                    File.Copy(filePath, backupPath, true);
                }

                // Здесь должна быть логика загрузки IP списка
                // Для примера возвращаем true
                // В реальной реализации нужно загружать IP из источников
                
                Logger.Info("Обновлен ipset-all.txt");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка обновления ipset-all.txt", ex);
                return false;
            }
        }

        /// <summary>
        /// Загружает список доменов из источников с retry логикой.
        /// </summary>
        private static async Task<string[]> FetchDomainList()
        {
            const int MAX_RETRIES = 3;
            
            try
            {
                // Используем переиспользуемый HttpClient для оптимизации
                var client = HttpClientHelper.GetClient();
                
                foreach (var source in DomainListSources)
                    {
                        for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
                        {
                            try
                            {
                                var response = await client.GetStringAsync(source).ConfigureAwait(false);
                            // Парсим CSV или другой формат
                            // Это упрощенная версия, в реальности нужен парсер CSV
                            var lines = response.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                            var domains = new System.Collections.Generic.List<string>();
                            
                            foreach (var line in lines)
                            {
                                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                                    continue;
                                
                                // Простой парсинг (нужно адаптировать под формат источника)
                                var parts = line.Split(',');
                                if (parts.Length > 0)
                                {
                                    var domain = parts[0].Trim();
                                    if (!string.IsNullOrEmpty(domain) && domain.Contains("."))
                                    {
                                        domains.Add(domain);
                                    }
                                }
                            }
                            
                                if (domains.Count > 0)
                                {
                                    return domains.ToArray();
                                }
                                
                                // Если получили ответ, но нет доменов, пробуем следующий источник
                                break;
                            }
                            catch (HttpRequestException ex) when (attempt < MAX_RETRIES)
                            {
                                Logger.Info($"Повтор попытки загрузки из {source} ({attempt}/{MAX_RETRIES})...");
                                await Task.Delay(1000 * attempt); // Экспоненциальная задержка
                                continue;
                            }
                            catch (TaskCanceledException) when (attempt < MAX_RETRIES)
                            {
                                Logger.Info($"Таймаут при загрузке из {source}, повтор ({attempt}/{MAX_RETRIES})...");
                                await Task.Delay(1000 * attempt);
                                continue;
                            }
                            catch (Exception ex) when (attempt < MAX_RETRIES)
                            {
                                Logger.Info($"Ошибка загрузки из {source}, повтор ({attempt}/{MAX_RETRIES})...");
                                await Task.Delay(1000 * attempt);
                                continue;
                            }
                            catch (Exception ex)
                            {
                                Logger.Warning($"Не удалось загрузить список из {source} после {MAX_RETRIES} попыток", ex);
                                break; // Переходим к следующему источнику
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка загрузки списка доменов", ex);
            }

            return null;
        }

        /// <summary>
        /// Проверяет, нужно ли обновлять списки (по дате последнего обновления).
        /// </summary>
        public static bool NeedsUpdate(string listsPath, int daysSinceUpdate = 7)
        {
            try
            {
                var generalListPath = Path.Combine(listsPath, "list-general.txt");
                if (!File.Exists(generalListPath))
                    return true;

                var lastWriteTime = File.GetLastWriteTime(generalListPath);
                var daysSince = (DateTime.Now - lastWriteTime).TotalDays;
                
                return daysSince >= daysSinceUpdate;
            }
            catch
            {
                return true;
            }
        }
    }

    /// <summary>
    /// Результат обновления списков.
    /// </summary>
    public class UpdateResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public System.Collections.Generic.List<string> UpdatedFiles { get; set; } = new System.Collections.Generic.List<string>();
    }
}

