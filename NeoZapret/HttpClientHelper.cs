using System;
using System.Net.Http;

namespace NeoZapret
{
    /// <summary>
    /// Помощник для работы с HttpClient с оптимизацией для большой аудитории.
    /// Переиспользует HttpClient для избежания утечек сокетов.
    /// </summary>
    public static class HttpClientHelper
    {
        private static readonly Lazy<HttpClient> _httpClient = new Lazy<HttpClient>(() =>
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            client.DefaultRequestHeaders.Add("User-Agent", "NeoZapret/3.1.0");
            return client;
        }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Получает переиспользуемый экземпляр HttpClient.
        /// </summary>
        public static HttpClient GetClient()
        {
            return _httpClient.Value;
        }

        /// <summary>
        /// Очищает ресурсы HttpClient (вызывать при завершении работы приложения).
        /// </summary>
        public static void Dispose()
        {
            if (_httpClient.IsValueCreated)
            {
                _httpClient.Value.Dispose();
            }
        }
    }
}

