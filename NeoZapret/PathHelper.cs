using System;
using System.IO;

namespace NeoZapret
{
    /// <summary>
    /// Утилита для работы с путями приложения.
    /// Централизует логику поиска папок bin и lists.
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// Находит корневую папку проекта, где находятся папки bin и lists.
        /// </summary>
        /// <param name="startPath">Начальный путь для поиска (обычно Application.StartupPath)</param>
        /// <returns>Корневой путь проекта или null, если не найден</returns>
        public static string FindProjectRoot(string startPath)
        {
            if (string.IsNullOrEmpty(startPath))
            {
                startPath = Environment.CurrentDirectory;
            }

            if (string.IsNullOrEmpty(startPath))
            {
                return null;
            }

            var searchPath = startPath;
            var maxLevels = 5; // Максимальное количество уровней для поиска

            for (int i = 0; i < maxLevels; i++)
            {
                if (string.IsNullOrEmpty(searchPath))
                    break;

                try
                {
                    var testBinPath = Path.Combine(searchPath, "bin");
                    var testListsPath = Path.Combine(searchPath, "lists");

                    // Проверяем наличие папок bin и lists, и файла winws.exe
                    if (Directory.Exists(testBinPath) && Directory.Exists(testListsPath))
                    {
                        var winwsPath = Path.Combine(testBinPath, "winws.exe");
                        if (File.Exists(winwsPath))
                        {
                            return searchPath;
                        }
                    }

                    // Поднимаемся на уровень вверх
                    var parent = Directory.GetParent(searchPath);
                    if (parent == null || string.IsNullOrEmpty(parent.FullName))
                        break;
                    
                    searchPath = parent.FullName;
                }
                catch
                {
                    // Пропускаем этот уровень, продолжаем поиск
                    try
                    {
                        var parent = Directory.GetParent(searchPath);
                        if (parent == null || string.IsNullOrEmpty(parent.FullName))
                            break;
                        searchPath = parent.FullName;
                    }
                    catch
                    {
                        break;
                    }
                    continue;
                }
            }

            return null;
        }

        /// <summary>
        /// Инициализирует пути для работы приложения.
        /// </summary>
        /// <param name="startPath">Начальный путь (обычно Application.StartupPath)</param>
        /// <param name="appPath">Выходной параметр: корневой путь приложения</param>
        /// <param name="binPath">Выходной параметр: путь к папке bin</param>
        /// <param name="listsPath">Выходной параметр: путь к папке lists</param>
        /// <returns>true, если пути успешно найдены, иначе false</returns>
        public static bool InitializePaths(string startPath, out string appPath, out string binPath, out string listsPath)
        {
            appPath = null;
            binPath = null;
            listsPath = null;

            try
            {
                // Ищем корневую папку проекта
                appPath = FindProjectRoot(startPath);

                if (!string.IsNullOrEmpty(appPath))
                {
                    binPath = Path.Combine(appPath, "bin");
                    listsPath = Path.Combine(appPath, "lists");

                    // Проверяем существование папок
                    if (Directory.Exists(binPath) && Directory.Exists(listsPath))
                    {
                        // Проверяем наличие winws.exe
                        var winwsPath = Path.Combine(binPath, "winws.exe");
                        if (File.Exists(winwsPath))
                        {
                            return true;
                        }
                    }
                }

                // Если не нашли, пробуем стандартные пути относительно startPath
                if (string.IsNullOrEmpty(startPath))
                {
                    startPath = AppDomain.CurrentDomain.BaseDirectory;
                }

                if (string.IsNullOrEmpty(startPath))
                {
                    startPath = Environment.CurrentDirectory ?? ".";
                }

                appPath = startPath;
                binPath = Path.Combine(appPath, "bin");
                listsPath = Path.Combine(appPath, "lists");

                return Directory.Exists(binPath) && Directory.Exists(listsPath);
            }
            catch
            {
                // В случае ошибки используем значения по умолчанию
                appPath = startPath ?? Environment.CurrentDirectory ?? ".";
                binPath = Path.Combine(appPath, "bin");
                listsPath = Path.Combine(appPath, "lists");
                return false;
            }
        }

        /// <summary>
        /// Проверяет наличие всех необходимых файлов для работы приложения.
        /// </summary>
        /// <param name="binPath">Путь к папке bin</param>
        /// <param name="missingFiles">Выходной параметр: список отсутствующих файлов</param>
        /// <returns>true, если все файлы найдены</returns>
        public static bool ValidateBinFiles(string binPath, out string[] missingFiles)
        {
            var requiredFiles = new[]
            {
                "winws.exe",
                "WinDivert.dll",
                "WinDivert64.sys",
                "cygwin1.dll",
                "quic_initial_www_google_com.bin",
                "tls_clienthello_4pda_to.bin",
                "tls_clienthello_www_google_com.bin"
            };

            var missing = new System.Collections.Generic.List<string>();

            foreach (var file in requiredFiles)
            {
                var filePath = Path.Combine(binPath, file);
                if (!File.Exists(filePath))
                {
                    missing.Add(file);
                }
            }

            missingFiles = missing.ToArray();
            return missing.Count == 0;
        }

        /// <summary>
        /// Получает путь к папке для сохранения стратегий.
        /// </summary>
        /// <param name="appPath">Корневой путь приложения</param>
        /// <returns>Путь к папке strategies</returns>
        public static string GetStrategiesPath(string appPath)
        {
            if (string.IsNullOrEmpty(appPath))
            {
                appPath = Environment.CurrentDirectory ?? ".";
            }

            var strategiesPath = Path.Combine(appPath, "strategies");
            
            // Создаем папку, если её нет
            if (!Directory.Exists(strategiesPath))
            {
                try
                {
                    Directory.CreateDirectory(strategiesPath);
                }
                catch
                {
                    // Игнорируем ошибки создания папки
                }
            }

            return strategiesPath;
        }

        /// <summary>
        /// Получает путь к папке логов.
        /// </summary>
        /// <param name="appPath">Корневой путь приложения</param>
        /// <returns>Путь к папке logs</returns>
        public static string GetLogsPath(string appPath)
        {
            if (string.IsNullOrEmpty(appPath))
            {
                appPath = Environment.CurrentDirectory ?? ".";
            }

            var logsPath = Path.Combine(appPath, "logs");
            
            // Создаем папку, если её нет
            if (!Directory.Exists(logsPath))
            {
                try
                {
                    Directory.CreateDirectory(logsPath);
                }
                catch
                {
                    // Игнорируем ошибки создания папки
                }
            }

            return logsPath;
        }
    }
}

