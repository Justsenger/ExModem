using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Toolkit.Uwp.Notifications;
using ExModem.Properties;

namespace ExModem.Tools
{
    // 统一的提示入口:
    //   Tip(msg)            -> 应用内轻量气泡(屏幕中下方,1.2s 自动消失,如"已复制")
    //   Notify(title, body) -> 真·Windows 系统通知(进通知中心、有声音;短信含验证码时附"复制验证码"按钮)
    public static class Toast
    {
        // ---- 应用内气泡 ----
        public static void Tip(string message)
        {
            // 不能用 Application.MainWindow(可能是隐形托盘窗);取真正可见的主窗口,不可见(在托盘)时不弹
            Window? win = null;
            var app = Application.Current;
            if (app != null)
                foreach (Window w in app.Windows)
                    if (w is ExModem.MainWindow && w.IsVisible) { win = w; break; }
            if (win == null) return;

            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(235, 38, 38, 38)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18, 10, 18, 10),
                Child = new TextBlock { Text = message, Foreground = Brushes.White, FontSize = 13 }
            };
            var popup = new Popup
            {
                Child = border,
                AllowsTransparency = true,
                StaysOpen = true,
                Placement = PlacementMode.Center,
                PlacementTarget = win,
                VerticalOffset = Math.Max(80, win.ActualHeight / 3),
                IsOpen = true
            };
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
            timer.Tick += (s, e) => { popup.IsOpen = false; timer.Stop(); };
            timer.Start();
        }

        // ---- 系统通知 ----
        public static void Notify(string title, string body)
        {
            try
            {
                var b = new ToastContentBuilder()
                    .AddText(string.IsNullOrEmpty(title) ? Resources.Toast_NewSms : title)
                    .AddText(body ?? "");

                string? code = ExtractOtp(body);
                if (code != null)
                {
                    b.AddButton(new ToastButton()
                        .SetContent(string.Format(Resources.Toast_CopyCode, code))
                        .AddArgument("action", "copyotp")
                        .AddArgument("code", code)
                        .SetBackgroundActivation());
                }
                b.Show();
            }
            catch { }
        }

        // 含"验证码/code"等关键词且有 4-8 位数字 → 取该数字为验证码
        public static string? ExtractOtp(string? body)
        {
            if (string.IsNullOrEmpty(body)) return null;
            bool looksOtp = Regex.IsMatch(body, "验证码|校验码|动态密码|verification|code|OTP|PIN", RegexOptions.IgnoreCase);
            if (!looksOtp) return null;
            var m = Regex.Match(body, @"(?<!\d)(\d{4,8})(?!\d)");
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
