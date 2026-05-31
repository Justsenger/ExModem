using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ExModem.Tools;
using ExModem.Properties;

namespace ExModem.Behaviors
{
    // 把消息正文填进 TextBlock(轻量、自动按 MaxWidth 换行,无需手工测宽),
    // 并把验证码/网址做成蓝色下划线可点链接:验证码点击=复制+弹"已复制";网址点击=默认浏览器打开。
    // 用 TextBlock(而非 RichTextBox)是为了切换/长会话不卡:RichTextBox+FlowDocument 实例化很重。
    public static class OtpText
    {
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.RegisterAttached("Source", typeof(string), typeof(OtpText),
                new PropertyMetadata(null, OnSourceChanged));

        public static void SetSource(DependencyObject o, string v) => o.SetValue(SourceProperty, v);
        public static string GetSource(DependencyObject o) => (string)o.GetValue(SourceProperty);

        private static readonly Regex Hint = new("验证码|校验码|动态码|verification|code|OTP", RegexOptions.IgnoreCase);
        private static readonly Regex CodeRx = new(@"(?<!\d)\d{4,8}(?!\d)");
        private static readonly Regex UrlRx = new(
            @"https?://\S+|[a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)*\.(cn|com|net|org|cc|io|co|gov|edu|me|tv|info|xyz|top|vip)(/\S*)?",
            RegexOptions.IgnoreCase);
        private static readonly Brush LinkBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xA0, 0xFF));

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBlock tb) return;
            string text = e.NewValue as string ?? "";

            tb.Inlines.Clear();

            var spans = new List<(int start, int end, string val, bool isUrl)>();
            foreach (Match u in UrlRx.Matches(text))
                spans.Add((u.Index, u.Index + u.Length, u.Value, true));
            if (Hint.IsMatch(text))
            {
                Match c = CodeRx.Match(text);
                if (c.Success && !InAnyUrl(spans, c.Index))
                    spans.Add((c.Index, c.Index + c.Length, c.Value, false));
            }
            spans.Sort((a, b) => a.start.CompareTo(b.start));

            int pos = 0;
            foreach (var sp in spans)
            {
                if (sp.start < pos) continue;
                if (sp.start > pos) tb.Inlines.Add(new Run(text.Substring(pos, sp.start - pos)));
                tb.Inlines.Add(MakeLink(sp.val, sp.isUrl));
                pos = sp.end;
            }
            if (pos < text.Length) tb.Inlines.Add(new Run(text.Substring(pos)));
        }

        private static bool InAnyUrl(List<(int start, int end, string val, bool isUrl)> spans, int idx)
        {
            foreach (var s in spans) if (s.isUrl && idx >= s.start && idx < s.end) return true;
            return false;
        }

        private static Hyperlink MakeLink(string value, bool isUrl)
        {
            var link = new Hyperlink(new Run(value))
            {
                Foreground = LinkBrush,
                Cursor = Cursors.Hand,
                TextDecorations = TextDecorations.Underline,
            };
            if (isUrl)
            {
                link.Click += (s, e) =>
                {
                    string nav = value.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? value : "http://" + value;
                    try { Process.Start(new ProcessStartInfo(nav) { UseShellExecute = true }); } catch { }
                };
            }
            else
            {
                link.Click += (s, e) =>
                {
                    try { Clipboard.SetText(value); } catch { }
                    Toast.Tip(string.Format(Resources.Toast_CodeCopied, value));
                };
            }
            return link;
        }
    }
}
