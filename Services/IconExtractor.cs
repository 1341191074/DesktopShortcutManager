using System;
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

        private const uint SHGFI_SYSICONINDEX = 0x4000;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x0010;
        private const int SHIL_JUMBO = 0x4; // 256x256
        private const int SHIL_EXTRALARGE = 0x2; // 48x48
        private const int ILD_TRANSPARENT = 0x1;
        #endregion

        public static async Task<ImageSource?> GetIconAsync(string filePath, Dispatcher dispatcher)
        {
            try
            {
                // --- 步骤 1: 在任何线程上安全地获取图标索引 ---
                var sfi = new SHFILEINFO();
                SHGetFileInfo(filePath, 0, ref sfi, (uint)Marshal.SizeOf(sfi), SHGFI_SYSICONINDEX | SHGFI_USEFILEATTRIBUTES);

                if (sfi.iIcon == 0) return null;

                // --- 步骤 2: 切换到UI线程 (STA) 来处理COM对象 ---
                return await dispatcher.InvokeAsync(() =>
                {
                    IImageList? imageList = null;
                    try
                    {
                        Guid iidImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");

                        // 尝试获取最高质量的图标列表
                        SHGetImageList(SHIL_JUMBO, ref iidImageList, out object? jumboResult);
                        imageList = jumboResult as IImageList;

                        // 如果失败，降级到次高质量
                        if (imageList == null)
                        {
                            SHGetImageList(SHIL_EXTRALARGE, ref iidImageList, out object? extraLargeResult);
                            imageList = extraLargeResult as IImageList;
                        }

                        if (imageList == null) return null;

                        IntPtr hIcon = IntPtr.Zero;
                        imageList.GetIcon(sfi.iIcon, ILD_TRANSPARENT, ref hIcon);

                        if (hIcon != IntPtr.Zero)
                        {
                            try
                            {
                                var imageSource = Imaging.CreateBitmapSourceFromHIcon(
                                    hIcon,
                                    Int32Rect.Empty,
                                    BitmapSizeOptions.FromEmptyOptions());
                                imageSource.Freeze();
                                return imageSource;
                            }
                            finally
                            {
                                DestroyIcon(hIcon);
                            }
                        }
                        return null;
                    }
                    finally
                    {
                        // 确保COM对象被释放
                        if (imageList != null)
                        {
                            Marshal.ReleaseComObject(imageList);
                        }
                    }
                });
            }
            catch
            {
                return null;
            }
        }
    }
}