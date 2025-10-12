// C#: DataService.cs
using DesktopShortcutManager.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json; // <-- 导入Json命名空间

namespace DesktopShortcutManager.Services
{
    public class DataService
    {
        private readonly string _filePath;

        public DataService()
        {
            // 构造函数中确定文件路径
            // AppData/ShortcutManager/data.json
            // string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDataPath = "";
            string appFolderPath = Path.Combine(appDataPath, "ShortcutManager");

            // 确保目录存在
            Directory.CreateDirectory(appFolderPath);

            _filePath = Path.Combine(appFolderPath, "data.json");


        }

        // 加载数据
        public ObservableCollection<Drawer> Load()
        {
            if (!File.Exists(_filePath))
            {
                // 文件不存在，返回一个空集合
                return new ObservableCollection<Drawer>();
            }

            try
            {
                string jsonString = File.ReadAllText(_filePath);
                var drawers = JsonSerializer.Deserialize<ObservableCollection<Drawer>>(jsonString);
                return drawers ?? new ObservableCollection<Drawer>(); // 如果反序列化结果为null，也返回空集合
            }
            catch (Exception ex)
            {
                // 处理读取或解析错误
                // 在真实应用中，这里应该记录日志
                Console.WriteLine($"Error loading data: {ex.Message}");
                return new ObservableCollection<Drawer>();
            }
        }

        // 保存数据
        public void Save(ObservableCollection<Drawer> drawers)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true }; // 让JSON格式化，易于阅读
                string jsonString = JsonSerializer.Serialize(drawers, options);
                File.WriteAllText(_filePath, jsonString);
            }
            catch (Exception ex)
            {
                // 处理写入错误
                Console.WriteLine($"Error saving data: {ex.Message}");
            }
        }
    }
}