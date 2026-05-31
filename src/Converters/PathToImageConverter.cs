using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ExModem.Converters
{
    // 本地图片路径 -> ImageSource(冻结、OnLoad 不锁文件)。空/不存在 -> null(露出底层首字母)。
    public sealed class PathToImageConverter : IValueConverter
    {
        // 解码后冻结的位图缓存:再次选中同一联系人(如带高清图的小悦)直接命中、零解码。
        // 键含文件修改时间,改了头像缓存键变化会自动重解,不会显示旧图。
        private static readonly ConcurrentDictionary<string, BitmapImage> _cache = new();

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            // 头像只有 42~104px,按显示尺寸降采样解码,避免每次解整张高清原图(卡顿主因)。
            // ConverterParameter 指定目标像素宽:列表小头像默认 128,详情大头像传 384。
            int decodeWidth = 128;
            if (parameter is string ps && int.TryParse(ps, out var pw) && pw > 0) decodeWidth = pw;
            else if (parameter is int pi && pi > 0) decodeWidth = pi;
            try
            {
                long stamp = File.GetLastWriteTimeUtc(path).Ticks;
                string key = path + "|" + decodeWidth + "|" + stamp;
                if (_cache.TryGetValue(key, out var cached)) return cached;

                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bi.DecodePixelWidth = decodeWidth;
                bi.UriSource = new Uri(path);
                bi.EndInit();
                bi.Freeze();
                _cache[key] = bi;
                return bi;
            }
            catch { return null; }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
