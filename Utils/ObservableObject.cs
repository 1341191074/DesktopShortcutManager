// C#: ObservableObject.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DesktopShortcutManager.Utils
{
    // 一个实现了INotifyPropertyChanged接口的基类，可以被所有需要通知UI更新的Model或ViewModel继承
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}