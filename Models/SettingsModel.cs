using DesktopShortcutManager.Utils; 
using System.Text.Json.Serialization;

namespace DesktopShortcutManager.Models
{
    public class SettingsModel : ObservableObject
    {
        private bool _isLocked;
        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                if (_isLocked == value) return;
                _isLocked = value;
                OnPropertyChanged();
            }
        }

        private double _opacityValue = 100.0;
        public double OpacityValue
        {
            get => _opacityValue;
            set
            {
                if (_opacityValue == value) return;
                _opacityValue = value;
                OnPropertyChanged();
            }
        }

        // --- 核心数据源 ---
        private bool _openWithDoubleClick = true;
        public bool OpenWithDoubleClick
        {
            get => _openWithDoubleClick;
            set
            {
                if (_openWithDoubleClick == value) return;
                _openWithDoubleClick = value;
                OnPropertyChanged();
            }
        }

        // --- 用于UI绑定的代理属性 ---
        [JsonIgnore] // 此属性不需要被保存到JSON文件
        public bool IsOpenWithSingleClick
        {
            get => !OpenWithDoubleClick;
            set => OpenWithDoubleClick = !value;
        }

        [JsonIgnore] // 此属性不需要被保存到JSON文件
        public bool IsOpenWithDoubleClick
        {
            get => OpenWithDoubleClick;
            set => OpenWithDoubleClick = value;
        }

        private double _iconSize = 32;
        public double IconSize
        {
            get => _iconSize;
            set
            {
                if (_iconSize == value) return;
                _iconSize = value;
                OnPropertyChanged();
            }
        }
        // --- 窗口几何信息 ---
        // 我们给一个默认值，防止第一次启动时位置不确定
        public double Top { get; set; } = 100;
        public double Left { get; set; } = 100;
        public double Width { get; set; } = 280;
        public double Height { get; set; } = 700;
    }
}