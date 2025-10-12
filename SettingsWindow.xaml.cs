// C#: SettingsWindow.xaml.cs (确认此结构)
using DesktopShortcutManager.Services;
using System.Windows;

namespace DesktopShortcutManager
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            // DataContext直接设置为那个共享的、会“说话”的Model实例
            this.DataContext = SettingsService.Instance.CurrentSettings;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}