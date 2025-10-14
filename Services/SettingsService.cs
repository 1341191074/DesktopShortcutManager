// C#: SettingsService.cs
using DesktopShortcutManager.Models;
using System;
using System.IO;
using System.Text.Json;

namespace DesktopShortcutManager.Services
{
    public class SettingsService
    {
        // --- Singleton Pattern Implementation ---
        private static readonly Lazy<SettingsService> _instance = new Lazy<SettingsService>(() => new SettingsService());
        public static SettingsService Instance => _instance.Value;
        // --- End Singleton ---

        private readonly string _filePath;
        public SettingsModel CurrentSettings { get; private set; }

        private SettingsService()
        {
            // Initialize and load settings
            // string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDataPath = "";
            string appFolderPath = Path.Combine(appDataPath, "ShortcutManager");
            Directory.CreateDirectory(appFolderPath);
            _filePath = Path.Combine(appFolderPath, "settings.json");

            CurrentSettings = Load();
        }

        private SettingsModel Load()
        {
            SettingsModel settings;
            if (!File.Exists(_filePath))
            {
                return new SettingsModel(); // Return default settings
            }
            try
            {
                string jsonString = File.ReadAllText(_filePath);
                settings = JsonSerializer.Deserialize<SettingsModel>(jsonString) ?? new SettingsModel();
            }
            catch
            {
                settings = new SettingsModel(); // Return default on error
            }
            // 加载完配置后，用注册表的真实状态覆盖配置中的值
            settings.RunAtStartup = StartupService.IsStartupEnabled();
            return settings;
        }

        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(CurrentSettings, options);
                File.WriteAllText(_filePath, jsonString);
            }
            catch
            {
                // Handle save error
            }
        }
    }
}