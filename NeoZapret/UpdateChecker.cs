using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace NeoZapret
{
    /// <summary>
    /// Класс для проверки обновлений приложения.
    /// Проверяет наличие новых версий на GitHub или других источниках.
    /// </summary>
    public static class UpdateChecker
    {
        private const string CurrentVersion = "3.1.0";
        private const string VersionCheckUrl = "https://api.github.com/repos/SoulXel/NeoZapret/releases/latest";
        public const string GitHubReleasesUrl = "https://github.com/SoulXel/NeoZapret/releases/latest";

        /// <summary>
        /// Проверяет наличие новой версии приложения.
        /// </summary>
        /// <param name="silent">Если true, не логирует операцию</param>
        /// <returns>Информация об обновлении или null, если обновлений нет</returns>
        public static async Task<UpdateInfo> CheckForUpdates(bool silent = false)
        {
            const int MAX_RETRIES = 3;
            const int RETRY_DELAY_MS = 1000;
            
            for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
            {
                try
                {
                    if (!silent && attempt == 1)
                    {
                        Logger.Info("Проверяю наличие обновлений...");
                    }

                    // Используем переиспользуемый HttpClient для оптимизации
                    var client = HttpClientHelper.GetClient();
                    var response = await client.GetStringAsync(VersionCheckUrl);
                    
                    // Простой парсинг JSON (для полной реализации лучше использовать Newtonsoft.Json)
                    var versionMatch = Regex.Match(response, @"""tag_name""\s*:\s*""([^""]+)""");
                    var nameMatch = Regex.Match(response, @"""name""\s*:\s*""([^""]+)""");
                    var bodyMatch = Regex.Match(response, @"""body""\s*:\s*""([^""]+)""", RegexOptions.Singleline);
                    var urlMatch = Regex.Match(response, @"""html_url""\s*:\s*""([^""]+)""");

                    if (versionMatch.Success)
                    {
                        var latestVersion = versionMatch.Groups[1].Value.TrimStart('v');
                        var name = nameMatch.Success ? nameMatch.Groups[1].Value : "";
                        var body = bodyMatch.Success ? bodyMatch.Groups[1].Value : "";
                        var url = urlMatch.Success ? urlMatch.Groups[1].Value : GitHubReleasesUrl;

                        if (IsNewerVersion(latestVersion, CurrentVersion))
                        {
                            if (!silent)
                            {
                                Logger.Info($"Найдена новая версия: {latestVersion} (текущая: {CurrentVersion})");
                            }

                            return new UpdateInfo
                            {
                                IsUpdateAvailable = true,
                                LatestVersion = latestVersion,
                                CurrentVersion = CurrentVersion,
                                ReleaseName = name,
                                ReleaseNotes = body,
                                DownloadUrl = url
                            };
                        }
                        else
                        {
                            if (!silent)
                            {
                                Logger.Info($"Установлена последняя версия: {CurrentVersion}");
                            }
                            
                            return new UpdateInfo
                            {
                                IsUpdateAvailable = false,
                                CurrentVersion = CurrentVersion
                            };
                        }
                    }
                    }
                }
                catch (HttpRequestException ex)
                {
                    if (attempt < MAX_RETRIES)
                    {
                        if (!silent)
                            Logger.Info($"Повтор попытки проверки обновлений ({attempt}/{MAX_RETRIES})...");
                        await Task.Delay(RETRY_DELAY_MS * attempt);
                        continue;
                    }
                    Logger.Warning("Не удалось проверить обновления (проблема с сетью)", ex);
                }
                catch (TaskCanceledException)
                {
                    if (attempt < MAX_RETRIES)
                    {
                        if (!silent)
                            Logger.Info($"Повтор попытки проверки обновлений ({attempt}/{MAX_RETRIES})...");
                        await Task.Delay(RETRY_DELAY_MS * attempt);
                        continue;
                    }
                    Logger.Warning("Таймаут при проверке обновлений");
                }
                catch (Exception ex)
                {
                    if (attempt < MAX_RETRIES)
                    {
                        if (!silent)
                            Logger.Info($"Повтор попытки проверки обновлений ({attempt}/{MAX_RETRIES})...");
                        await Task.Delay(RETRY_DELAY_MS * attempt);
                        continue;
                    }
                    Logger.Warning("Ошибка при проверке обновлений", ex);
                }

                return new UpdateInfo
                {
                    IsUpdateAvailable = false,
                    CurrentVersion = CurrentVersion
                };
            }

            return new UpdateInfo
            {
                IsUpdateAvailable = false,
                CurrentVersion = CurrentVersion
            };
        }

        /// <summary>
        /// Сравнивает версии и определяет, является ли новая версия более новой.
        /// </summary>
        private static bool IsNewerVersion(string newVersion, string currentVersion)
        {
            try
            {
                // Убираем префикс "v" если есть
                newVersion = newVersion.TrimStart('v', 'V');
                currentVersion = currentVersion.TrimStart('v', 'V');

                var newParts = newVersion.Split('.');
                var currentParts = currentVersion.Split('.');

                int maxLength = Math.Max(newParts.Length, currentParts.Length);

                for (int i = 0; i < maxLength; i++)
                {
                    int newPart = i < newParts.Length && int.TryParse(newParts[i], out int n) ? n : 0;
                    int currentPart = i < currentParts.Length && int.TryParse(currentParts[i], out int c) ? c : 0;

                    if (newPart > currentPart)
                        return true;
                    if (newPart < currentPart)
                        return false;
                }

                return false; // Версии одинаковые
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Информация об обновлении.
    /// </summary>
    public class UpdateInfo
    {
        public bool IsUpdateAvailable { get; set; }
        public string LatestVersion { get; set; }
        public string CurrentVersion { get; set; }
        public string ReleaseName { get; set; }
        public string ReleaseNotes { get; set; }
        public string DownloadUrl { get; set; }
    }
}

