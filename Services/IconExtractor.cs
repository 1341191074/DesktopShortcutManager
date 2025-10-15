using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DesktopShortcutManager.Services
{
    public static class IconExtractor
    {
        #region Win32 API Declarations
        [DllImport("shell32.dll", EntryPoint = "#727")]
        private static extern int SHGetImageList(int iImageList, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object? ppv);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);
        [DllImport("user32.dll")]
        private static extern bool GetIconInfo(IntPtr hIcon, ref ICONINFO pIconInfo);

        [ComImport, Guid("46EB5926-582E-4017-9FDF-E8998DAA0950"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IImageList
        {
            [PreserveSig] int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);
            [PreserveSig] int ReplaceIcon(int i, IntPtr hicon, ref int pi);
            [PreserveSig] int SetOverlayImage(int iImage, int iOverlay);
            [PreserveSig] int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);
            [PreserveSig] int AddMasked(IntPtr hbmImage, int crMask, ref int pi);
            [PreserveSig] int Draw(ref IMAGELISTDRAWPARAMS pimldp);
            [PreserveSig] int Remove(int i);
            [PreserveSig] int GetIcon(int i, int flags, ref IntPtr picon);
            [PreserveSig] int GetIconSize(ref int cx, ref int cy); // 获取图标尺寸
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGELISTDRAWPARAMS { /* not needed */ }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }
        #endregion

        // 修正系统图标尺寸常量（按标准定义）
        private const uint SHGFI_SYSICONINDEX = 0x4000;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x0010;
        private const uint SHGFI_ICON = 0x000000100; // 直接获取图标
        private const int ILD_TRANSPARENT = 0x1;

        // 图标尺寸等级（从大到小排序）
        private const int SHIL_JUMBO = 0x00000004;      // 256x256
        private const int SHIL_128X128 = 0x00000005;    // 128x128
        private const int SHIL_EXTRALARGE = 0x00000003; // 48x48
        private const int SHIL_LARGE = 0x00000002;      // 32x32
        private const int SHIL_MEDIUM = 0x00000001;     // 16x16

        // 主方法：获取图标（增加多重 fallback 机制）
        public static async Task<ImageSource?> GetIconAsync(string filePath, Dispatcher dispatcher)
        {
            try
            {
                // 优先尝试系统图标列表
                var systemIcon = await GetSystemImageListIconAsync(filePath, dispatcher);
                if (systemIcon != null)
                    return systemIcon;

                // Fallback 1: 直接从文件获取图标
                var directIcon = await GetDirectFileIconAsync(filePath, dispatcher);
                if (directIcon != null)
                    return directIcon;

                // Fallback 2: 使用默认图标
                return await GetDefaultIconAsync(dispatcher);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取图标总失败: {ex.Message}");
                return null;
            }
        }

        // 方法1: 通过系统图像列表获取图标（原逻辑优化版）
        private static async Task<ImageSource?> GetSystemImageListIconAsync(string filePath, Dispatcher dispatcher)
        {
            return await dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var sfi = new SHFILEINFO();
                    // 获取文件图标索引
                    var hResult = SHGetFileInfo(filePath, 0, ref sfi, (uint)Marshal.SizeOf(sfi),
                                               SHGFI_SYSICONINDEX | SHGFI_USEFILEATTRIBUTES);
                    if (hResult == IntPtr.Zero || sfi.iIcon == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"文件 {Path.GetFileName(filePath)} 无法获取图标索引");
                        return null;
                    }

                    IImageList? imageList = null;
                    int[] iconSizes = new[] { SHIL_JUMBO, SHIL_128X128, SHIL_EXTRALARGE, SHIL_LARGE, SHIL_MEDIUM };
                    Guid iidImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");

                    foreach (var size in iconSizes)
                    {
                        try
                        {
                            // 获取图像列表
                            int result = SHGetImageList(size, ref iidImageList, out object? imageListObj);
                            if (result != 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"获取{GetSizeDescription(size)}列表失败，错误码: {result}");
                                continue;
                            }

                            imageList = imageListObj as IImageList;
                            if (imageList == null) continue;

                            // 获取图标尺寸（增加有效性检查）
                            int cx = 0, cy = 0;
                            var sizeResult = imageList.GetIconSize(ref cx, ref cy);
                            if (sizeResult != 0 || cx <= 0 || cy <= 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"获取{GetSizeDescription(size)}尺寸失败，返回值: {sizeResult}, 尺寸: {cx}x{cy}");
                                continue;
                            }

                            // 提取图标
                            IntPtr hIcon = IntPtr.Zero;
                            int getIconResult = imageList.GetIcon(sfi.iIcon, ILD_TRANSPARENT, ref hIcon);
                            if (getIconResult == 0 && hIcon != IntPtr.Zero)
                            {
                                // 适配DPI缩放
                                var dpi = VisualTreeHelper.GetDpi(Application.Current.MainWindow);
                                double scaleX = dpi.DpiScaleX;
                                double scaleY = dpi.DpiScaleY;

                                int scaledWidth = (int)Math.Ceiling(cx * scaleX);
                                int scaledHeight = (int)Math.Ceiling(cy * scaleY);

                                // 确保缩放后尺寸有效
                                if (scaledWidth <= 0 || scaledHeight <= 0)
                                {
                                    scaledWidth = Math.Max(cx, 32);
                                    scaledHeight = Math.Max(cy, 32);
                                }

                                System.Diagnostics.Debug.WriteLine(
                                    $"文件: {Path.GetFileName(filePath)} " +
                                    $"尺寸等级: {GetSizeDescription(size)} " +
                                    $"原始尺寸: {cx}x{cy} " +
                                    $"缩放后: {scaledWidth}x{scaledHeight}");

                                // 创建BitmapSource（增加尺寸有效性检查）
                                var imageSource = Imaging.CreateBitmapSourceFromHIcon(
                                    hIcon,
                                    Int32Rect.Empty,
                                    BitmapSizeOptions.FromWidthAndHeight(scaledWidth, scaledHeight)
                                );
                                imageSource.Freeze();
                                DestroyIcon(hIcon);
                                Marshal.ReleaseComObject(imageList);
                                return imageSource;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"获取{GetSizeDescription(size)}图标失败: {ex.Message}");
                        }
                        finally
                        {
                            if (imageList != null)
                                Marshal.ReleaseComObject(imageList);
                        }
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"系统图像列表获取失败: {ex.Message}");
                    return null;
                }
            });
        }

        // 方法2: 直接从文件获取图标（Fallback机制）
        private static async Task<ImageSource?> GetDirectFileIconAsync(string filePath, Dispatcher dispatcher)
        {
            return await dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var sfi = new SHFILEINFO();
                    // 直接获取图标（不通过图像列表）
                    var hIcon = SHGetFileInfo(filePath, 0, ref sfi, (uint)Marshal.SizeOf(sfi),
                                             SHGFI_ICON | SHGFI_USEFILEATTRIBUTES);
                    if (hIcon == IntPtr.Zero || sfi.hIcon == IntPtr.Zero)
                    {
                        System.Diagnostics.Debug.WriteLine($"文件 {Path.GetFileName(filePath)} 无法直接获取图标");
                        return null;
                    }

                    // 获取图标尺寸
                    var iconInfo = new ICONINFO();
                    GetIconInfo(sfi.hIcon, ref iconInfo);
                    int cx = (int)iconInfo.xHotspot * 2; // 热点坐标的2倍约等于图标尺寸
                    int cy = (int)iconInfo.yHotspot * 2;
                    DestroyIcon(iconInfo.hbmMask);
                    DestroyIcon(iconInfo.hbmColor);

                    // 确保尺寸有效
                    if (cx <= 0 || cy <= 0)
                    {
                        cx = 32;
                        cy = 32;
                    }

                    // 适配DPI缩放
                    var dpi = VisualTreeHelper.GetDpi(Application.Current.MainWindow);
                    int scaledWidth = (int)Math.Ceiling(cx * dpi.DpiScaleX);
                    int scaledHeight = (int)Math.Ceiling(cy * dpi.DpiScaleY);

                    System.Diagnostics.Debug.WriteLine(
                        $"文件: {Path.GetFileName(filePath)} " +
                        $"直接获取图标，原始尺寸: {cx}x{cy} " +
                        $"缩放后: {scaledWidth}x{scaledHeight}");

                    var imageSource = Imaging.CreateBitmapSourceFromHIcon(
                        sfi.hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(scaledWidth, scaledHeight)
                    );
                    imageSource.Freeze();
                    DestroyIcon(sfi.hIcon);
                    return imageSource;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"直接获取图标失败: {ex.Message}");
                    return null;
                }
            });
        }

        // 方法3: 返回默认图标（最终Fallback）
        private static async Task<ImageSource?> GetDefaultIconAsync(Dispatcher dispatcher)
        {
            return await dispatcher.InvokeAsync(() =>
            {
                try
                {
                    // 创建一个简单的默认图标（可替换为资源文件中的图标）
                    var geometry = new GeometryDrawing(
                        Brushes.Gray,
                        new Pen(Brushes.White, 2),
                        new RectangleGeometry(new Rect(0, 0, 16, 16))
                    );
                    var drawingImage = new DrawingImage(geometry);
                    drawingImage.Freeze();
                    return drawingImage;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"创建默认图标失败: {ex.Message}");
                    return null;
                }
            });
        }

        // 辅助方法：尺寸描述
        private static string GetSizeDescription(int size)
        {
            return size switch
            {
                SHIL_JUMBO => "256x256",
                SHIL_128X128 => "128x128",
                SHIL_EXTRALARGE => "48x48",
                SHIL_LARGE => "32x32",
                SHIL_MEDIUM => "16x16",
                _ => "未知尺寸"
            };
        }

    }
}