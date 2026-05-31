using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ExModem.Models;

namespace ExModem.Models
{
    public partial class Conversation : ObservableObject
    {
        public string Peer { get; }
        public ObservableCollection<SmsMessage> Messages { get; } = new();

        [ObservableProperty] private string _lastText = "";
        [ObservableProperty] private string _lastTime = "";
        [ObservableProperty] private int _unread;   // 未读(收到且未读)条数
        [ObservableProperty] private string _contactName = "";   // 命中联系人时的名字
        [ObservableProperty] private string _photoPath = "";     // 命中联系人时的头像

        public bool HasUnread => Unread > 0;
        partial void OnUnreadChanged(int value) => OnPropertyChanged(nameof(HasUnread));

        // 命中联系人则显示名字,否则显示号码;陌生号码可「存为联系人」
        public string DisplayName => string.IsNullOrEmpty(ContactName) ? PeerDisplay : ContactName;
        public bool IsUnknownContact => string.IsNullOrEmpty(ContactName);

        partial void OnContactNameChanged(string value)
        {
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(IsUnknownContact));
            OnPropertyChanged(nameof(Initial));
        }

        public DateTime SortTime { get; set; } = DateTime.MinValue;

        public string Initial
        {
            get
            {
                var s = string.IsNullOrEmpty(ContactName) ? (Peer ?? "").TrimStart('+') : ContactName;
                return s.Length > 0 ? s.Substring(0, 1) : "?";
            }
        }

        // 显示用:+86 与号码分开,如 "+86 13912345678";服务号(无 +86)原样
        public string PeerDisplay
            => (Peer != null && Peer.StartsWith("+86") && Peer.Length > 3) ? "+86 " + Peer.Substring(3) : Peer ?? "";

        public Conversation(string peer)
        {
            Peer = peer;
        }

        public void Touch(string text)
        {
            LastText = text;
            var now = DateTime.Now;
            LastTime = now.ToString("HH:mm");
            SortTime = now;
        }
    }
}
