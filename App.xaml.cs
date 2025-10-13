using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace DesktopShortcutManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 检查是否已在运行（避免多实例）
            var currentProcess = Process.GetCurrentProcess();
            if (Process.GetProcessesByName(currentProcess.ProcessName).Length > 1)
            {
                // 激活已运行的实例
                var existingProcess = Process.GetProcessesByName(currentProcess.ProcessName)
                    .First(p => p.Id != currentProcess.Id);
                IntPtr mainWindowHandle = existingProcess.MainWindowHandle;
                if (mainWindowHandle != IntPtr.Zero)
                {
                    ShowWindow(mainWindowHandle, SW_RESTORE);
                    SetForegroundWindow(mainWindowHandle);
                }
                currentProcess.Kill();
                return;
            }

            // 加载时显示启动动画
            //var splashScreen = new SplashScreen("Resources/splash.png");
            //splashScreen.Show(true);
        }

        // 引入Win32 API用于窗口激活
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;
    }

}
