using System;
using DesktopShortcutManager.Models;
using DesktopShortcutManager.Services;
using DesktopShortcutManager.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;

namespace DesktopShortcutManager
{
    public partial class MainWindow : Window
    {
        // 系统消息：非客户区鼠标左键按下（触发窗口移动的核心消息）
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        // 非客户区区域代码：标题栏（点击此区域会触发移动）
        private const int HTCAPTION = 0x0002;
       
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            this.DataContext = _viewModel;

            // Manually create binding for Opacity
            var settingsModel = SettingsService.Instance.CurrentSettings;
            var opacityBinding = new Binding("OpacityValue")
            {
                Source = settingsModel,
                Converter = (IValueConverter)this.Resources["PercentageToOpacity"]
            };
            this.SetBinding(Window.OpacityProperty, opacityBinding);

            this.Closing += MainWindow_Closing;
        }

        // 窗口初始化时注册消息钩子
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // 获取窗口句柄关联的消息源
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            if (source != null)
            {
                // 注册消息处理函数
                source.AddHook(WndProc);
            }
        }

        // 消息处理函数：拦截并控制移动消息
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // 仅当“禁止移动”且触发的是“标题栏鼠标按下”消息时，拦截移动
            if (SettingsService.Instance.CurrentSettings.IsLocked && msg == WM_NCLBUTTONDOWN && wParam.ToInt32() == HTCAPTION)
            {
                // 标记消息为“已处理”，阻止系统执行默认的移动逻辑
                handled = true;
            }
            return IntPtr.Zero;
        }


        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            _viewModel.GetDataService().Save(_viewModel.Drawers);
            SettingsService.Instance.Save();
        }

        #region Window Control and Settings
        private void DragHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!SettingsService.Instance.CurrentSettings.IsLocked)
            {
                this.DragMove();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow { Owner = this };
            settingsWindow.ShowDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion

        #region Item Interactions
        private void Shortcut_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!SettingsService.Instance.CurrentSettings.OpenWithDoubleClick)
            {
                if (sender is FrameworkElement element && element.DataContext is ShortcutItem item)
                {
                    _viewModel.LaunchShortcut(item);
                }
            }
        }

        private void DeleteShortcut_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ShortcutItem item)
            {
                _viewModel.DeleteShortcut(item);
            }
            e.Handled = true;
        }

        private void AddDrawer_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.AddDrawer(NewDrawerNameTextBox.Text);
            NewDrawerNameTextBox.Clear();
        }

        private void NewDrawerNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddDrawer_Click(sender, e);
            }
        }

        private void EditableTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox textBox)
            {
                textBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                var dataContext = textBox.DataContext;
                if (dataContext is Drawer drawer) drawer.IsEditing = false;
                else if (dataContext is ShortcutItem shortcut) shortcut.IsEditing = false;
            }
        }
        #endregion
    }
}