using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace NeoZapret
{
    public static class SettingsManager
    {
        private static string SettingsFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NeoZapret",
            "settings.json");

        private static RegistryKey GetSettingsKey()
        {
            return Registry.CurrentUser.CreateSubKey(@"Software\NeoZapret", true);
        }

        public static void SaveSetting(string key, string value)
        {
            try
            {
                using (var regKey = GetSettingsKey())
                {
                    regKey?.SetValue(key, value ?? "");
                }
            }
            catch { }
        }

        public static string LoadSetting(string key, string defaultValue = "")
        {
            try
            {
                using (var regKey = GetSettingsKey())
                {
                    return regKey?.GetValue(key, defaultValue)?.ToString() ?? defaultValue;
                }
            }
            catch
            {
                return defaultValue;
            }
        }

        public static void SaveBoolSetting(string key, bool value)
        {
            SaveSetting(key, value ? "1" : "0");
        }

        public static bool LoadBoolSetting(string key, bool defaultValue = false)
        {
            var value = LoadSetting(key, defaultValue ? "1" : "0");
            return value == "1";
        }

        public static void SaveIntSetting(string key, int value)
        {
            SaveSetting(key, value.ToString());
        }

        public static int LoadIntSetting(string key, int defaultValue = 0)
        {
            var value = LoadSetting(key, defaultValue.ToString());
            if (int.TryParse(value, out int result))
                return result;
            return defaultValue;
        }
    }
}



