using DesktopShortcutManager.Models;
using DesktopShortcutManager.Services;
using DesktopShortcutManager.Utils;
using GongSolutions.Wpf.DragDrop;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DesktopShortcutManager.ViewModels
{
    public class MainViewModel : ObservableObject, IDropTarget
    {
        public ObservableCollection<Drawer> Drawers { get; set; }
        private readonly DataService _dataService;
        private Timer? _debounceTimer;

        #region Commands
        public ICommand DeleteShortcutCommand { get; }
        public ICommand ShowInExplorerCommand { get; }
        public ICommand DeleteDrawerCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand EndEditingCommand { get; }
        public ICommand LaunchShortcutCommand { get; }
        #endregion

        public MainViewModel()
        {
            _dataService = new DataService();
            Drawers = _dataService.Load();

            // Initialize commands
            DeleteShortcutCommand = new RelayCommand<ShortcutItem>(DeleteShortcut);
            ShowInExplorerCommand = new RelayCommand<ShortcutItem>(ShowInExplorer);
            DeleteDrawerCommand = new RelayCommand<Drawer>(DeleteDrawer);
            RenameCommand = new RelayCommand<object>(StartEditing);
            EndEditingCommand = new RelayCommand<object>(EndEditing);
            LaunchShortcutCommand = new RelayCommand<ShortcutItem>(LaunchShortcut);

            if (Drawers.Count == 0)
            {
                LoadSampleData();
            }
            else
            {
                _ = RestoreIconsAsync();
            }

            SubscribeToCollectionChanges();
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

        #region Real-time Saving
        private void SubscribeToCollectionChanges()
        {
            Drawers.CollectionChanged += OnDataChanged;
            foreach (var drawer in Drawers)
            {
                drawer.Items.CollectionChanged += OnDataChanged;
            }
        }

        private void OnDataChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(
                callback: _ => _dataService.Save(Drawers),
                state: null,
                dueTime: 500,
                period: Timeout.Infinite);
        }
        #endregion

        #region Core Logic Methods
        public void AddShortcuts(string[] filePaths, Drawer targetDrawer)
        {
            if (targetDrawer == null) return;
            foreach (var path in filePaths)
            {
                var (resolvedPath, arguments) = ShortcutResolver.Resolve(path);
                if (targetDrawer.Items.Any(item => item.Path.Equals(resolvedPath, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show($"快捷方式 '{Path.GetFileName(resolvedPath)}' 已存在于抽屉 '{targetDrawer.Name}' 中。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    continue;
                }

                var newShortcut = new ShortcutItem
                {
                    Name = Path.GetFileNameWithoutExtension(path), // 名字我们仍然用 .lnk 文件的名字
                    Path = resolvedPath, // 路径保存的是解析后的真实路径
                    Arguments = arguments // 保存解析出的参数
                };
                targetDrawer.Items.Add(newShortcut);
                _ = newShortcut.LoadIconAsync(this);
            }
        }
        public void LaunchShortcut(ShortcutItem? item)
        {
            if (item == null || string.IsNullOrEmpty(item.Path)) return;
            try
            {
                var psi = new ProcessStartInfo(item.Path)
                {
                    UseShellExecute = true,
                    Arguments = item.Arguments // 👇 核心修正：将保存的参数传递给进程
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开目标：\n{item.Path}\n\n错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
        #endregion

        #region Command Methods
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
                drawerToDelete.Items.CollectionChanged -= OnDataChanged;
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
            var newDrawer = new Drawer(drawerName);
            newDrawer.Items.CollectionChanged += OnDataChanged;
            Drawers.Add(newDrawer);
        }
        #endregion

        #region GongSolutions.Wpf.DragDrop IDropTarget Implementation
        public void DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is ShortcutItem && dropInfo.TargetCollection is ObservableCollection<ShortcutItem>)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;
            }
            else if (dropInfo.Data is Drawer && dropInfo.TargetCollection is ObservableCollection<Drawer>)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;
            }
            else if (dropInfo.Data is IDataObject dataObject && dataObject.GetDataPresent(DataFormats.FileDrop))
            {
                if (dropInfo.TargetItem is Drawer)
                {
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                    dropInfo.Effects = DragDropEffects.Copy;
                }
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is ShortcutItem shortcut)
            {
                ((ObservableCollection<ShortcutItem>)dropInfo.DragInfo.SourceCollection).Remove(shortcut);
                ((ObservableCollection<ShortcutItem>)dropInfo.TargetCollection).Insert(dropInfo.InsertIndex, shortcut);
            }
            else if (dropInfo.Data is Drawer drawer)
            {
                var collection = (ObservableCollection<Drawer>)dropInfo.TargetCollection;
                int oldIndex = collection.IndexOf(drawer);
                int newIndex = dropInfo.InsertIndex;
                if (oldIndex >= 0 && newIndex >= 0)
                {
                    if (oldIndex < newIndex) newIndex--;
                    collection.Move(oldIndex, newIndex);
                }
            }
            else if (dropInfo.Data is IDataObject dataObject && dataObject.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])dataObject.GetData(DataFormats.FileDrop);
                if (dropInfo.TargetItem is Drawer targetDrawer)
                {
                    AddShortcuts(files, targetDrawer);
                }
            }
        }
        #endregion
    }
}