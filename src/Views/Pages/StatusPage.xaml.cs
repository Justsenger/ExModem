using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ExModem.Controls;
using ExModem.ViewModels;

namespace ExModem.Views.Pages
{
    public partial class StatusPage
    {
        private readonly StatusViewModel _vm = new();
        private bool _bannerRendered;

        public StatusPage()
        {
            InitializeComponent();
            DataContext = _vm;
            Loaded += OnLoaded;
            Unloaded += (s, e) => _vm.StopPolling();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _vm.StartPolling();
            if (!_bannerRendered)
            {
                _bannerRendered = true;
                RenderBanner();
            }
        }

        // 把矢量 banner 一次性渲染成冻结位图,GPU 只采样纹理、不再每帧重绘。
        private void RenderBanner()
        {
            var banner = new BannerView();
            banner.Measure(new Size(1200, 200));
            banner.Arrange(new Rect(0, 0, 1200, 200));
            banner.UpdateLayout();

            var rtb = new RenderTargetBitmap(1200, 200, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(banner);
            rtb.Freeze();

            BannerBorder.Background = new ImageBrush(rtb)
            {
                Stretch = Stretch.UniformToFill,
                AlignmentY = AlignmentY.Center,
            };
        }
    }
}
