using System.Windows;
using ExModem.Views.Pages;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace ExModem
{
    public partial class MainWindow : FluentWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            // 托盘窗先于本窗创建,会抢占 Application.MainWindow;这里纠正回真正的主窗口
            if (Application.Current != null) Application.Current.MainWindow = this;
            Loaded += PagePreload;

            // 应用已保存的主题(无保存值则跟随系统)
            ExModem.Services.SettingsService.ApplyStartupTheme();
        }

        private void PagePreload(object sender, RoutedEventArgs e)
        {
            RootNavigation.Navigate(typeof(ChatPage));
            RootNavigation.Navigate(typeof(StatusPage));
        }

        // 跨页导航(短信"存为联系人"跳转联系人页)
        public static void NavigateTo(System.Type page)
        {
            if (Application.Current?.MainWindow is MainWindow w)
                w.RootNavigation.Navigate(page);
        }

        // 关窗=隐藏到托盘(单实例,不销毁不重建→不泄漏);真正退出走托盘「退出」(置 App.ExitRequested)。
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!App.ExitRequested)
            {
                e.Cancel = true;
                Hide();
                ExModem.Tools.MemoryTools.Trim();   // 收到托盘时立即把工作集压回系统
                return;
            }
            base.OnClosing(e);
        }
    }
}
