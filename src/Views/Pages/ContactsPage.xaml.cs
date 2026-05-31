using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ExModem.Services;

namespace ExModem.Views.Pages
{
    public partial class ContactsPage
    {
        private readonly ViewModels.ContactsViewModel _vm = new();

        // 短信"存为联系人"跳转时带过来的号码;Loaded 时打开新建表单
        public static string? PendingNumber;

        public ContactsPage()
        {
            InitializeComponent();
            DataContext = _vm;
            Loaded += (_, _) =>
            {
                // NavigationView 外层 ScrollViewer 会让整页撑开,内部列表不滚;把根高度钉到视口
                var sv = FindAncestor<ScrollViewer>(this);
                if (sv != null)
                    RootGrid.SetBinding(FrameworkElement.MaxHeightProperty,
                        new Binding(nameof(ScrollViewer.ViewportHeight)) { Source = sv });

                if (!string.IsNullOrEmpty(PendingNumber))
                {
                    _vm.NewWithNumber(PendingNumber);
                    PendingNumber = null;
                }
            };
        }

        private static T? FindAncestor<T>(DependencyObject d) where T : DependencyObject
        {
            DependencyObject? p = VisualTreeHelper.GetParent(d);
            while (p != null && p is not T) p = VisualTreeHelper.GetParent(p);
            return p as T;
        }

        // 触摸:手指稍滑时 PanningMode 会把点按当平移吞掉选中(只选中不打开)。听系统级 Tap 手势,
        // tap 命中即选中该联系人 -> 立刻打开详情(与短信会话列表同一修法)。
        private void ContactList_StylusSystemGesture(object sender, StylusSystemGestureEventArgs e)
        {
            if (e.SystemGesture != SystemGesture.Tap) return;
            if (e.OriginalSource is DependencyObject d && FindAncestor<ListBoxItem>(d) is ListBoxItem item)
                item.IsSelected = true;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (((sender as MenuItem)?.Parent as ContextMenu)?.PlacementTarget is FrameworkElement fe
                && fe.DataContext is ContactItem c)
                _vm.DeleteCommand.Execute(c);
        }

        // 输入框按回车 = 保存,并取消光标(退出编辑态)
        private void Field_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            if (_vm.SaveCommand.CanExecute(null)) _vm.SaveCommand.Execute(null);
            if (sender is TextBox tb)
            {
                var scope = FocusManager.GetFocusScope(tb);
                FocusManager.SetFocusedElement(scope, null);
                Keyboard.ClearFocus();
            }
        }
    }
}
