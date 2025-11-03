using System;
using System.Collections.Generic;
using System.Threading;

namespace NeoZapret
{
    /// <summary>
    /// Ограничитель скорости для предотвращения перегрузки сетевых запросов.
    /// Необходим для стабильной работы при большой аудитории.
    /// </summary>
    public class RateLimiter
    {
        private readonly int _maxRequests;
        private readonly TimeSpan _timeWindow;
        private readonly Queue<DateTime> _requestTimes;
        private readonly object _lockObject = new object();

        public RateLimiter(int maxRequests, TimeSpan timeWindow)
        {
            _maxRequests = maxRequests;
            _timeWindow = timeWindow;
            _requestTimes = new Queue<DateTime>();
        }

        /// <summary>
        /// Проверяет, можно ли выполнить запрос, и регистрирует его.
        /// </summary>
        public bool TryAcquire()
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;
                
                // Удаляем старые запросы из окна времени
                while (_requestTimes.Count > 0 && now - _requestTimes.Peek() > _timeWindow)
                {
                    _requestTimes.Dequeue();
                }

                // Проверяем, не превышен ли лимит
                if (_requestTimes.Count >= _maxRequests)
                {
                    return false;
                }

                // Регистрируем новый запрос
                _requestTimes.Enqueue(now);
                return true;
            }
        }

        /// <summary>
        /// Ожидает, пока можно будет выполнить запрос.
        /// </summary>
        public void WaitForAvailability()
        {
            while (!TryAcquire())
            {
                Thread.Sleep(100); // Небольшая задержка перед повтором
            }
        }
    }
}

