using System;

namespace NeoZapret
{
    /// <summary>
    /// Конфигурация приложения для оптимизации под большую аудиторию.
    /// </summary>
    public static class AppConfig
    {
        // Настройки логирования для высокой нагрузки
        public const int LOG_FLUSH_INTERVAL_MS = 3000;
        public const int LOG_MAX_QUEUE_SIZE = 5000;
        public const int LOG_BATCH_SIZE = 100;
        
        // Настройки кэширования
        public const int STRATEGY_CACHE_MAX_SIZE = 50;
        public const int BYPASS_MONITOR_CACHE_MAX_SIZE = 1000;
        public const int BYPASS_MONITOR_CACHE_DURATION_MINUTES = 5;
        
        // Настройки сетевых запросов
        public const int HTTP_TIMEOUT_SECONDS = 30;
        public const int DNS_TEST_TIMEOUT_SECONDS = 5;
        public const int MAX_NETWORK_RETRIES = 3;
        
        // Настройки мониторинга
        public const int BYPASS_CHECK_INTERVAL_SECONDS = 30;
        public const int STRATEGY_TEST_TIMEOUT_SECONDS = 10;
        
        // Настройки UI (производительность)
        public const bool ENABLE_DOUBLE_BUFFERING = true;
        public const int UI_UPDATE_THROTTLE_MS = 100; // Минимальный интервал обновления UI
        
        // Настройки памяти
        public const int MAX_CONCURRENT_OPERATIONS = 10; // Ограничение параллельных операций
    }
}

