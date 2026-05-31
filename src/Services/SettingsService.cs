using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Xml.Linq;
using ExModem.Properties;
using Wpf.Ui.Appearance;

namespace ExModem.Services
{
    // 全应用唯一的 config.xml 读写口(语言 / 主题 / 自动切换)。
    // 语言改动需重启(UI culture 在进程启动时固定,见 App.OnStartup)。
    public static class SettingsService
    {
        private const string ConfigFilePath = "config.xml";
        private const string DefaultLanguage = "en-US";

        // ---- 通用 config 读写 ----
        public static string? GetConfig(string name)
        {
            if (!File.Exists(ConfigFilePath)) return null;
            try { return XDocument.Load(ConfigFilePath).Root?.Element(name)?.Value; }
            catch { return null; }
        }

        public static void SetConfig(string name, string value)
        {
            XDocument doc;
            if (File.Exists(ConfigFilePath))
            {
                doc = XDocument.Load(ConfigFilePath);
                var el = doc.Root?.Element(name);
                if (el != null) el.Value = value; else doc.Root?.Add(new XElement(name, value));
            }
            else doc = new XDocument(new XElement("Config", new XElement(name, value)));
            doc.Save(ConfigFilePath);
        }

        // ---- 自动切换网络 ----
        public static bool GetAutoSwitch() => GetConfig("AutoSwitch") == "true";
        public static void SetAutoSwitch(bool on) => SetConfig("AutoSwitch", on ? "true" : "false");

        // ---- 语言 ----
        // 下拉里始终显示母语名(不随当前 UI 语言变);Code = .NET 文化名(en-US 走中性 Resources.resx)。
        public static readonly (string Code, string Native)[] Languages =
        {
            ("zh-CN",   "简体中文"),
            ("zh-Hant", "繁體中文"),
            ("en-US",   "English"),
            ("ru",      "Русский"),
            ("ja",      "日本語"),
        };

        // 首次运行(config 里没有 Language)按系统显示语言选最接近的受支持语言,否则英文。
        public static string GetLanguage() => GetConfig("Language") ?? DetectSystemLanguage();
        public static void SaveLanguageCode(string code) => SetConfig("Language", code);

        public static string DetectSystemLanguage()
        {
            // 逐级回退:具体文化 → 中性文化(如 ja-JP → ja, zh-Hant-TW → zh-Hant → zh)
            for (var c = System.Globalization.CultureInfo.CurrentUICulture;
                 c != null && !string.IsNullOrEmpty(c.Name); c = c.Parent)
            {
                string n = c.Name;
                if (n.Equals("ja", System.StringComparison.OrdinalIgnoreCase)) return "ja";
                if (n.Equals("ru", System.StringComparison.OrdinalIgnoreCase)) return "ru";
                if (n.Equals("en", System.StringComparison.OrdinalIgnoreCase)) return "en-US";
                if (n.Equals("zh-Hant", System.StringComparison.OrdinalIgnoreCase)) return "zh-Hant";
                if (n.Equals("zh-Hans", System.StringComparison.OrdinalIgnoreCase)) return "zh-CN";
                if (n.StartsWith("zh", System.StringComparison.OrdinalIgnoreCase))
                    return (n.IndexOf("Hant", System.StringComparison.OrdinalIgnoreCase) >= 0
                            || n is "zh-TW" or "zh-HK" or "zh-MO") ? "zh-Hant" : "zh-CN";
            }
            return DefaultLanguage; // en-US
        }

        public static string LangNativeName(string code)
        {
            foreach (var l in Languages) if (l.Code == code) return l.Native;
            return "English";
        }

        public static void SetLanguageAndRestart(string nativeName)
        {
            string code = "en-US";
            foreach (var l in Languages) if (l.Native == nativeName) { code = l.Code; break; }
            SaveLanguageCode(code);
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath != null) Process.Start(exePath);
            Application.Current.Shutdown();
        }

        // ---- 主题(持久化到 config,启动时应用)----
        public static string GetTheme()
            => ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark ? Resources.dark : Resources.light;

        public static void ApplyTheme(string themeName)
        {
            var theme = themeName == Resources.dark ? ApplicationTheme.Dark : ApplicationTheme.Light;
            ApplicationThemeManager.Apply(theme);
            SetConfig("Theme", theme == ApplicationTheme.Dark ? "Dark" : "Light");
        }

        // 启动时应用:有保存值用保存的,否则跟随系统
        public static void ApplyStartupTheme()
        {
            string? saved = GetConfig("Theme");
            ApplicationTheme theme;
            if (saved == "Dark") theme = ApplicationTheme.Dark;
            else if (saved == "Light") theme = ApplicationTheme.Light;
            else theme = SystemThemeManager.GetCachedSystemTheme() == SystemTheme.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light;
            ApplicationThemeManager.Apply(theme);
        }
    }
}
