using System;
using System.Windows.Forms;

namespace NeoZapret
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Логирование запуска приложения
            Logger.Info("════════════════════════════════════════");
            Logger.Info("NeoZapret запущен");
            Logger.Info($"Версия: 3.1.0-alpha");
            Logger.Info($"Дата: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Logger.Info("════════════════════════════════════════");

            // Проверка прав администратора
            if (!IsAdministrator())
            {
                Logger.Warning("Приложение запущено без прав администратора");
                MessageBox.Show(
                    "Для работы приложения требуются права администратора!",
                    "NeoZapret",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                
                // Попытка запуска с правами администратора
                try
                {
                    System.Diagnostics.ProcessStartInfo procInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        UseShellExecute = true,
                        FileName = Application.ExecutablePath,
                        Verb = "runas"
                    };
                    System.Diagnostics.Process.Start(procInfo);
                    Logger.Info("Попытка перезапуска с правами администратора");
                }
                catch (Exception ex)
                {
                    Logger.Error("Не удалось запустить с правами администратора", ex);
                    MessageBox.Show("Не удалось запустить с правами администратора!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            Logger.Info("Права администратора подтверждены");

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                Logger.Error("Критическая ошибка при запуске приложения", ex);
                MessageBox.Show($"Критическая ошибка:\n\n{ex.Message}\n\nДетали сохранены в логах.", 
                    "NeoZapret - Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try
                {
                    // Освобождаем ресурсы HttpClient
                    HttpClientHelper.Dispose();
                    
                    // Очищаем кэш аргументов стратегий
                    StrategyArgumentsGenerator.ClearCache();
                    
                    Logger.Flush(); // Принудительно записываем все логи при завершении
                    Logger.Info("NeoZapret завершает работу");
                    Logger.Flush(); // Финальная запись
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при завершении работы: {ex.Message}");
                }
            }
        }

        static bool IsAdministrator()
        {
            try
            {
                System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
}

