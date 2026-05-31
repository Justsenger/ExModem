using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ExModem.Models;
using ExModem.ViewModels;
using ExModem.Properties;

namespace ExModem.Views.Pages
{
    public partial class ChatPage
    {
        private readonly ChatViewModel _vm = new();
        private Conversation? _watched;

        public ChatPage()
        {
            InitializeComponent();
            DataContext = _vm;
            _vm.PropertyChanged += OnVmChanged;
            Loaded += OnLoaded;
        }

        // NavigationView wraps page content in an outer ScrollViewer, which makes the
        // whole page scroll as one. Pin the root to the viewport height so the page fits
        // exactly -> no outer scroll -> the left list and right thread scroll independently.
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var sv = FindAncestor<ScrollViewer>(this);
            if (sv != null)
            {
                RootGrid.SetBinding(FrameworkElement.MaxHeightProperty,
                    new Binding(nameof(ScrollViewer.ViewportHeight)) { Source = sv });
            }
        }

        private static T? FindAncestor<T>(DependencyObject d) where T : DependencyObject
        {
            DependencyObject? p = VisualTreeHelper.GetParent(d);
            while (p != null && p is not T) p = VisualTreeHelper.GetParent(p);
            return p as T;
        }

        private void OnVmChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ChatViewModel.SelectedConversation)) return;

            if (_watched != null)
                _watched.Messages.CollectionChanged -= OnMessagesChanged;

            _watched = _vm.SelectedConversation;
            if (_watched != null)
            {
                _watched.Messages.CollectionChanged += OnMessagesChanged;
                MsgScroll.ScrollToEnd();
            }
        }

        private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => MsgScroll.ScrollToEnd();

        // 触摸场景:手指稍有滑动时 PanningMode 会把点按当成平移而吞掉选中,
        // 表现为"按一次只选中不切换、要按下抬起干净一点才切"。改听系统级 Tap 手势
        // (容差更宽、且仍与拖动区分):识别为 tap 即选中命中的会话 → 立刻切换。
        private void ConvList_StylusSystemGesture(object sender, StylusSystemGestureEventArgs e)
        {
            if (e.SystemGesture != SystemGesture.Tap) return;
            if (e.OriginalSource is DependencyObject d && FindAncestor<ListBoxItem>(d) is ListBoxItem item)
                item.IsSelected = true;
        }

        private static FrameworkElement? MenuTarget(object sender)
            => ((sender as MenuItem)?.Parent as ContextMenu)?.PlacementTarget as FrameworkElement;

        private void CopyMessage_Click(object sender, RoutedEventArgs e)
        {
            var target = MenuTarget(sender);
            string text = (target?.DataContext as SmsMessage)?.Body ?? "";
            try { Clipboard.SetText(text); } catch { }
        }

        private async void DeleteMessage_Click(object sender, RoutedEventArgs e)
        {
            if (MenuTarget(sender)?.DataContext is SmsMessage msg)
                await _vm.DeleteMessageAsync(msg);
        }

        // 会话列表右键:删除整段会话
        private void DeleteConversation_Click(object sender, RoutedEventArgs e)
        {
            if (MenuTarget(sender)?.DataContext is Conversation c)
                _vm.DeleteConversationCommand.Execute(c);
        }

        // 点击顶部号码:复制 + 弹"已复制"小框(纯文本,不是链接)
        private void HeaderNumber_Click(object sender, MouseButtonEventArgs e)
        {
            string num = _vm.CurrentPeer;
            if (string.IsNullOrEmpty(num)) return;
            try { Clipboard.SetText(num); } catch { }
            ExModem.Tools.Toast.Tip(string.Format(ExModem.Properties.Resources.Toast_Copied, num));
        }
    }
}
