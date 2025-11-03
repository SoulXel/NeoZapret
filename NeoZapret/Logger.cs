using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NeoZapret
{
    /// <summary>
    /// Централизованное логирование операций приложения.
    /// Записывает логи в файл и поддерживает разные уровни логирования.
    /// </summary>
    public static class Logger
    {
        private static readonly object _lockObject = new object();
        private static string _logsDirectory;
        private static string _logFileName;
        private static readonly Queue<string> _logQueue = new Queue<string>();
        private static readonly System.Threading.Timer _flushTimer;
        private const int FLUSH_INTERVAL_MS = 3000; // Буферизация 3 секунды (оптимизация для высокой нагрузки)
        private const int MAX_QUEUE_SIZE = 5000; // Увеличенный размер очереди для большой аудитории
        private const int BATCH_SIZE = 100; // Записываем батчами для эффективности

        static Logger()
        {
            Initialize();
            
            // Запускаем таймер для периодической записи из буфера
            _flushTimer = new System.Threading.Timer(
                FlushLogs,
                null,
                FLUSH_INTERVAL_MS,
                FLUSH_INTERVAL_MS);
        }

        private static void Initialize()
        {
            try
            {
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "NeoZapret");

                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }

                _logsDirectory = Path.Combine(appDataPath, "logs");
                if (!Directory.Exists(_logsDirectory))
                {
                    Directory.CreateDirectory(_logsDirectory);
                }

                // Имя файла лога: NeoZapret_YYYY-MM-DD.log
                _logFileName = Path.Combine(_logsDirectory, $"NeoZapret_{DateTime.Now:yyyy-MM-dd}.log");

                // Очистка старых логов (старше 30 дней) - асинхронно (fire and forget)
                _ = System.Threading.Tasks.Task.Run(() => CleanOldLogs());
            }
            catch
            {
                // Если не удалось создать папку для логов, используем временную папку
                _logsDirectory = Path.GetTempPath();
                _logFileName = Path.Combine(_logsDirectory, $"NeoZapret_{DateTime.Now:yyyy-MM-dd}.log");
            }
        }

        /// <summary>
        /// Записывает сообщение в лог с указанным уровнем.
        /// Использует буферизацию для оптимизации производительности.
        /// </summary>
        public static void Log(LogLevel level, string message, Exception exception = null)
        {
            try
            {
                var logEntry = FormatLogEntry(level, message, exception);
                
                lock (_lockObject)
                {
                    // Если очередь слишком большая, принудительно записываем
                    if (_logQueue.Count >= MAX_QUEUE_SIZE)
                    {
                        FlushLogsInternal();
                    }
                    
                    _logQueue.Enqueue(logEntry);
                }
            }
            catch
            {
                // Игнорируем ошибки записи в лог, чтобы не нарушать работу приложения
            }
        }

        /// <summary>
        /// Принудительно записывает все накопленные логи в файл.
        /// </summary>
        public static void Flush()
        {
            FlushLogsInternal();
        }

        private static void FlushLogs(object state)
        {
            FlushLogsInternal();
        }

        private static void FlushLogsInternal()
        {
            if (_logQueue.Count == 0)
                return;

            // Оптимизированная батчевая запись для высокой производительности
            List<string> batch = new List<string>();
            
            lock (_lockObject)
            {
                // Извлекаем батч для записи (не блокируем добавление новых логов долго)
                int count = Math.Min(BATCH_SIZE, _logQueue.Count);
                for (int i = 0; i < count; i++)
                {
                    if (_logQueue.Count > 0)
                    {
                        batch.Add(_logQueue.Dequeue());
                    }
                }
            }

            // Записываем вне блокировки для лучшей производительности
            if (batch.Count > 0)
            {
                try
                {
                    var sb = new StringBuilder(batch.Count * 200); // Предварительно выделяем память
                    foreach (var entry in batch)
                    {
                        sb.Append(entry);
                    }

                    if (sb.Length > 0)
                    {
                        File.AppendAllText(_logFileName, sb.ToString(), Encoding.UTF8);
                    }
                }
                catch (Exception ex)
                {
                    // Логируем ошибку записи, но не падаем
                    System.Diagnostics.Debug.WriteLine($"Ошибка записи логов: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Записывает информационное сообщение.
        /// </summary>
        public static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        /// <summary>
        /// Записывает предупреждение.
        /// </summary>
        public static void Warning(string message, Exception exception = null)
        {
            Log(LogLevel.Warning, message, exception);
        }

        /// <summary>
        /// Записывает ошибку.
        /// </summary>
        public static void Error(string message, Exception exception = null)
        {
            Log(LogLevel.Error, message, exception);
        }

        /// <summary>
        /// Записывает успешную операцию.
        /// </summary>
        public static void Success(string message)
        {
            Log(LogLevel.Success, message);
        }

        /// <summary>
        /// Записывает отладочное сообщение.
        /// </summary>
        public static void Debug(string message)
        {
#if DEBUG
            Log(LogLevel.Debug, message);
#endif
        }

        private static string FormatLogEntry(LogLevel level, string message, Exception exception)
        {
            var sb = new StringBuilder();
            sb.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ");
            sb.Append($"[{GetLevelString(level)}] ");
            sb.Append(message);

            if (exception != null)
            {
                sb.AppendLine();
                sb.Append($"   Exception: {exception.GetType().Name}");
                sb.AppendLine($"   Message: {exception.Message}");
                if (exception.StackTrace != null)
                {
                    sb.AppendLine($"   StackTrace: {exception.StackTrace}");
                }
            }

            sb.AppendLine();
            return sb.ToString();
        }

        private static string GetLevelString(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug: return "DEBUG";
                case LogLevel.Info: return "INFO";
                case LogLevel.Warning: return "WARN";
                case LogLevel.Error: return "ERROR";
                case LogLevel.Success: return "SUCCESS";
                default: return "INFO";
            }
        }

        /// <summary>
        /// Очищает старые логи (старше указанного количества дней).
        /// </summary>
        public static void CleanOldLogs(int daysToKeep = 30)
        {
            try
            {
                if (string.IsNullOrEmpty(_logsDirectory) || !Directory.Exists(_logsDirectory))
                    return;

                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

                foreach (var file in Directory.GetFiles(_logsDirectory, "NeoZapret_*.log"))
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.LastWriteTime < cutoffDate)
                        {
                            File.Delete(file);
                        }
                    }
                    catch
                    {
                        // Игнорируем ошибки удаления отдельных файлов
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки очистки логов
            }
        }

        /// <summary>
        /// Получает путь к папке с логами.
        /// </summary>
        public static string GetLogsDirectory()
        {
            return _logsDirectory ?? Path.GetTempPath();
        }

        /// <summary>
        /// Получает путь к текущему файлу лога.
        /// </summary>
        public static string GetLogFilePath()
        {
            return _logFileName ?? Path.Combine(Path.GetTempPath(), $"NeoZapret_{DateTime.Now:yyyy-MM-dd}.log");
        }
    }

    /// <summary>
    /// Уровни логирования.
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Success
    }
}


