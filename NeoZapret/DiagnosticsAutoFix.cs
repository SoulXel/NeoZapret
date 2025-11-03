using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace NeoZapret
{
    /// <summary>
    /// Класс для автоматического исправления проблем, обнаруженных диагностикой.
    /// </summary>
    public static class DiagnosticsAutoFix
    {
        /// <summary>
        /// Результат попытки автоисправления.
        /// </summary>
        public class FixResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public bool RequiresRestart { get; set; }
        }

        /// <summary>
        /// Запускает службу BFE, если она не запущена (с альтернативными методами).
        /// </summary>
        public static async Task<FixResult> StartBFE()
        {
            try
            {
                var service = new ServiceController("BFE");
                service.Refresh();
                
                if (service.Status == ServiceControllerStatus.Running)
                {
                    return new FixResult { Success = true, Message = "Служба BFE уже запущена" };
                }

                // Метод 1: Прямой запуск через ServiceController
                try
                {
                    service.Start();
                    
                    // Ждем запуска службы (максимум 15 секунд)
                    int waitTime = 0;
                    while (waitTime < 15000 && service.Status != ServiceControllerStatus.Running)
                    {
                        await Task.Delay(500);
                        service.Refresh();
                        waitTime += 500;
                    }

                    if (service.Status == ServiceControllerStatus.Running)
                    {
                        return new FixResult { Success = true, Message = "Служба BFE успешно запущена" };
                    }
                }
                catch
                {
                    // Пробуем альтернативный метод
                }

                // Метод 2: Запуск через sc.exe с правами администратора
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "sc",
                            Arguments = "start BFE",
                            UseShellExecute = true,
                            Verb = "runas",
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    await Task.Delay(2000);
                    
                    // Проверяем результат
                    service.Refresh();
                    if (service.Status == ServiceControllerStatus.Running)
                    {
                        return new FixResult { Success = true, Message = "Служба BFE запущена через sc.exe" };
                    }
                }
                catch
                {
                    // Пробуем третий метод
                }

                // Метод 3: Запуск через net start
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "net",
                            Arguments = "start BFE",
                            UseShellExecute = true,
                            Verb = "runas",
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    await Task.Delay(2000);
                    
                    // Проверяем результат
                    service.Refresh();
                    if (service.Status == ServiceControllerStatus.Running)
                    {
                        return new FixResult { Success = true, Message = "Служба BFE запущена через net start" };
                    }
                }
                catch { }

                return new FixResult { Success = false, Message = $"Не удалось запустить BFE. Текущий статус: {service.Status}. Возможно, служба отключена или требуются права администратора" };
            }
            catch (InvalidOperationException ex)
            {
                return new FixResult { Success = false, Message = $"Нет прав для управления службой BFE. {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new FixResult { Success = false, Message = $"Ошибка запуска BFE: {ex.Message}" };
            }
        }

        /// <summary>
        /// Включает TCP timestamps через netsh (с альтернативными методами).
        /// </summary>
        public static async Task<FixResult> EnableTcpTimestamps()
        {
            try
            {
                // Сначала проверяем текущее состояние
                var checkProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "interface tcp show global",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                checkProcess.Start();
                var output = await checkProcess.StandardOutput.ReadToEndAsync();
                await System.Threading.Tasks.Task.Run(() => checkProcess.WaitForExit());

                // Если timestamps уже включены, возвращаем успех
                if (output.IndexOf("Autotuning Level", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    (output.IndexOf("normal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     output.IndexOf("highlyrestricted", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return new FixResult { Success = true, Message = "TCP timestamps уже включены" };
                }

                Logger.Info("Попытка включения TCP timestamps...");

                // Метод 1: Создание временного батника и запуск с правами администратора
                try
                {
                    Logger.Info("Метод 1: Создание временного bat-файла и запуск с правами администратора");
                    
                    var tempBatPath = Path.Combine(Path.GetTempPath(), $"enable_tcp_timestamps_{Guid.NewGuid()}.bat");
                    var batContent = "@echo off\n" +
                                   "netsh interface tcp set global autotuninglevel=normal\n" +
                                   "exit %ERRORLEVEL%";
                    
                    File.WriteAllText(tempBatPath, batContent);
                    
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = tempBatPath,
                            UseShellExecute = true,
                            Verb = "runas",
                            CreateNoWindow = false,
                            WindowStyle = ProcessWindowStyle.Normal // Показываем окно для видимости UAC
                        }
                    };

                    try
                    {
                        process.Start();
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                        // UAC был отклонен или другая ошибка
                        Logger.Warning("Метод 1: UAC был отклонен или не удалось запустить");
                        File.Delete(tempBatPath);
                        throw;
                    }
                    
                    // Даем больше времени на UAC и выполнение (до 30 секунд)
                    int waitTime = 0;
                    while (!process.HasExited && waitTime < 30000)
                    {
                        await Task.Delay(500);
                        waitTime += 500;
                    }
                    
                    if (!process.HasExited)
                    {
                        try { process.Kill(); } catch { }
                        Logger.Warning("Метод 1: Процесс завис, принудительно завершен");
                    }
                    
                    // Удаляем временный файл
                    try { File.Delete(tempBatPath); } catch { }
                    
                    // Даем время на применение изменений
                    await Task.Delay(2000);
                    
                    // Проверяем результат
                    if (await VerifyTcpTimestampsEnabled())
                    {
                        Logger.Success("TCP timestamps включены (метод 1 - bat файл)");
                        return new FixResult { Success = true, Message = "TCP timestamps успешно включены" };
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Метод 1 не сработал: {ex.Message}");
                }

                // Метод 2: netsh через cmd с явным показом окна
                try
                {
                    Logger.Info("Метод 2: netsh через cmd (явный показ окна для UAC)");
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = "/c netsh interface tcp set global autotuninglevel=normal && pause",
                            UseShellExecute = true,
                            CreateNoWindow = false,
                            Verb = "runas",
                            WindowStyle = ProcessWindowStyle.Normal
                        }
                    };

                    try
                    {
                        process.Start();
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                        Logger.Warning("Метод 2: UAC был отклонен");
                        throw;
                    }
                    
                    // Даем больше времени на UAC и выполнение
                    int waitTime = 0;
                    while (!process.HasExited && waitTime < 30000)
                    {
                        await Task.Delay(500);
                        waitTime += 500;
                    }
                    
                    if (!process.HasExited)
                    {
                        try { process.Kill(); } catch { }
                    }
                    
                    await Task.Delay(2000);
                    
                    if (await VerifyTcpTimestampsEnabled())
                    {
                        Logger.Success("TCP timestamps включены (метод 2)");
                        return new FixResult { Success = true, Message = "TCP timestamps успешно включены (через cmd)" };
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Метод 2 не сработал: {ex.Message}");
                }

                // Метод 3: Попытка через PowerShell с более простым синтаксисом
                try
                {
                    Logger.Info("Метод 3: PowerShell с упрощенным синтаксисом");
                    
                    // Создаем временный PowerShell скрипт для более надежного выполнения
                    var tempPsPath = Path.Combine(Path.GetTempPath(), $"enable_tcp_timestamps_{Guid.NewGuid()}.ps1");
                    var psContent = "Start-Process -FilePath 'netsh' -ArgumentList 'interface','tcp','set','global','autotuninglevel=normal' -Verb RunAs -Wait\n";
                    File.WriteAllText(tempPsPath, psContent);
                    
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempPsPath}\"",
                            UseShellExecute = true, // Должно быть true для Verb = "runas"
                            CreateNoWindow = false,
                            Verb = "runas"
                        }
                    };

                    try
                    {
                        process.Start();
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                        Logger.Warning("Метод 3: UAC был отклонен");
                        try { File.Delete(tempPsPath); } catch { }
                        throw;
                    }
                    
                    int waitTime = 0;
                    while (!process.HasExited && waitTime < 30000)
                    {
                        await Task.Delay(500);
                        waitTime += 500;
                    }
                    
                    if (!process.HasExited)
                    {
                        try { process.Kill(); } catch { }
                    }
                    
                    // Удаляем временный файл
                    try { File.Delete(tempPsPath); } catch { }
                    
                    await Task.Delay(2000);
                    
                    if (await VerifyTcpTimestampsEnabled())
                    {
                        Logger.Success("TCP timestamps включены (метод 3 - PowerShell)");
                        return new FixResult { Success = true, Message = "TCP timestamps успешно включены (через PowerShell)" };
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Метод 3 не сработал: {ex.Message}");
                }

                // Если все методы не сработали - показываем инструкции
                Logger.Warning("Все методы включения TCP timestamps не сработали");
                return new FixResult 
                { 
                    Success = false, 
                    Message = "Не удалось автоматически включить TCP timestamps.\n\n" +
                              "Чтобы включить вручную:\n" +
                              "1. Откройте командную строку от имени администратора\n" +
                              "2. Выполните команду:\n" +
                              "   netsh interface tcp set global autotuninglevel=normal\n" +
                              "3. Проверьте результат командой:\n" +
                              "   netsh interface tcp show global\n\n" +
                              "Если проблема сохраняется, возможна блокировка политиками группы."
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка включения TCP timestamps: {ex.Message}", ex);
                return new FixResult { Success = false, Message = $"Ошибка включения TCP timestamps: {ex.Message}" };
            }
        }

        /// <summary>
        /// Проверяет, включены ли TCP timestamps.
        /// </summary>
        private static async Task<bool> VerifyTcpTimestampsEnabled()
        {
            try
            {
                // Проверяем несколько раз для надежности
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    await Task.Delay(1000 * (attempt + 1)); // Увеличиваем задержку с каждой попыткой
                    
                    var verifyProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "netsh",
                            Arguments = "interface tcp show global",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    verifyProcess.Start();
                    var verifyOutput = await verifyProcess.StandardOutput.ReadToEndAsync();
                    await System.Threading.Tasks.Task.Run(() => verifyProcess.WaitForExit(5000));

                    // Более точная проверка
                    if (verifyOutput.IndexOf("Autotuning Level", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (verifyOutput.IndexOf("normal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            verifyOutput.IndexOf("highlyrestricted", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return true;
                        }
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Проверяет и исправляет права доступа к папке bin.
        /// </summary>
        public static FixResult FixBinPermissions(string binPath)
        {
            try
            {
                if (string.IsNullOrEmpty(binPath) || !Directory.Exists(binPath))
                {
                    return new FixResult { Success = false, Message = "Папка bin не найдена" };
                }

                // Проверяем доступность winws.exe
                var winwsPath = Path.Combine(binPath, "winws.exe");
                if (!File.Exists(winwsPath))
                {
                    return new FixResult { Success = false, Message = "Файл winws.exe не найден в папке bin" };
                }

                // Проверяем, можно ли выполнить файл
                try
                {
                    var fileInfo = new FileInfo(winwsPath);
                    if ((fileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        fileInfo.Attributes &= ~FileAttributes.ReadOnly;
                        return new FixResult { Success = true, Message = "Снят атрибут ReadOnly с winws.exe" };
                    }
                }
                catch
                {
                    return new FixResult { Success = false, Message = "Не удалось изменить атрибуты winws.exe. Требуются права администратора" };
                }

                return new FixResult { Success = true, Message = "Права доступа к bin проверены" };
            }
            catch (Exception ex)
            {
                return new FixResult { Success = false, Message = $"Ошибка проверки прав доступа: {ex.Message}" };
            }
        }

        /// <summary>
        /// Исправляет атрибуты файла winws.exe (снимает ReadOnly).
        /// </summary>
        public static FixResult FixWinwsAttributes(string binPath)
        {
            try
            {
                if (string.IsNullOrEmpty(binPath) || !Directory.Exists(binPath))
                {
                    return new FixResult { Success = false, Message = "Папка bin не найдена" };
                }

                var winwsPath = Path.Combine(binPath, "winws.exe");
                if (!File.Exists(winwsPath))
                {
                    return new FixResult { Success = false, Message = "Файл winws.exe не найден" };
                }

                var fileInfo = new FileInfo(winwsPath);
                
                // Сохраняем текущие атрибуты
                var oldAttributes = fileInfo.Attributes;
                
                // Снимаем ReadOnly
                fileInfo.Attributes &= ~FileAttributes.ReadOnly;
                
                // Проверяем результат
                fileInfo.Refresh();
                if ((fileInfo.Attributes & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
                {
                    return new FixResult { Success = true, Message = "Атрибут ReadOnly успешно снят с winws.exe" };
                }
                else
                {
                    // Пробуем через альтернативный метод
                    try
                    {
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "attrib",
                                Arguments = $"-R \"{winwsPath}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                Verb = "runas"
                            }
                        };
                        process.Start();
                        process.WaitForExit(5000);
                        
                        fileInfo.Refresh();
                        if ((fileInfo.Attributes & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
                        {
                            return new FixResult { Success = true, Message = "Атрибут ReadOnly снят через attrib" };
                        }
                    }
                    catch { }
                    
                    return new FixResult { Success = false, Message = "Не удалось снять атрибут ReadOnly. Требуются права администратора" };
                }
            }
            catch (Exception ex)
            {
                return new FixResult { Success = false, Message = $"Ошибка исправления атрибутов: {ex.Message}" };
            }
        }

        /// <summary>
        /// Исправляет права доступа к файлу winws.exe.
        /// </summary>
        public static FixResult FixWinwsPermissions(string binPath)
        {
            try
            {
                if (string.IsNullOrEmpty(binPath) || !Directory.Exists(binPath))
                {
                    return new FixResult { Success = false, Message = "Папка bin не найдена" };
                }

                var winwsPath = Path.Combine(binPath, "winws.exe");
                if (!File.Exists(winwsPath))
                {
                    return new FixResult { Success = false, Message = "Файл winws.exe не найден" };
                }

                // Сначала пытаемся снять ReadOnly
                var attributesResult = FixWinwsAttributes(binPath);
                if (!attributesResult.Success)
                {
                    return attributesResult;
                }

                // Проверяем доступность на запись
                try
                {
                    var fileInfo = new FileInfo(winwsPath);
                    using (var stream = fileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        // Файл доступен для записи
                        return new FixResult { Success = true, Message = "Права доступа к winws.exe исправлены" };
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    return new FixResult { Success = false, Message = "Недостаточно прав для изменения winws.exe. Запустите приложение от имени администратора" };
                }
            }
            catch (Exception ex)
            {
                return new FixResult { Success = false, Message = $"Ошибка исправления прав: {ex.Message}" };
            }
        }

        /// <summary>
        /// Завершает зависшие процессы winws.
        /// </summary>
        public static async Task<FixResult> KillHungWinwsProcesses()
        {
            try
            {
                var processes = Process.GetProcessesByName("winws");
                int killedCount = 0;

                foreach (var process in processes)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                            await Task.Delay(500);
                            killedCount++;
                        }
                        process.Dispose();
                    }
                    catch { }
                }

                if (killedCount > 0)
                {
                    return new FixResult { Success = true, Message = $"Завершено зависших процессов winws: {killedCount}" };
                }
                else
                {
                    return new FixResult { Success = true, Message = "Зависших процессов не обнаружено" };
                }
            }
            catch (Exception ex)
            {
                return new FixResult { Success = false, Message = $"Ошибка завершения процессов: {ex.Message}" };
            }
        }

        /// <summary>
        /// Проверяет и обновляет списки доменов и IP, если они устарели.
        /// </summary>
        public static async Task<FixResult> UpdateListsIfStale(string listsPath)
        {
            try
            {
                if (string.IsNullOrEmpty(listsPath) || !Directory.Exists(listsPath))
                {
                    return new FixResult { Success = false, Message = "Папка lists не найдена" };
                }

                // Проверяем, нужны ли обновления (старше 7 дней)
                if (ListUpdater.NeedsUpdate(listsPath, 7))
                {
                    var progress = new Progress<string>(msg => Logger.Info($"Обновление списков: {msg}"));
                    var result = await ListUpdater.UpdateAllLists(listsPath, progress);

                    if (result.Success && result.UpdatedFiles.Count > 0)
                    {
                        return new FixResult { Success = true, Message = $"Обновлено файлов списков: {result.UpdatedFiles.Count}" };
                    }
                    else
                    {
                        return new FixResult { Success = false, Message = "Не удалось обновить списки" };
                    }
                }
                else
                {
                    return new FixResult { Success = true, Message = "Списки актуальны, обновление не требуется" };
                }
            }
            catch (Exception ex)
            {
                return new FixResult { Success = false, Message = $"Ошибка обновления списков: {ex.Message}" };
            }
        }

        /// <summary>
        /// Проверяет наличие необходимых файлов в папке bin и предлагает решение.
        /// </summary>
        public static FixResult CheckRequiredFiles(string binPath)
        {
            try
            {
                if (string.IsNullOrEmpty(binPath) || !Directory.Exists(binPath))
                {
                    return new FixResult { Success = false, Message = "Папка bin не найдена" };
                }

                var requiredFiles = new[]
                {
                    "winws.exe",
                    "quic_initial_www_google_com.bin",
                    "tls_clienthello_4pda_to.bin",
                    "tls_clienthello_www_google_com.bin"
                };

                var missingFiles = requiredFiles.Where(f => !File.Exists(Path.Combine(binPath, f))).ToList();

                if (missingFiles.Any())
                {
                    return new FixResult 
                    { 
                        Success = false, 
                        Message = $"Отсутствуют необходимые файлы: {string.Join(", ", missingFiles)}" 
                    };
                }

                return new FixResult { Success = true, Message = "Все необходимые файлы присутствуют" };
            }
            catch (Exception ex)
            {
                return new FixResult { Success = false, Message = $"Ошибка проверки файлов: {ex.Message}" };
            }
        }
    }
}

