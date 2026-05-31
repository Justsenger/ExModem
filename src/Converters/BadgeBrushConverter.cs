using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ExModem.Converters
{
    // value = bool(是否点亮), parameter = "bg" 背景 / "fg" 文字。
    // 点亮:蓝底白字;熄灭:暗底灰字。与信号格同一套配色。
    public sealed class BadgeBrushConverter : IValueConverter
    {
        private static readonly Brush OnBg = new SolidColorBrush(Color.FromRgb(0x4C, 0xA0, 0xFF));
        private static readonly Brush OffBg = Brushes.Transparent;   // 未选:透明,露出凹槽底色
        private static readonly Brush OnFg = Brushes.White;
        private static readonly Brush OffFg = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool on = value is bool b && b;
            bool fg = parameter as string == "fg";
            return on ? (fg ? OnFg : OnBg) : (fg ? OffFg : OffBg);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
