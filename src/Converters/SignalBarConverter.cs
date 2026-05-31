using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ExModem.Converters
{
    // value = 信号等级(0-4), parameter = 第几格("1".."4")。该格亮则返回蓝色,否则暗色。
    public sealed class SignalBarConverter : IValueConverter
    {
        private static readonly Brush On = new SolidColorBrush(Color.FromRgb(0x4C, 0xA0, 0xFF));
        private static readonly Brush Off = new SolidColorBrush(Color.FromArgb(0x40, 0x90, 0x90, 0x90));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int level = value is int i ? i : 0;
            int idx = parameter is string s && int.TryParse(s, out var n) ? n : 0;
            return level >= idx ? On : Off;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
