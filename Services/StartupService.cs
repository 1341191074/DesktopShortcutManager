using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows;

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

                    // 使用我们自己的、纯净的快捷方式创建方法
                    CreateShortcut(ShortcutPath, exePath, AppContext.BaseDirectory);
                }
                else
                {
                    if (File.Exists(ShortcutPath))
                    {
                        File.Delete(ShortcutPath);
                    }
                }
            }
            catch (Exception ex)
            {
                // 关键修正：现在，任何错误都会通过MessageBox弹出，我们不会再“盲目”了！
                MessageBox.Show($"设置开机自启动时发生错误: \n\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static bool IsStartupEnabled()
        {
            return File.Exists(ShortcutPath);
        }

        #region Pure .LNK Creation Logic (No COM)

        private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory)
        {
            IShellLink link = (IShellLink)new ShellLink();

            // 设置快捷方式的属性
            link.SetPath(targetPath);
            link.SetWorkingDirectory(workingDirectory);

            // 保存快捷方式文件
            IPersistFile file = (IPersistFile)link;
            file.Save(shortcutPath, false);
        }

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        internal class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        internal interface IShellLink
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxChars);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        #endregion
    }
}