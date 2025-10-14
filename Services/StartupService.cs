using System;
using System.IO;
using System.Windows;
using IWshRuntimeLibrary; // 核心修正：现在我们只使用这个COM库

namespace DesktopShortcutManager.Services
{
    public static class StartupService
    {
        private const string ShortcutName = "DesktopShortcutManager.lnk";
        private static readonly string StartupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        private static readonly string ShortcutPath = Path.Combine(StartupFolderPath, ShortcutName);

        public static void SetStartup(bool isEnabled)
        {
            try
            {
                if (isEnabled)
                {
                    string? exePath = Environment.ProcessPath;
                    if (string.IsNullOrEmpty(exePath)) return;

                    string? workingDirectory = Path.GetDirectoryName(exePath);
                    if (string.IsNullOrEmpty(workingDirectory)) return;

                    // --- 核心修正：使用 WshShell 来创建快捷方式 ---
                    WshShell shell = new WshShell();
                    IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(ShortcutPath);

                    shortcut.Description = "启动桌面快捷方式管理器";
                    shortcut.TargetPath = exePath;
                    shortcut.WorkingDirectory = workingDirectory;

                    shortcut.Save();
                }
                else
                {
                    if (System.IO.File.Exists(ShortcutPath))
                    {
                        System.IO.File.Delete(ShortcutPath);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置开机自启动时发生错误: \n\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static bool IsStartupEnabled()
        {
            return System.IO.File.Exists(ShortcutPath);
        }
    }
}