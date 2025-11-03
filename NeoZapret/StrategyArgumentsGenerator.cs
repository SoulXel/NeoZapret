using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace NeoZapret
{
    /// <summary>
    /// Централизованный генератор аргументов для стратегий обхода.
    /// Устраняет дублирование кода между MainForm и ServiceManageForm.
    /// Оптимизирован для производительности с кэшированием.
    /// </summary>
    public static class StrategyArgumentsGenerator
    {
        // Кэш для оптимизации производительности
        private static readonly Dictionary<string, string> _argumentCache = new Dictionary<string, string>();
        private static readonly object _cacheLock = new object();
        /// <summary>
        /// Генерирует аргументы командной строки для указанной стратегии.
        /// </summary>
        /// <param name="strategy">Название стратегии: recommended, fast, max, aggressive, games</param>
        /// <param name="gameFilter">Игровой фильтр портов (например, "12" или "1024-65535")</param>
        /// <param name="binPath">Путь к папке bin с winws.exe</param>
        /// <param name="listsPath">Путь к папке lists со списками доменов и IP</param>
        /// <returns>Строка аргументов для winws.exe или пустая строка в случае ошибки</returns>
        public static string GenerateBypassArguments(string strategy, string gameFilter, string binPath, string listsPath)
        {
            try
            {
                // Валидация входных параметров
                if (string.IsNullOrWhiteSpace(strategy))
                {
                    throw new ArgumentException("Стратегия не может быть пустой", nameof(strategy));
                }

                if (string.IsNullOrWhiteSpace(gameFilter))
                {
                    gameFilter = "12";
                }

                if (string.IsNullOrWhiteSpace(binPath) || !Directory.Exists(binPath))
                {
                    throw new DirectoryNotFoundException($"Папка bin не найдена: {binPath}");
                }

                if (string.IsNullOrWhiteSpace(listsPath) || !Directory.Exists(listsPath))
                {
                    throw new DirectoryNotFoundException($"Папка lists не найдена: {listsPath}");
                }

                // Проверка наличия необходимых файлов
                var quicFile = Path.Combine(binPath, "quic_initial_www_google_com.bin");
                var tlsFile1 = Path.Combine(binPath, "tls_clienthello_4pda_to.bin");
                var tlsFile2 = Path.Combine(binPath, "tls_clienthello_www_google_com.bin");

                if (!File.Exists(quicFile) || !File.Exists(tlsFile1) || !File.Exists(tlsFile2))
                {
                    throw new FileNotFoundException("Не найдены необходимые файлы паттернов в папке bin");
                }

                // Базовые аргументы для всех стратегий
                var baseArgs = $"--wf-tcp=80,443,2053,2083,2087,2096,8443,{gameFilter} --wf-udp=443,19294-19344,50000-50100,{gameFilter} ";

                // Экранирование путей для командной строки
                var escapedBinPath = EscapePath(binPath);
                var escapedListsPath = EscapePath(listsPath);
                var escapedQuicFile = EscapePath(quicFile);
                var escapedTlsFile1 = EscapePath(tlsFile1);
                var escapedTlsFile2 = EscapePath(tlsFile2);

                // Проверка кэша (кэшируем только без gameFilter для экономии памяти)
                string cacheKey = $"{strategy}:{escapedListsPath}:{escapedBinPath}";
                string cachedArgs = null;
                
                if (string.IsNullOrEmpty(gameFilter) || gameFilter == "12")
                {
                    lock (_cacheLock)
                    {
                        if (_argumentCache.TryGetValue(cacheKey, out cachedArgs))
                        {
                            // Возвращаем кэшированные аргументы с подстановкой gameFilter
                            return baseArgs + cachedArgs;
                        }
                    }
                }

                // Генерация аргументов в зависимости от стратегии
                string strategyArgs;
                switch (strategy.ToLowerInvariant())
                {
                    case "recommended":
                        strategyArgs = GenerateRecommendedStrategy(escapedListsPath, escapedBinPath, escapedQuicFile, escapedTlsFile1, escapedTlsFile2, gameFilter);
                        break;

                    case "fast":
                        strategyArgs = GenerateFastStrategy(escapedListsPath, escapedBinPath, escapedQuicFile, escapedTlsFile2, gameFilter);
                        break;

                    case "max":
                        strategyArgs = GenerateMaxStrategy(escapedListsPath, escapedBinPath, escapedQuicFile, escapedTlsFile2, gameFilter);
                        break;

                    case "aggressive":
                        strategyArgs = GenerateAggressiveStrategy(escapedListsPath, escapedBinPath, escapedQuicFile, escapedTlsFile1, escapedTlsFile2, gameFilter);
                        break;

                    case "games":
                        strategyArgs = GenerateGamesStrategy(escapedListsPath, escapedBinPath, escapedQuicFile, escapedTlsFile2, gameFilter);
                        break;

                    // Стратегии для провайдеров
                    case "rostelecom":
                        strategyArgs = GenerateRostelecomStrategy(escapedListsPath, escapedBinPath, escapedQuicFile, escapedTlsFile1, escapedTlsFile2, gameFilter);
                        break;

                    case "mts":
                        strategyArgs = GenerateMTSStrategy(escapedListsPath, escapedBinPath, escapedQuicFile, escapedTlsFile2, gameFilter);
                        break;

                    case "beeline":
                        strategyArgs = GenerateBeelineStrategy(escapedListsPath, escapedBinPath, escapedQuicFile, escapedTlsFile1, escapedTlsFile2, gameFilter);
                        break;

                    case "megafon":
                        strategyArgs = GenerateMegafonStrategy(escapedListsPath, escapedBinPath, escapedQuicFile, escapedTlsFile2, gameFilter);
                        break;

                    case "tele2":
                        strategyArgs = GenerateTele2Strategy(escapedListsPath, escapedBinPath, escapedQuicFile, escapedTlsFile2, gameFilter);
                        break;

                    case "ttk":
                        strategyArgs = GenerateTTKStrategy(escapedListsPath, escapedBinPath, escapedQuicFile, escapedTlsFile1, escapedTlsFile2, gameFilter);
                        break;

                    case "ertelecom":
                        strategyArgs = GenerateERTelecomStrategy(escapedListsPath, escapedBinPath, escapedQuicFile, escapedTlsFile2, gameFilter);
                        break;

                    default:
                        throw new ArgumentException($"Неизвестная стратегия: {strategy}", nameof(strategy));
                }

                // Кэшируем результат (только для стандартного gameFilter)
                if ((string.IsNullOrEmpty(gameFilter) || gameFilter == "12") && cachedArgs == null)
                {
                    lock (_cacheLock)
                    {
                        if (!_argumentCache.ContainsKey(cacheKey))
                        {
                            _argumentCache[cacheKey] = strategyArgs;
                            
                            // Ограничиваем размер кэша (максимум 50 записей)
                            if (_argumentCache.Count > 50)
                            {
                                var firstKey = _argumentCache.Keys.GetEnumerator();
                                firstKey.MoveNext();
                                _argumentCache.Remove(firstKey.Current);
                            }
                        }
                    }
                }

                return baseArgs + strategyArgs;
            }
            catch (Exception ex)
            {
                // Логирование ошибки (можно расширить)
                System.Diagnostics.Debug.WriteLine($"Ошибка генерации аргументов: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Экранирует путь для использования в командной строке.
        /// </summary>
        private static string EscapePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // Если путь содержит пробелы, заключаем в кавычки
            if (path.Contains(" "))
                return $"\"{path}\"";

            return path;
        }

        private static string GenerateRecommendedStrategy(string listsPath, string binPath, string quicFile, string tlsFile1, string tlsFile2, string gameFilter)
        {
            // Улучшенная стратегия на основе методов из zapret и GoodbyeDPI
            // Комбинация fake + multisplit для максимальной эффективности
            // Используются оптимальные параметры split-seqovl для лучшей фрагментации
            return $"--filter-udp=443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,multisplit --dpi-desync-repeats=3 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile1} --dpi-desync-fake-quic={quicFile} --new --filter-udp=19294-19344,50000-50100 --filter-l7=discord,stun --dpi-desync=fake,multisplit --dpi-desync-repeats=3 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=2053,2083,2087,2096,8443 --hostlist-domains=discord.media --dpi-desync=multisplit --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile1} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile1} --new --filter-tcp=443 --hostlist={listsPath}\\list-google.txt --ip-id=zero --dpi-desync=fake,multisplit --dpi-desync-repeats=3 --dpi-desync-split-seqovl=681 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-tcp=80,443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,multisplit --dpi-desync-repeats=3 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile1} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile1} --new --filter-udp=443 --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,multisplit --dpi-desync-repeats=4 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=80,443,{gameFilter} --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,multisplit --dpi-desync-repeats=4 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile1} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile1} --new --filter-udp={gameFilter} --ipset={listsPath}\\ipset-all.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake --dpi-desync-autottl=1 --dpi-desync-repeats=8 --dpi-desync-any-protocol=1 --dpi-desync-fake-unknown-udp={quicFile} --dpi-desync-cutoff=n1";
        }

        private static string GenerateFastStrategy(string listsPath, string binPath, string quicFile, string tlsFile2, string gameFilter)
        {
            // Оптимизированная быстрая стратегия с минимальными повторами
            // Использует fake с fooling=ts для быстрого обхода без фрагментации
            return $"--filter-udp=443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake --dpi-desync-repeats=2 --dpi-desync-fake-quic={quicFile} --new --filter-udp=19294-19344,50000-50100 --filter-l7=discord,stun --dpi-desync=fake --dpi-desync-repeats=2 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=2053,2083,2087,2096,8443 --hostlist-domains=discord.media --dpi-desync=fake --dpi-desync-repeats=2 --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-tcp=443 --hostlist={listsPath}\\list-google.txt --ip-id=zero --dpi-desync=fake --dpi-desync-repeats=2 --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-tcp=80,443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake --dpi-desync-repeats=2 --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-udp=443 --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake --dpi-desync-repeats=2 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=80,443,{gameFilter} --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake --dpi-desync-repeats=2 --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-udp={gameFilter} --ipset={listsPath}\\ipset-all.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake --dpi-desync-autottl=1 --dpi-desync-repeats=4 --dpi-desync-any-protocol=1 --dpi-desync-fake-unknown-udp={quicFile} --dpi-desync-cutoff=n1";
        }

        private static string GenerateMaxStrategy(string listsPath, string binPath, string quicFile, string tlsFile2, string gameFilter)
        {
            // Максимально агрессивная стратегия на основе методов из GoodbyeDPI и zapret
            // Использует комбинацию методов: fake + fakedsplit для UDP, multisplit для TCP
            // ВАЖНО: fake+fakedsplit+multisplit недопустимая комбинация! Используем multisplit отдельно для TCP
            // Увеличен autottl и cutoff для максимальной надежности
            return $"--filter-udp=443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,fakedsplit --dpi-desync-repeats=8 --dpi-desync-fakedsplit-pattern=0x00 --dpi-desync-fake-quic={quicFile} --new --filter-udp=19294-19344,50000-50100 --filter-l7=discord,stun --dpi-desync=fake,fakedsplit --dpi-desync-repeats=8 --dpi-desync-fakedsplit-pattern=0x00 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=2053,2083,2087,2096,8443 --hostlist-domains=discord.media --dpi-desync=multisplit --dpi-desync-repeats=8 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-tcp=443 --hostlist={listsPath}\\list-google.txt --ip-id=zero --dpi-desync=multisplit --dpi-desync-repeats=8 --dpi-desync-split-seqovl=681 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-tcp=80,443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=multisplit --dpi-desync-repeats=8 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-udp=443 --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,fakedsplit --dpi-desync-repeats=8 --dpi-desync-fakedsplit-pattern=0x00 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=80,443,{gameFilter} --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=multisplit --dpi-desync-repeats=8 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-udp={gameFilter} --ipset={listsPath}\\ipset-all.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,fakedsplit --dpi-desync-autottl=3 --dpi-desync-repeats=16 --dpi-desync-any-protocol=1 --dpi-desync-fake-unknown-udp={quicFile} --dpi-desync-cutoff=n3";
        }

        private static string GenerateAggressiveStrategy(string listsPath, string binPath, string quicFile, string tlsFile1, string tlsFile2, string gameFilter)
        {
            // Агрессивная стратегия - использует те же параметры что и Max стратегия
            // Для гарантии работоспособности полностью идентична рабочей Max стратегии
            // Можно использовать Max стратегию напрямую, но оставляем для совместимости
            return GenerateMaxStrategy(listsPath, binPath, quicFile, tlsFile2, gameFilter);
        }

        private static string GenerateGamesStrategy(string listsPath, string binPath, string quicFile, string tlsFile2, string gameFilter)
        {
            // Оптимизированная стратегия для игр с фокусом на UDP и минимальный пинг
            // Использует fake для быстрого обхода игровых протоколов
            return $"--filter-udp=443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake --dpi-desync-repeats=4 --dpi-desync-fake-quic={quicFile} --new --filter-udp=19294-19344,50000-50100 --filter-l7=discord,stun --dpi-desync=fake --dpi-desync-repeats=4 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=2053,2083,2087,2096,8443 --hostlist-domains=discord.media --dpi-desync=fake --dpi-desync-repeats=4 --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-tcp=443 --hostlist={listsPath}\\list-google.txt --ip-id=zero --dpi-desync=fake --dpi-desync-repeats=4 --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-tcp=80,443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake --dpi-desync-repeats=4 --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-udp=443 --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake --dpi-desync-repeats=4 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=80,443,{gameFilter} --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake --dpi-desync-repeats=4 --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-udp={gameFilter} --ipset={listsPath}\\ipset-all.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake --dpi-desync-autottl=1 --dpi-desync-repeats=10 --dpi-desync-any-protocol=1 --dpi-desync-fake-unknown-udp={quicFile} --dpi-desync-cutoff=n1";
        }

        // ========== СТРАТЕГИИ ДЛЯ ПРОВАЙДЕРОВ ==========

        /// <summary>
        /// Стратегия для Ростелеком - использует multisplit с повышенными повторами для обхода их DPI.
        /// </summary>
        private static string GenerateRostelecomStrategy(string listsPath, string binPath, string quicFile, string tlsFile1, string tlsFile2, string gameFilter)
        {
            // Ростелеком использует агрессивный DPI, требуется multisplit с высокими повторами
            return $"--filter-udp=443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,multisplit --dpi-desync-repeats=6 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile1} --dpi-desync-fake-quic={quicFile} --new --filter-udp=19294-19344,50000-50100 --filter-l7=discord,stun --dpi-desync=fake,multisplit --dpi-desync-repeats=6 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=2053,2083,2087,2096,8443 --hostlist-domains=discord.media --dpi-desync=multisplit --dpi-desync-repeats=8 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile1} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile1} --new --filter-tcp=443 --hostlist={listsPath}\\list-google.txt --ip-id=zero --dpi-desync=multisplit --dpi-desync-repeats=8 --dpi-desync-split-seqovl=681 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-tcp=80,443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=multisplit --dpi-desync-repeats=8 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile1} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile1} --new --filter-udp=443 --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,multisplit --dpi-desync-repeats=6 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=80,443,{gameFilter} --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=multisplit --dpi-desync-repeats=8 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile1} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile1} --new --filter-udp={gameFilter} --ipset={listsPath}\\ipset-all.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,multisplit --dpi-desync-autottl=2 --dpi-desync-repeats=12 --dpi-desync-any-protocol=1 --dpi-desync-fake-unknown-udp={quicFile} --dpi-desync-cutoff=n2";
        }

        /// <summary>
        /// Стратегия для МТС - использует комбинацию fake+fakedsplit для эффективного обхода.
        /// </summary>
        private static string GenerateMTSStrategy(string listsPath, string binPath, string quicFile, string tlsFile2, string gameFilter)
        {
            // МТС хорошо работает с fake+fakedsplit для UDP и multisplit для TCP
            return $"--filter-udp=443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,fakedsplit --dpi-desync-repeats=5 --dpi-desync-fakedsplit-pattern=0x00 --dpi-desync-fake-quic={quicFile} --new --filter-udp=19294-19344,50000-50100 --filter-l7=discord,stun --dpi-desync=fake,fakedsplit --dpi-desync-repeats=5 --dpi-desync-fakedsplit-pattern=0x00 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=2053,2083,2087,2096,8443 --hostlist-domains=discord.media --dpi-desync=multisplit --dpi-desync-repeats=6 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-tcp=443 --hostlist={listsPath}\\list-google.txt --ip-id=zero --dpi-desync=multisplit --dpi-desync-repeats=6 --dpi-desync-split-seqovl=681 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-tcp=80,443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=multisplit --dpi-desync-repeats=6 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-udp=443 --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,fakedsplit --dpi-desync-repeats=5 --dpi-desync-fakedsplit-pattern=0x00 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=80,443,{gameFilter} --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=multisplit --dpi-desync-repeats=6 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-udp={gameFilter} --ipset={listsPath}\\ipset-all.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,fakedsplit --dpi-desync-autottl=2 --dpi-desync-repeats=10 --dpi-desync-any-protocol=1 --dpi-desync-fake-unknown-udp={quicFile} --dpi-desync-cutoff=n2";
        }

        /// <summary>
        /// Стратегия для Билайн - использует комбинированные методы с повышенными повторами.
        /// </summary>
        private static string GenerateBeelineStrategy(string listsPath, string binPath, string quicFile, string tlsFile1, string tlsFile2, string gameFilter)
        {
            // Билайн требует комбинированные методы с оптимальными параметрами
            return $"--filter-udp=443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,multisplit --dpi-desync-repeats=5 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile1} --dpi-desync-fake-quic={quicFile} --new --filter-udp=19294-19344,50000-50100 --filter-l7=discord,stun --dpi-desync=fake,multisplit --dpi-desync-repeats=5 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=2053,2083,2087,2096,8443 --hostlist-domains=discord.media --dpi-desync=multisplit --dpi-desync-repeats=7 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile1} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile1} --new --filter-tcp=443 --hostlist={listsPath}\\list-google.txt --ip-id=zero --dpi-desync=multisplit --dpi-desync-repeats=7 --dpi-desync-split-seqovl=681 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-tcp=80,443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=multisplit --dpi-desync-repeats=7 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile1} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile1} --new --filter-udp=443 --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,multisplit --dpi-desync-repeats=5 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=80,443,{gameFilter} --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=multisplit --dpi-desync-repeats=7 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile1} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile1} --new --filter-udp={gameFilter} --ipset={listsPath}\\ipset-all.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,multisplit --dpi-desync-autottl=2 --dpi-desync-repeats=12 --dpi-desync-any-protocol=1 --dpi-desync-fake-unknown-udp={quicFile} --dpi-desync-cutoff=n2";
        }

        /// <summary>
        /// Стратегия для Мегафон - использует агрессивные методы с максимальными повторами.
        /// </summary>
        private static string GenerateMegafonStrategy(string listsPath, string binPath, string quicFile, string tlsFile2, string gameFilter)
        {
            // Мегафон использует жесткие блокировки, требуется максимально агрессивная стратегия
            return $"--filter-udp=443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,fakedsplit --dpi-desync-repeats=10 --dpi-desync-fakedsplit-pattern=0x00 --dpi-desync-fake-quic={quicFile} --new --filter-udp=19294-19344,50000-50100 --filter-l7=discord,stun --dpi-desync=fake,fakedsplit --dpi-desync-repeats=10 --dpi-desync-fakedsplit-pattern=0x00 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=2053,2083,2087,2096,8443 --hostlist-domains=discord.media --dpi-desync=multisplit --dpi-desync-repeats=10 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-tcp=443 --hostlist={listsPath}\\list-google.txt --ip-id=zero --dpi-desync=multisplit --dpi-desync-repeats=10 --dpi-desync-split-seqovl=681 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-tcp=80,443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=multisplit --dpi-desync-repeats=10 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-udp=443 --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,fakedsplit --dpi-desync-repeats=10 --dpi-desync-fakedsplit-pattern=0x00 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=80,443,{gameFilter} --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=multisplit --dpi-desync-repeats=10 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-udp={gameFilter} --ipset={listsPath}\\ipset-all.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,fakedsplit --dpi-desync-autottl=3 --dpi-desync-repeats=18 --dpi-desync-any-protocol=1 --dpi-desync-fake-unknown-udp={quicFile} --dpi-desync-cutoff=n3";
        }

        /// <summary>
        /// Стратегия для Теле2 - использует комбинацию методов с оптимизацией для их сети.
        /// </summary>
        private static string GenerateTele2Strategy(string listsPath, string binPath, string quicFile, string tlsFile2, string gameFilter)
        {
            // Теле2 требует агрессивные методы, но с оптимальной настройкой
            return $"--filter-udp=443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,fakedsplit --dpi-desync-repeats=7 --dpi-desync-fakedsplit-pattern=0x00 --dpi-desync-fake-quic={quicFile} --new --filter-udp=19294-19344,50000-50100 --filter-l7=discord,stun --dpi-desync=fake,fakedsplit --dpi-desync-repeats=7 --dpi-desync-fakedsplit-pattern=0x00 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=2053,2083,2087,2096,8443 --hostlist-domains=discord.media --dpi-desync=multisplit --dpi-desync-repeats=8 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-tcp=443 --hostlist={listsPath}\\list-google.txt --ip-id=zero --dpi-desync=multisplit --dpi-desync-repeats=8 --dpi-desync-split-seqovl=681 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-tcp=80,443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=multisplit --dpi-desync-repeats=8 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-udp=443 --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,fakedsplit --dpi-desync-repeats=7 --dpi-desync-fakedsplit-pattern=0x00 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=80,443,{gameFilter} --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=multisplit --dpi-desync-repeats=8 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-udp={gameFilter} --ipset={listsPath}\\ipset-all.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,fakedsplit --dpi-desync-autottl=3 --dpi-desync-repeats=14 --dpi-desync-any-protocol=1 --dpi-desync-fake-unknown-udp={quicFile} --dpi-desync-cutoff=n3";
        }

        /// <summary>
        /// Стратегия для ТТК - использует специальную конфигурацию для их сети.
        /// </summary>
        private static string GenerateTTKStrategy(string listsPath, string binPath, string quicFile, string tlsFile1, string tlsFile2, string gameFilter)
        {
            // ТТК требует специфичную настройку с комбинацией методов
            return $"--filter-udp=443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,multisplit --dpi-desync-repeats=6 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile1} --dpi-desync-fake-quic={quicFile} --new --filter-udp=19294-19344,50000-50100 --filter-l7=discord,stun --dpi-desync=fake,multisplit --dpi-desync-repeats=6 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=2053,2083,2087,2096,8443 --hostlist-domains=discord.media --dpi-desync=multisplit --dpi-desync-repeats=7 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile1} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile1} --new --filter-tcp=443 --hostlist={listsPath}\\list-google.txt --ip-id=zero --dpi-desync=multisplit --dpi-desync-repeats=7 --dpi-desync-split-seqovl=681 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-tcp=80,443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=multisplit --dpi-desync-repeats=7 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile1} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile1} --new --filter-udp=443 --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,multisplit --dpi-desync-repeats=6 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=80,443,{gameFilter} --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=multisplit --dpi-desync-repeats=7 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile1} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile1} --new --filter-udp={gameFilter} --ipset={listsPath}\\ipset-all.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,multisplit --dpi-desync-autottl=2 --dpi-desync-repeats=12 --dpi-desync-any-protocol=1 --dpi-desync-fake-unknown-udp={quicFile} --dpi-desync-cutoff=n2";
        }

        /// <summary>
        /// Стратегия для Эр-Телеком - использует сбалансированные методы.
        /// </summary>
        private static string GenerateERTelecomStrategy(string listsPath, string binPath, string quicFile, string tlsFile2, string gameFilter)
        {
            // Эр-Телеком требует сбалансированную стратегию
            return $"--filter-udp=443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,multisplit --dpi-desync-repeats=5 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-fake-quic={quicFile} --new --filter-udp=19294-19344,50000-50100 --filter-l7=discord,stun --dpi-desync=fake,multisplit --dpi-desync-repeats=5 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=2053,2083,2087,2096,8443 --hostlist-domains=discord.media --dpi-desync=multisplit --dpi-desync-repeats=6 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-tcp=443 --hostlist={listsPath}\\list-google.txt --ip-id=zero --dpi-desync=multisplit --dpi-desync-repeats=6 --dpi-desync-split-seqovl=681 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-tcp=80,443 --hostlist={listsPath}\\list-general.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=multisplit --dpi-desync-repeats=6 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-udp=443 --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,multisplit --dpi-desync-repeats=5 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-fake-quic={quicFile} --new --filter-tcp=80,443,{gameFilter} --ipset={listsPath}\\ipset-all.txt --hostlist-exclude={listsPath}\\list-exclude.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=multisplit --dpi-desync-repeats=6 --dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern={tlsFile2} --dpi-desync-fooling=ts --dpi-desync-fake-tls={tlsFile2} --new --filter-udp={gameFilter} --ipset={listsPath}\\ipset-all.txt --ipset-exclude={listsPath}\\ipset-exclude.txt --dpi-desync=fake,multisplit --dpi-desync-autottl=2 --dpi-desync-repeats=10 --dpi-desync-any-protocol=1 --dpi-desync-fake-unknown-udp={quicFile} --dpi-desync-cutoff=n2";

        }

        /// <summary>
        /// Очищает кэш аргументов (для отладки и оптимизации памяти).
        /// </summary>
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _argumentCache.Clear();
            }
        }
    }
}


