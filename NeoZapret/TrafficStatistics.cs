using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NeoZapret
{
    /// <summary>
    /// Класс для отслеживания статистики трафика и использования обхода.
    /// Похоже на функционал из проектов zapret и GoodbyeDPI.
    /// </summary>
    public class TrafficStatistics
    {
        private readonly string statsFilePath;
        private Dictionary<string, SessionStats> sessions;
        private SessionStats currentSession;

        public TrafficStatistics(string appPath)
        {
            try
            {
                var statsDir = Path.Combine(appPath ?? ".", "statistics");
                if (!Directory.Exists(statsDir))
                {
                    Directory.CreateDirectory(statsDir);
                }

                statsFilePath = Path.Combine(statsDir, "traffic_stats.json");
                sessions = new Dictionary<string, SessionStats>();
                LoadStats();
                
                // Создаем текущую сессию
                currentSession = new SessionStats
                {
                    SessionId = Guid.NewGuid().ToString(),
                    StartTime = DateTime.Now,
                    Strategy = ""
                };
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка инициализации статистики трафика", ex);
                sessions = new Dictionary<string, SessionStats>();
                currentSession = new SessionStats
                {
                    SessionId = Guid.NewGuid().ToString(),
                    StartTime = DateTime.Now
                };
            }
        }

        /// <summary>
        /// Запускает новую сессию обхода.
        /// </summary>
        public void StartSession(string strategy)
        {
            try
            {
                if (currentSession != null && !string.IsNullOrEmpty(currentSession.Strategy))
                {
                    // Сохраняем предыдущую сессию
                    EndSession();
                }

                currentSession = new SessionStats
                {
                    SessionId = Guid.NewGuid().ToString(),
                    StartTime = DateTime.Now,
                    Strategy = strategy ?? "unknown"
                };

                Logger.Info($"Начата новая сессия обхода: {strategy}");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка начала сессии", ex);
            }
        }

        /// <summary>
        /// Завершает текущую сессию.
        /// </summary>
        public void EndSession()
        {
            try
            {
                if (currentSession == null || string.IsNullOrEmpty(currentSession.SessionId))
                    return;

                currentSession.EndTime = DateTime.Now;
                currentSession.Duration = (currentSession.EndTime.Value - currentSession.StartTime).TotalSeconds;

                sessions[currentSession.SessionId] = currentSession;
                SaveStats();

                Logger.Info($"Сессия завершена: {currentSession.Duration:F0} секунд, стратегия: {currentSession.Strategy}");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка завершения сессии", ex);
            }
        }

        /// <summary>
        /// Обновляет статистику доступности сайтов в текущей сессии.
        /// </summary>
        public void UpdateAvailability(int successCount, int totalCount, double successRate)
        {
            try
            {
                if (currentSession == null)
                    return;

                currentSession.TotalChecks += totalCount;
                currentSession.SuccessfulChecks += successCount;
                
                // Обновляем средний процент успешности
                if (currentSession.TotalChecks > 0)
                {
                    currentSession.AverageSuccessRate = 
                        (currentSession.AverageSuccessRate * (currentSession.TotalChecks - totalCount) + successRate * totalCount) 
                        / currentSession.TotalChecks;
                }

                // Обновляем последний статус
                currentSession.LastSuccessRate = successRate;
                currentSession.LastUpdate = DateTime.Now;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка обновления статистики доступности", ex);
            }
        }

        /// <summary>
        /// Получает статистику всех сессий.
        /// </summary>
        public List<SessionStats> GetAllSessions()
        {
            try
            {
                return sessions.Values
                    .OrderByDescending(s => s.StartTime)
                    .ToList();
            }
            catch
            {
                return new List<SessionStats>();
            }
        }

        /// <summary>
        /// Получает общую статистику использования.
        /// </summary>
        public UsageStatistics GetUsageStatistics()
        {
            try
            {
                var stats = new UsageStatistics();
                
                if (sessions.Count == 0)
                    return stats;

                stats.TotalSessions = sessions.Count;
                stats.TotalDuration = sessions.Values.Sum(s => s.Duration);

                var strategyGroups = sessions.Values
                    .Where(s => !string.IsNullOrEmpty(s.Strategy))
                    .GroupBy(s => s.Strategy);

                foreach (var group in strategyGroups)
                {
                    stats.StrategyUsage[group.Key] = group.Count();
                }

                if (sessions.Values.Any(s => s.TotalChecks > 0))
                {
                    stats.AverageSuccessRate = sessions.Values
                        .Where(s => s.TotalChecks > 0)
                        .Average(s => s.AverageSuccessRate);
                }

                return stats;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка получения статистики использования", ex);
                return new UsageStatistics();
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

                var lines = json.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || !line.Contains("|"))
                        continue;

                    try
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 6)
                        {
                            var session = new SessionStats
                            {
                                SessionId = parts[0],
                                Strategy = parts[1],
                                StartTime = DateTime.TryParse(parts[2], out DateTime st) ? st : DateTime.Now,
                                EndTime = DateTime.TryParse(parts[3], out DateTime et) ? et : (DateTime?)null,
                                Duration = double.TryParse(parts[4], out double d) ? d : 0,
                                TotalChecks = int.TryParse(parts[5], out int tc) ? tc : 0,
                                SuccessfulChecks = parts.Length > 6 && int.TryParse(parts[6], out int sc) ? sc : 0,
                                AverageSuccessRate = parts.Length > 7 && double.TryParse(parts[7], out double asr) ? asr : 0
                            };

                            if (!string.IsNullOrEmpty(session.SessionId))
                            {
                                sessions[session.SessionId] = session;
                            }
                        }
                    }
                    catch
                    {
                        // Пропускаем невалидные строки
                    }
                }

                Logger.Debug($"Загружена статистика трафика: {sessions.Count} сессий");
            }
            catch (Exception ex)
            {
                Logger.Warning("Ошибка загрузки статистики трафика", ex);
            }
        }

        private void SaveStats()
        {
            try
            {
                if (sessions.Count == 0)
                    return;

                var sb = new StringBuilder();
                foreach (var kvp in sessions)
                {
                    var s = kvp.Value;
                    sb.AppendLine($"{s.SessionId}|{s.Strategy}|{s.StartTime:yyyy-MM-dd HH:mm:ss}|{s.EndTime:yyyy-MM-dd HH:mm:ss}|{s.Duration:F2}|{s.TotalChecks}|{s.SuccessfulChecks}|{s.AverageSuccessRate:F2}");
                }

                File.WriteAllText(statsFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка сохранения статистики трафика", ex);
            }
        }
    }

    /// <summary>
    /// Статистика одной сессии.
    /// </summary>
    public class SessionStats
    {
        public string SessionId { get; set; }
        public string Strategy { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double Duration { get; set; }
        public int TotalChecks { get; set; }
        public int SuccessfulChecks { get; set; }
        public double AverageSuccessRate { get; set; }
        public double LastSuccessRate { get; set; }
        public DateTime? LastUpdate { get; set; }
    }

    /// <summary>
    /// Общая статистика использования.
    /// </summary>
    public class UsageStatistics
    {
        public int TotalSessions { get; set; }
        public double TotalDuration { get; set; }
        public Dictionary<string, int> StrategyUsage { get; set; } = new Dictionary<string, int>();
        public double AverageSuccessRate { get; set; }
    }
}

