using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace DesktopShortcutManager
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            this.DataContext = _viewModel;

            var settingsModel = SettingsService.Instance.CurrentSettings;
            var opacityBinding = new Binding("OpacityValue")
            {
                Source = settingsModel,
                Converter = (IValueConverter)this.Resources["PercentageToOpacity"]
            };
            this.SetBinding(Window.OpacityProperty, opacityBinding);

            this.Closing += MainWindow_Closing;
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
        private void Shortcut_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ShortcutItem item)
            {
                _viewModel.LaunchShortcut(item);
            }
        }

        private void DeleteShortcut_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ShortcutItem item)
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