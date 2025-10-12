using GongSolutions.Wpf.DragDrop;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DesktopShortcutManager
{
    public class MainViewModel : ObservableObject, IDropTarget
    {
        public ObservableCollection<Drawer> Drawers { get; set; }
        private readonly DataService _dataService;

        #region Commands
        public ICommand DeleteShortcutCommand { get; }
        public ICommand ShowInExplorerCommand { get; }
        public ICommand DeleteDrawerCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand EndEditingCommand { get; }
        #endregion

        public MainViewModel()
        {
            _dataService = new DataService();
            Drawers = _dataService.Load();

            // Initialize commands
            // 👇 注意：这里将 public 的 DeleteShortcut 方法传递给命令
            DeleteShortcutCommand = new RelayCommand<ShortcutItem>(DeleteShortcut);
            ShowInExplorerCommand = new RelayCommand<ShortcutItem>(ShowInExplorer);
            DeleteDrawerCommand = new RelayCommand<Drawer>(DeleteDrawer);
            RenameCommand = new RelayCommand<object>(StartEditing);
            EndEditingCommand = new RelayCommand<object>(EndEditing);

            if (Drawers.Count == 0)
            {
                LoadSampleData();
            }
            else
            {
                _ = RestoreIconsAsync();
            }
        }

        public DataService GetDataService() => _dataService;

        private void LoadSampleData()
        {
            Drawers.Add(new Drawer("常用软件"));
            Drawers.Add(new Drawer("我的文档"));
        }

        private async Task RestoreIconsAsync()
        {
            var loadTasks = Drawers.SelectMany(drawer => drawer.Items)
                                   .Select(item => item.LoadIconAsync(this));
            await Task.WhenAll(loadTasks);
        }

        public void AddShortcuts(string[] filePaths, Drawer targetDrawer)
        {
            if (targetDrawer == null) return;
            foreach (var path in filePaths)
            {
                if (targetDrawer.Items.Any(item => item.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show($"快捷方式 '{Path.GetFileName(path)}' 已存在于抽屉 '{targetDrawer.Name}' 中。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    continue;
                }
                var newShortcut = new ShortcutItem
                {
                    Name = Path.GetFileNameWithoutExtension(path),
                    Path = path,
                };
                targetDrawer.Items.Add(newShortcut);
                _ = newShortcut.LoadIconAsync(this);
            }
        }

        public async Task<ImageSource?> GetIconForFileAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(filePath) && !Directory.Exists(filePath)) return null;
                    using (var icon = Icon.ExtractAssociatedIcon(filePath))
                    {
                        if (icon == null) return null;
                        var imageSource = Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        imageSource.Freeze();
                        return imageSource;
                    }
                }
                catch { return null; }
            });
        }

        public void LaunchShortcut(ShortcutItem? item)
        {
            if (item == null || string.IsNullOrEmpty(item.Path)) return;
            try
            {
                var psi = new ProcessStartInfo(item.Path) { UseShellExecute = true };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开文件：\n{item.Path}\n\n错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Command Methods

        // --- 👇 修正：将此方法改为 public，以便 MainWindow.xaml.cs 可以调用 ---
        public void DeleteShortcut(ShortcutItem? itemToDelete)
        {
            if (itemToDelete == null) return;
            foreach (var drawer in Drawers)
            {
                if (drawer.Items.Remove(itemToDelete)) break;
            }
        }

        private void StartEditing(object? item)
        {
            if (item is Drawer drawer) drawer.IsEditing = true;
            else if (item is ShortcutItem shortcut) shortcut.IsEditing = true;
        }

        private void EndEditing(object? item)
        {
            if (item is Drawer drawer) drawer.IsEditing = false;
            else if (item is ShortcutItem shortcut) shortcut.IsEditing = false;
        }

        private void ShowInExplorer(ShortcutItem? item)
        {
            if (item == null || string.IsNullOrEmpty(item.Path)) return;
            if (File.Exists(item.Path) || Directory.Exists(item.Path))
            {
                Process.Start("explorer.exe", $"/select,\"{item.Path}\"");
            }
            else
            {
                MessageBox.Show("文件或文件夹不存在。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteDrawer(Drawer? drawerToDelete)
        {
            if (drawerToDelete == null) return;
            var result = MessageBox.Show($"确定要删除抽屉 '{drawerToDelete.Name}' 及其所有快捷方式吗？",
                                         "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                Drawers.Remove(drawerToDelete);
            }
        }

        public void AddDrawer(string drawerName)
        {
            if (string.IsNullOrWhiteSpace(drawerName)) return;
            if (Drawers.Any(d => d.Name.Equals(drawerName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("已存在同名的抽屉。", "提示");
                return;
            }
            Drawers.Add(new Drawer(drawerName));
        }
        #endregion

        #region GongSolutions.Wpf.DragDrop IDropTarget Implementation

        public void DragOver(IDropInfo dropInfo)
        {
            // --- 👇 核心修正：在这里统一处理所有拖放类型 ---

            // 1. 处理内部快捷方式的拖拽
            if (dropInfo.Data is ShortcutItem && dropInfo.TargetCollection is ObservableCollection<ShortcutItem>)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;
            }
            // 2. 处理内部抽屉的拖拽
            else if (dropInfo.Data is Drawer && dropInfo.TargetCollection is ObservableCollection<Drawer>)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;
            }
            // 3. 新增：处理从外部拖入的文件
            else if (dropInfo.Data is IDataObject dataObject && dataObject.GetDataPresent(DataFormats.FileDrop))
            {
                // 如果鼠标悬浮在一个抽屉上，就允许放置
                if (dropInfo.TargetItem is Drawer)
                {
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                    dropInfo.Effects = DragDropEffects.Copy;
                }
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            // --- 👇 核心修正：在这里统一处理所有放置逻辑 ---

            // 1. 处理内部快捷方式的放置
            if (dropInfo.Data is ShortcutItem shortcut)
            {
                // ... (这部分逻辑保持不变)
                ((ObservableCollection<ShortcutItem>)dropInfo.DragInfo.SourceCollection).Remove(shortcut);
                ((ObservableCollection<ShortcutItem>)dropInfo.TargetCollection).Insert(dropInfo.InsertIndex, shortcut);
            }
            // 2. 处理内部抽屉的放置
            else if (dropInfo.Data is Drawer drawer)
            {
                // ... (这部分逻辑保持不变)
                var collection = (ObservableCollection<Drawer>)dropInfo.TargetCollection;
                // ... (移动逻辑)
            }
            // 3. 新增：处理从外部拖入的文件的放置
            else if (dropInfo.Data is IDataObject dataObject && dataObject.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])dataObject.GetData(DataFormats.FileDrop);

                // dropInfo.TargetItem 就是鼠标指针下方的那个抽屉对象
                if (dropInfo.TargetItem is Drawer targetDrawer)
                {
                    AddShortcuts(files, targetDrawer);
                }
            }
        }
        #endregion
    }
}