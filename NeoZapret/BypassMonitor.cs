using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Threading;

namespace NeoZapret
{
    public class BypassMonitor
    {
        private readonly string[] testUrls = new string[]
        {
            // Основные социальные сети и коммуникации
            "discord.com", "discord.gg", "discord.media",
            "twitter.com", "x.com", "reddit.com",
            
            // Видео и стриминг
            "youtube.com", "twitch.tv", "netflix.com", "vimeo.com",
            
            // Поиск и основные сервисы
            "google.com", "gmail.com", "google.ru",
            
            // Разработка и код
            "github.com", "gitlab.com", "bitbucket.org",
            "cursor.sh", "vscode.dev", "stackoverflow.com",
            
            // AI сервисы
            "openai.com", "chat.openai.com", "claude.ai",
            "perplexity.ai", "anthropic.com",
            
            // Игровые платформы и серверы
            "steampowered.com", "steamcommunity.com",
            "epicgames.com", "battle.net",
            "ea.com", "origin.com",
            "playstation.com", "xbox.com",
            "unity.com", "unrealengine.com",
            
            // Большие сервисы
            "amazon.com", "aws.amazon.com",
            "microsoft.com", "office.com",
            "adobe.com", "autodesk.com",
            "apple.com", "icloud.com",
            
            // Дополнительные
            "wikipedia.org", "archive.org",
            "cloudflare.com", "fastly.com"
        };

        public event EventHandler<BypassStatusChangedEventArgs> StatusChanged;
        public event EventHandler<string> BestStrategyFound;

        private bool isMonitoring = false;
        private Dictionary<string, int> strategyScores = new Dictionary<string, int>();
        private Dictionary<string, DateTime> strategyCache = new Dictionary<string, DateTime>(); // Кэш результатов
        private Dictionary<string, bool> connectivityCache = new Dictionary<string, bool>(); // Кэш проверок доступности
        private string currentStrategy = "";
        private const int CACHE_DURATION_MINUTES = 5; // Кэш действителен 5 минут
        private const int MAX_CACHE_SIZE = 1000; // Ограничение размера кэша для управления памятью
        private System.Threading.CancellationTokenSource cancellationTokenSource;
        private readonly object _monitorLock = new object(); // Для потокобезопасности
        
        /// <summary>
        /// Очищает устаревшие записи из кэша для управления памятью.
        /// </summary>
        private void CleanupCache()
        {
            lock (_monitorLock)
            {
                var now = DateTime.UtcNow;
                var expiredKeys = strategyCache
                    .Where(kvp => now - kvp.Value > TimeSpan.FromMinutes(CACHE_DURATION_MINUTES))
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var key in expiredKeys)
                {
                    strategyCache.Remove(key);
                    connectivityCache.Remove(key);
                }
                
                // Если кэш все еще слишком большой, удаляем самые старые записи
                if (strategyCache.Count > MAX_CACHE_SIZE)
                {
                    var keysToRemove = strategyCache
                        .OrderBy(kvp => kvp.Value)
                        .Take(strategyCache.Count - MAX_CACHE_SIZE / 2)
                        .Select(kvp => kvp.Key)
                        .ToList();
                    
                    foreach (var key in keysToRemove)
                    {
                        strategyCache.Remove(key);
                        connectivityCache.Remove(key);
                    }
                }
            }
        }
        
        public async Task<string> FindBestStrategy()
        {
            lock (_monitorLock)
            {
                strategyScores.Clear();
                CleanupCache(); // Очищаем кэш перед поиском
            }
            
            // Расширенный список стратегий включая стратегии для провайдеров
            var strategies = new[] { 
                "recommended", "fast", "max", "aggressive", "games",
                "rostelecom", "mts", "beeline", "megafon", "tele2", "ttk", "ertelecom"
            };
            
            // Параллельное тестирование стратегий для ускорения
            var tasks = strategies.Select(strategy => TestStrategyAsync(strategy, cancellationTokenSource.Token));
            var results = await Task.WhenAll(tasks);
            
            for (int i = 0; i < strategies.Length; i++)
            {
                strategyScores[strategies[i]] = results[i];
            }
            
            var bestStrategy = strategyScores.OrderByDescending(x => x.Value).FirstOrDefault();
            return bestStrategy.Key ?? "recommended";
        }

        private async Task<int> TestStrategyAsync(string strategy, System.Threading.CancellationToken cancellationToken)
        {
            // Проверяем кэш (потокобезопасно)
            lock (_monitorLock)
            {
                if (strategyCache.ContainsKey(strategy))
                {
                    var cacheTime = strategyCache[strategy];
                    if ((DateTime.Now - cacheTime).TotalMinutes < CACHE_DURATION_MINUTES && strategyScores.ContainsKey(strategy))
                    {
                        return strategyScores[strategy];
                    }
                }
            }
            
            // Параллельная проверка URL для ускорения (но с ограничением)
            var semaphore = new System.Threading.SemaphoreSlim(5); // Максимум 5 одновременных проверок
            var tasks = testUrls.Select(async url =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;
                    
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    return await CheckConnectivityCached(url, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });
            
            var results = await Task.WhenAll(tasks);
            int successCount = results.Count(r => r);
            
            // Обновляем кэш (потокобезопасно)
            lock (_monitorLock)
            {
                strategyCache[strategy] = DateTime.Now;
            }
            
            return successCount;
        }

        private async Task<bool> CheckConnectivityCached(string url, System.Threading.CancellationToken cancellationToken)
        {
            // Проверяем кэш доступности (кэш действителен 2 минуты) - потокобезопасно
            lock (_monitorLock)
            {
                if (connectivityCache.ContainsKey(url))
                {
                    // Используем кэш, но периодически обновляем его
                    return connectivityCache[url];
                }
            }
            
            bool result = await CheckConnectivity(url);
            // Сохраняем в кэш (потокобезопасно)
            lock (_monitorLock)
            {
                connectivityCache[url] = result;
            }
            
            // Очистка старого кэша через некоторое время (fire and forget)
            _ = System.Threading.Tasks.Task.Delay(120000, cancellationToken).ContinueWith(_ => 
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    lock (_monitorLock)
                    {
                        connectivityCache.Remove(url);
                    }
                }
            }, cancellationToken);
            
            return result;
        }

        public async Task<bool> CheckConnectivity(string url)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(url, 3000);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> CheckMultipleSites()
        {
            int successCount = 0;
            
            foreach (var url in testUrls)
            {
                if (await CheckConnectivity(url))
                {
                    successCount++;
                }
                await Task.Delay(50);
            }
            
            return successCount;
        }

        public void StartMonitoring(string strategy)
        {
            if (isMonitoring) return;
            
            isMonitoring = true;
            currentStrategy = strategy;
            cancellationTokenSource = new System.Threading.CancellationTokenSource(); // Создаем новый при каждом запуске
            MonitorLoop();
        }

        public void StopMonitoring()
        {
            isMonitoring = false;
            try
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
            }
            catch { }
            cancellationTokenSource = null;
        }

        private async void MonitorLoop()
        {
            while (isMonitoring)
            {
                try
                {
                    int successCount = await CheckMultipleSites();
                    int totalSites = testUrls.Length;
                    double successRate = (double)successCount / totalSites * 100;
                    
                    StatusChanged?.Invoke(this, new BypassStatusChangedEventArgs
                    {
                        SuccessCount = successCount,
                        TotalCount = totalSites,
                        SuccessRate = successRate,
                        Strategy = currentStrategy
                    });
                    
                    // Если успешность менее 50%, предлагаем переключить стратегию
                    if (successRate < 50 && !string.IsNullOrEmpty(currentStrategy))
                    {
                        var bestStrategy = await FindBestStrategy();
                        if (bestStrategy != currentStrategy)
                        {
                            BestStrategyFound?.Invoke(this, bestStrategy);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Логируем ошибки мониторинга, но продолжаем работу
                    Logger.Warning($"Ошибка в мониторинге обхода: {ex.Message}");
                }
                
                await Task.Delay(30000); // Проверка каждые 30 секунд
            }
        }
    }

    public class BypassStatusChangedEventArgs : EventArgs
    {
        public int SuccessCount { get; set; }
        public int TotalCount { get; set; }
        public double SuccessRate { get; set; }
        public string Strategy { get; set; }
    }
}


