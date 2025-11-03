using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NeoZapret
{
    /// <summary>
    /// Умный автоматический обновлятор списков блокировок.
    /// Работает в фоновом режиме и автоматически обновляет списки по расписанию.
    /// </summary>
    public static class SmartUpdater
    {
        private static System.Threading.Timer updateTimer;
        private static bool isUpdating = false;
        private const int UPDATE_CHECK_INTERVAL_HOURS = 24; // Проверка каждые 24 часа
        private const int UPDATE_REQUIRED_HOURS = 168; // Обновление требуется через 7 дней

        /// <summary>
        /// Запускает фоновый обновлятор списков.
        /// </summary>
        public static void Start(string listsPath)
        {
            try
            {
                if (updateTimer != null)
                {
                    updateTimer.Dispose();
                }

                // Проверяем при старте (fire and forget)
                _ = System.Threading.Tasks.Task.Run(() => CheckAndUpdateAsync(listsPath, false));

                // Настраиваем периодическую проверку
                updateTimer = new System.Threading.Timer(
                    _ => _ = System.Threading.Tasks.Task.Run(() => CheckAndUpdateAsync(listsPath, true)),
                    null,
                    TimeSpan.FromHours(UPDATE_CHECK_INTERVAL_HOURS),
                    TimeSpan.FromHours(UPDATE_CHECK_INTERVAL_HOURS)
                );

                Logger.Info("Умный обновлятор списков запущен");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка запуска умного обновлятора", ex);
            }
        }

        /// <summary>
        /// Останавливает фоновый обновлятор.
        /// </summary>
        public static void Stop()
        {
            try
            {
                updateTimer?.Dispose();
                updateTimer = null;
                Logger.Info("Умный обновлятор списков остановлен");
            }
            catch (Exception ex)
            {
                Logger.Warning("Ошибка остановки умного обновлятора", ex);
            }
        }

        /// <summary>
        /// Проверяет необходимость обновления и обновляет при необходимости.
        /// </summary>
        private static async Task CheckAndUpdateAsync(string listsPath, bool silent)
        {
            if (isUpdating)
            {
                Logger.Debug("Обновление уже выполняется, пропускаю...");
                return;
            }

            try
            {
                isUpdating = true;

                if (string.IsNullOrEmpty(listsPath) || !Directory.Exists(listsPath))
                {
                    Logger.Debug("Папка lists не найдена, пропускаю обновление");
                    return;
                }

                // Проверяем, нужно ли обновление
                if (!ListUpdater.NeedsUpdate(listsPath, UPDATE_REQUIRED_HOURS / 24))
                {
                    if (!silent)
                        Logger.Debug("Списки актуальны, обновление не требуется");
                    return;
                }

                if (!silent)
                    Logger.Info("Автоматическое обновление списков...");
                else
                    Logger.Debug("Фоновое обновление списков...");

                var progress = new Progress<string>(message =>
                {
                    if (!silent)
                        Logger.Info(message);
                    else
                        Logger.Debug(message);
                });

                var result = await ListUpdater.UpdateAllLists(listsPath, progress);

                if (result.Success && result.UpdatedFiles.Count > 0)
                {
                    if (!silent)
                    {
                        Logger.Success($"✓ Автоматически обновлено файлов: {result.UpdatedFiles.Count}");
                        Logger.Info($"Обновленные файлы: {string.Join(", ", result.UpdatedFiles)}");
                    }
                    else
                    {
                        Logger.Info($"Фоновое обновление завершено: {result.UpdatedFiles.Count} файлов");
                    }
                }
                else if (!silent)
                {
                    Logger.Warning("Не удалось обновить списки автоматически");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка автоматического обновления списков", ex);
            }
            finally
            {
                isUpdating = false;
            }
        }

        /// <summary>
        /// Принудительно запускает обновление (для ручного запуска).
        /// </summary>
        public static async Task<bool> ForceUpdateAsync(string listsPath, IProgress<string> progress = null)
        {
            if (isUpdating)
            {
                Logger.Warning("Обновление уже выполняется");
                return false;
            }

            try
            {
                isUpdating = true;

                if (string.IsNullOrEmpty(listsPath) || !Directory.Exists(listsPath))
                {
                    Logger.Error("Папка lists не найдена");
                    return false;
                }

                progress?.Report("Принудительное обновление списков...");
                Logger.Info("Запускаю принудительное обновление списков");

                var updateProgress = progress ?? new Progress<string>(message => Logger.Info(message));
                var result = await ListUpdater.UpdateAllLists(listsPath, updateProgress);

                if (result.Success && result.UpdatedFiles.Count > 0)
                {
                    Logger.Success($"✓ Принудительное обновление завершено: {result.UpdatedFiles.Count} файлов");
                    return true;
                }
                else
                {
                    Logger.Warning("Не удалось обновить списки");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка принудительного обновления", ex);
                return false;
            }
            finally
            {
                isUpdating = false;
            }
        }

        /// <summary>
        /// Получает информацию о последнем обновлении.
        /// </summary>
        public static UpdateStatus GetUpdateStatus(string listsPath)
        {
            try
            {
                if (string.IsNullOrEmpty(listsPath) || !Directory.Exists(listsPath))
                {
                    return new UpdateStatus
                    {
                        IsAvailable = false,
                        LastUpdateTime = null,
                        DaysSinceUpdate = null,
                        Message = "Папка lists не найдена"
                    };
                }

                var generalListPath = Path.Combine(listsPath, "list-general.txt");
                if (!File.Exists(generalListPath))
                {
                    return new UpdateStatus
                    {
                        IsAvailable = true,
                        LastUpdateTime = null,
                        DaysSinceUpdate = null,
                        Message = "Списки не инициализированы"
                    };
                }

                var lastWriteTime = File.GetLastWriteTime(generalListPath);
                var daysSince = (DateTime.Now - lastWriteTime).TotalDays;

                var needsUpdate = daysSince >= UPDATE_REQUIRED_HOURS / 24;

                return new UpdateStatus
                {
                    IsAvailable = needsUpdate,
                    LastUpdateTime = lastWriteTime,
                    DaysSinceUpdate = (int)daysSince,
                    Message = needsUpdate 
                        ? $"Требуется обновление (прошло {daysSince:F0} дней)"
                        : $"Списки актуальны (обновлены {daysSince:F0} дней назад)"
                };
            }
            catch (Exception ex)
            {
                Logger.Warning("Ошибка получения статуса обновления", ex);
                return new UpdateStatus
                {
                    IsAvailable = false,
                    Message = "Ошибка проверки статуса"
                };
            }
        }
    }

    /// <summary>
    /// Статус обновления списков.
    /// </summary>
    public class UpdateStatus
    {
        public bool IsAvailable { get; set; }
        public DateTime? LastUpdateTime { get; set; }
        public int? DaysSinceUpdate { get; set; }
        public string Message { get; set; }
    }
}

