// C#: SettingsModel.cs (确认此结构)
namespace DesktopShortcutManager
{
    public class SettingsModel : ObservableObject
    {
        private bool _isLocked;
        public bool IsLocked
        {
            get => _isLocked;
            set { _isLocked = value; OnPropertyChanged(); } // 关键：调用 OnPropertyChanged
        }

        private double _opacityValue = 100.0;
        public double OpacityValue
        {
            get => _opacityValue;
            set { _opacityValue = value; OnPropertyChanged(); } // 关键：调用 OnPropertyChanged
        }
    }
}