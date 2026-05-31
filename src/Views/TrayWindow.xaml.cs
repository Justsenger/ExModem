using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ExModem.Views
{
    // 隐形常驻窗口:承载 WPF-UI 托盘图标(主题右键菜单)。通知走真·Windows Toast(Tools.Toaster)。
    public partial class TrayWindow : Window
    {
        public TrayWindow()
        {
            InitializeComponent();
        }

        private void Tray_DoubleClick(object sender, RoutedEventArgs e) => ExModem.App.ShowMainWindow();
        private void Show_Click(object sender, RoutedEventArgs e) => ExModem.App.ShowMainWindow();
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            ExModem.App.ExitRequested = true;
            System.Windows.Application.Current.Shutdown();
        }

        // 不进 Alt-Tab(隐形宿主窗口不该作为独立应用出现)
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, ex | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}
