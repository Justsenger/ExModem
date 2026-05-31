using System.Globalization;
using System.Windows;
using ExModem.Properties;

namespace ExModem;

public partial class App
{
    // true 时 MainWindow 关闭才真正退出;否则关窗=隐藏到托盘
    public static bool ExitRequested;

    private ExModem.Services.SmsNotifier? _smsNotifier;
    private ExModem.Views.TrayWindow? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ★ 语言必须最先设好:下面的托盘窗口、通知等会用 Resources(x:Static 在控件加载时取值定死),
        //   晚设会导致托盘菜单等显示启动那刻的系统默认语言,而不是用户所选语言。
        var configLang = ExModem.Services.SettingsService.GetConfig("Language");
        SetLanguage(IsLanguageSupported(configLang)
            ? configLang!
            : ExModem.Services.SettingsService.DetectSystemLanguage());

        // 关主窗口不退出(关窗=释放窗口内存,进程靠托盘保活收通知);真正退出走托盘「退出」=Shutdown()
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // 常驻隐形托盘窗口(WPF-UI 托盘图标 + 主题菜单)
        _tray = new ExModem.Views.TrayWindow();
        _tray.Show();

        // 全局新短信桌面通知(独立于短信页,后台也能弹;真·Windows Toast,进通知中心+有声音)
        _smsNotifier = new ExModem.Services.SmsNotifier((t, b) => ExModem.Tools.Toast.Notify(t, b));
        _ = _smsNotifier.StartAsync();

        // 点击 Toast:有"复制验证码"按钮则复制到剪贴板;否则唤起主窗口
        Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.OnActivated += e =>
        {
            var args = Microsoft.Toolkit.Uwp.Notifications.ToastArguments.Parse(e.Argument);
            if (args.TryGetValue("action", out var act) && act == "copyotp" && args.TryGetValue("code", out var code))
            {
                Dispatcher.Invoke(() => { try { System.Windows.Clipboard.SetText(code); } catch { } });
            }
            else
            {
                Dispatcher.Invoke(ShowMainWindow);
            }
        };

        // 空闲(主窗口隐藏在托盘)时定期把工作集压回系统,避免后台虚占几百 M
        var trim = new System.Windows.Threading.DispatcherTimer { Interval = System.TimeSpan.FromSeconds(30) };
        trim.Tick += (s, ev) =>
        {
            var mw = MainWindow;
            if (mw == null || !mw.IsVisible) ExModem.Tools.MemoryTools.Trim();
        };
        trim.Start();
        // 启动加载完成后压一次(初始加载会把工作集顶高)
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle,
            new System.Action(() => ExModem.Tools.MemoryTools.Trim()));

        // 全局异常兜底:出错弹窗而不是硬崩
        DispatcherUnhandledException += (s, ex) =>
        {
            MessageBox.Show(ex.Exception.ToString(), ExModem.Properties.Resources.App_ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };

    }

    // 唤回隐藏的单实例主窗口(托盘双击/菜单/Toast 点击共用)
    public static void ShowMainWindow()
    {
        var app = Current;
        if (app == null) return;
        MainWindow? mw = null;
        foreach (System.Windows.Window w in app.Windows) if (w is MainWindow m) { mw = m; break; }
        if (mw == null) { mw = new MainWindow(); app.MainWindow = mw; }
        if (mw.WindowState == System.Windows.WindowState.Minimized) mw.WindowState = System.Windows.WindowState.Normal;
        mw.Show();
        mw.Activate();
        mw.Topmost = true;
        mw.Topmost = false;
    }

    // 受支持 = SettingsService.Languages 里列出的 5 种之一
    private static bool IsLanguageSupported(string? code)
    {
        if (string.IsNullOrEmpty(code)) return false;
        foreach (var l in ExModem.Services.SettingsService.Languages) if (l.Code == code) return true;
        return false;
    }

    private void SetLanguage(string cultureCode)
    {
        var culture = new CultureInfo(cultureCode);
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
