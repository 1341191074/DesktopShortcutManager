// C#: Services/ShortcutResolver.cs
using IWshRuntimeLibrary; // 引入我们刚刚添加的COM库
using System.IO;

namespace DesktopShortcutManager.Services
{
    public static class ShortcutResolver
    {
        public static (string Path, string? Arguments) Resolve(string filePath)
        {
            // 如果文件不是 .lnk 文件，直接返回原始路径
            if (Path.GetExtension(filePath).ToLower() != ".lnk")
            {
                return (filePath, null);
            }

            // 如果 .lnk 文件不存在，也返回原始路径作为后备
            if (!System.IO.File.Exists(filePath))
            {
                return (filePath, null);
            }

            try
            {
                // 创建 WshShell 对象
                WshShell shell = new WshShell();
                // 创建快捷方式对象的引用
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(filePath);

                // 返回解析出的目标路径和参数
                return (shortcut.TargetPath, shortcut.Arguments);
            }
            catch
            {
                // 如果解析失败，返回原始路径作为后备
                return (filePath, null);
            }
        }
    }
}