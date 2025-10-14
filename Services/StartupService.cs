using Microsoft.Win32; // 引入操作注册表所需的命名空间
using System;
using System.Diagnostics;
using System.Reflection;

namespace DesktopShortcutManager.Services
{
    public static class StartupService
    {
        // 定义我们应用在注册表中的唯一名称
        private const string AppName = "DesktopShortcutManager";

        // 定义注册表路径
        private const string RunRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// 设置或取消开机自启动
        /// </summary>
        /// <param name="isEnabled">True to enable, False to disable.</param>
        public static void SetStartup(bool isEnabled)
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, true))
                {
                    if (key == null) return;

                    if (isEnabled)
                    {
                        // 获取当前正在运行的 .exe 文件的完整路径
                        // 使用 AppContext.BaseDirectory 更可靠，可以找到 .exe 而不是 .dll
                        string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
                        if (!string.IsNullOrEmpty(exePath))
                        {
                            // 创建或更新注册表项
                            key.SetValue(AppName, $"\"{exePath}\"");
                        }
                    }
                    else
                    {
                        // 如果存在，则删除注册表项
                        if (key.GetValue(AppName) != null)
                        {
                            key.DeleteValue(AppName, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 在真实应用中，这里应该记录日志
                Console.WriteLine($"Error setting startup: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查当前是否已设置为开机自启动
        /// </summary>
        public static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, false))
                {
                    if (key == null) return false;

                    // 检查是否存在以我们应用命名的键值对
                    object? value = key.GetValue(AppName);
                    return value != null;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}