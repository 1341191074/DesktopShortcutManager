using DesktopShortcutManager.Models;
using DesktopShortcutManager.Services;
using DesktopShortcutManager.ViewModels;
using System;
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

            // Manually create binding for Opacity as it's more reliable after window initialization
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
            // The real-time save handles most cases, but this ensures a final save on clean exit
            // and is crucial for saving the settings.
            _viewModel.GetDataService().Save(_viewModel.Drawers);
            SettingsService.Instance.Save();
        }

        private void TrayExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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
                // The DataContext of the clicked element is the ShortcutItem
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
            e.Handled = true; // Prevents the click from bubbling up to the parent item
        }

        private void AddDrawer_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.AddDrawer(NewDrawerNameTextBox.Text);
            NewDrawerNameTextBox.Clear();
            NewDrawerNameTextBox.Focus();
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
                // Force the binding to update
                textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

                // End the editing state
                var dataContext = textBox.DataContext;
                if (dataContext is Drawer drawer) drawer.IsEditing = false;
                else if (dataContext is ShortcutItem shortcut) shortcut.IsEditing = false;
            }
        }
        #endregion
    }
}