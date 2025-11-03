using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NeoZapret
{
    /// <summary>
    /// Статистика использования стратегий обхода.
    /// Сохраняет данные о производительности стратегий для рекомендаций.
    /// </summary>
    public class BypassStatistics
    {
        private readonly string statsFilePath;
        private Dictionary<string, StrategyStats> strategyStats;

        public BypassStatistics(string appPath)
        {
            try
            {
                var statsDir = Path.Combine(appPath ?? ".", "statistics");
                if (!Directory.Exists(statsDir))
                {
                    Directory.CreateDirectory(statsDir);
                }
                
                statsFilePath = Path.Combine(statsDir, "bypass_stats.json");
                strategyStats = new Dictionary<string, StrategyStats>();
                LoadStats();
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка инициализации статистики", ex);
                strategyStats = new Dictionary<string, StrategyStats>();
            }
        }

        /// <summary>
        /// Записывает использование стратегии с результатами.
        /// </summary>
        public void RecordStrategyUsage(string strategy, bool success, double successRate)
        {
            if (string.IsNullOrWhiteSpace(strategy))
                return;

            try
            {
                if (!strategyStats.ContainsKey(strategy))
                {
                    strategyStats[strategy] = new StrategyStats
                    {
                        StrategyName = strategy,
                        UsageCount = 0,
                        SuccessCount = 0,
                        TotalSuccessRate = 0,
                        LastUsed = DateTime.Now
                    };
                }

                var stats = strategyStats[strategy];
                stats.UsageCount++;
                stats.LastUsed = DateTime.Now;
                
                if (success)
                {
                    stats.SuccessCount++;
                }
                
                // Вычисляем средний процент успешности
                stats.TotalSuccessRate = (stats.TotalSuccessRate * (stats.UsageCount - 1) + successRate) / stats.UsageCount;
                
                SaveStats();
                
                Logger.Info($"Статистика обновлена: {strategy} - {successRate:F1}% успешности (использований: {stats.UsageCount})");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка записи статистики", ex);
            }
        }

        /// <summary>
        /// Получает лучшую стратегию на основе статистики.
        /// </summary>
        public string GetBestStrategy()
        {
            if (strategyStats.Count == 0)
                return "recommended";
            
            try
            {
                var best = strategyStats.Values
                    .Where(s => s.UsageCount >= 3) // Только стратегии, которые использовались минимум 3 раза
                    .OrderByDescending(s => s.TotalSuccessRate)
                    .ThenByDescending(s => s.SuccessCount)
                    .ThenByDescending(s => s.UsageCount)
                    .FirstOrDefault();
                
                if (best != null)
                {
                    Logger.Debug($"Рекомендуемая стратегия: {best.StrategyName} (успешность: {best.TotalSuccessRate:F1}%)");
                    return best.StrategyName;
                }
                
                // Если нет статистики с минимум 3 использованиями, берем любую
                best = strategyStats.Values
                    .OrderByDescending(s => s.TotalSuccessRate)
                    .ThenByDescending(s => s.SuccessCount)
                    .FirstOrDefault();
                
                return best?.StrategyName ?? "recommended";
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка получения лучшей стратегии", ex);
                return "recommended";
            }
        }

        /// <summary>
        /// Получает все статистики, отсортированные по успешности.
        /// </summary>
        public List<StrategyStats> GetAllStats()
        {
            try
            {
                return strategyStats.Values
                    .OrderByDescending(s => s.TotalSuccessRate)
                    .ThenByDescending(s => s.SuccessCount)
                    .ToList();
            }
            catch
            {
                return new List<StrategyStats>();
            }
        }

        /// <summary>
        /// Получает статистику для конкретной стратегии.
        /// </summary>
        public StrategyStats GetStrategyStats(string strategy)
        {
            if (string.IsNullOrWhiteSpace(strategy))
                return null;

            return strategyStats.ContainsKey(strategy) ? strategyStats[strategy] : null;
        }

        /// <summary>
        /// Сбрасывает всю статистику.
        /// </summary>
        public void ResetStats()
        {
            try
            {
                strategyStats.Clear();
                if (File.Exists(statsFilePath))
                {
                    File.Delete(statsFilePath);
                }
                Logger.Info("Статистика сброшена");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка сброса статистики", ex);
            }
        }

        private void LoadStats()
        {
            try
            {
                if (!File.Exists(statsFilePath))
                    return;

                var json = File.ReadAllText(statsFilePath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                    return;

                // Простой парсинг JSON (формат: StrategyName|UsageCount|SuccessCount|TotalSuccessRate|LastUsed)
                // Используем простой текстовый формат для совместимости
                var lines = json.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || !line.Contains("|"))
                        continue;

                    try
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 5)
                        {
                            var stats = new StrategyStats
                            {
                                StrategyName = parts[0],
                                UsageCount = int.TryParse(parts[1], out int uc) ? uc : 0,
                                SuccessCount = int.TryParse(parts[2], out int sc) ? sc : 0,
                                TotalSuccessRate = double.TryParse(parts[3], out double tr) ? tr : 0,
                                LastUsed = DateTime.TryParse(parts[4], out DateTime dt) ? dt : DateTime.Now
                            };

                            if (!string.IsNullOrWhiteSpace(stats.StrategyName))
                            {
                                strategyStats[stats.StrategyName] = stats;
                            }
                        }
                    }
                    catch
                    {
                        // Пропускаем невалидные строки
                    }
                }

                Logger.Debug($"Загружена статистика для {strategyStats.Count} стратегий");
            }
            catch (Exception ex)
            {
                Logger.Warning("Ошибка загрузки статистики, создается новая", ex);
                strategyStats = new Dictionary<string, StrategyStats>();
            }
        }

        private void SaveStats()
        {
            try
            {
                if (strategyStats.Count == 0)
                    return;

                var sb = new StringBuilder();
                foreach (var kvp in strategyStats)
                {
                    var stats = kvp.Value;
                    sb.AppendLine($"{stats.StrategyName}|{stats.UsageCount}|{stats.SuccessCount}|{stats.TotalSuccessRate:F2}|{stats.LastUsed:yyyy-MM-dd HH:mm:ss}");
                }

                var json = sb.ToString();
                File.WriteAllText(statsFilePath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка сохранения статистики", ex);
            }
        }
    }

    /// <summary>
    /// Статистика использования стратегии.
    /// </summary>
    public class StrategyStats
    {
        public string StrategyName { get; set; }
        public int UsageCount { get; set; }
        public int SuccessCount { get; set; }
        public double TotalSuccessRate { get; set; }
        public DateTime LastUsed { get; set; }

        /// <summary>
        /// Получает процент успешных использований.
        /// </summary>
        public double SuccessPercentage => UsageCount > 0 ? (double)SuccessCount / UsageCount * 100 : 0;
    }
}


