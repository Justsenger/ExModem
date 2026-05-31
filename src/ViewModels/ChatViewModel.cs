using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExModem.Core.Backends;
using ExModem.Models;
using ExModem.Services;
using ExModem.Properties;

namespace ExModem.ViewModels
{
    public partial class ChatViewModel : ObservableObject
    {
        private readonly SystemSmsService _sysSms = new();
        private readonly ContactsService _contacts = ContactsService.Shared;
        private bool _syncing;
        private List<SimSmsRecord> _simCache = new();   // SIM 短信缓存:只在启动/删 SIM 后重读,不挂每次同步
        private bool _simCapLoaded;

        public ObservableCollection<Conversation> Conversations { get; } = new();

        [ObservableProperty] private Conversation? _selectedConversation;
        [ObservableProperty] private string _currentPeer = "";
        [ObservableProperty] private string _draft = "";
        [ObservableProperty] private string _status = "";
        [ObservableProperty] private bool _isComposingNew;   // 仅新建时号码可编辑
        [ObservableProperty] private string _searchText = "";   // 全文搜索
        [ObservableProperty] private int _totalUnread;          // 总未读
        [ObservableProperty] private int _simUsed;              // SIM 卡已用短信数
        [ObservableProperty] private int _simCapacity;          // SIM 卡短信容量

        public string SimCapacityText => $"SIM : {SimUsed} / {(SimCapacity > 0 ? SimCapacity : 50)}";
        partial void OnSimUsedChanged(int v) => OnPropertyChanged(nameof(SimCapacityText));
        partial void OnSimCapacityChanged(int v) => OnPropertyChanged(nameof(SimCapacityText));

        private readonly List<Conversation> _all = new();       // 过滤前的全量会话
        partial void OnSearchTextChanged(string value) => ApplyFilter();

        // 显示用:把国家码和号码分开,如 "+86 13912345678"
        public string CurrentPeerDisplay => FormatPeer(CurrentPeer);

        // 会话标题:命中联系人显示名字,否则显示号码
        public string CurrentTitle =>
            (SelectedConversation != null && !SelectedConversation.IsUnknownContact)
                ? SelectedConversation.ContactName : CurrentPeerDisplay;

        // 仅当前会话是陌生号码时,可「存为联系人」
        public bool CanSaveContact =>
            !IsComposingNew && SelectedConversation != null && SelectedConversation.IsUnknownContact;

        partial void OnCurrentPeerChanged(string value)
        {
            OnPropertyChanged(nameof(CurrentPeerDisplay));
            OnPropertyChanged(nameof(CurrentTitle));
        }

        partial void OnIsComposingNewChanged(bool value) => OnPropertyChanged(nameof(CanSaveContact));

        private static string FormatPeer(string p)
        {
            if (string.IsNullOrWhiteSpace(p)) return "";
            p = p.Trim();
            return (p.StartsWith("+86") && p.Length > 3) ? "+86 " + p.Substring(3) : p;
        }

        public ChatViewModel()
        {
            _ = InitAsync();
        }

        private async Task InitAsync()
        {
            await SyncAsync();                  // 先渲染系统短信(快)
            _ = RefreshSimCacheAsync();         // SIM 短信(慢)后台读一次,读完并入会话
            try { await _sysSms.StartWatchAsync(() => Dispatch(() => { _ = SyncAsync(); })); }
            catch { }
            // 联系人改名/换头像后,刷新会话显示的名字与头像(只读系统联系人,不重读短信/SIM)
            ContactsService.Changed += () => Dispatch(() => { _ = RefreshContactNamesAsync(); });
        }

        // 重读 SIM 短信缓存(慢,仅启动/删 SIM 后调),读完重建会话
        private async Task RefreshSimCacheAsync()
        {
            try { _simCache = await Task.Run(() => Modem.Current.ReadSimSms()); }
            catch { _simCache = new List<SimSmsRecord>(); }
            if (!_simCapLoaded)
            {
                try { SimCapacity = await Task.Run(() => Modem.Current.GetSmsCapacity()); _simCapLoaded = true; }
                catch { }
            }
            await SyncAsync();
        }

        private async Task RefreshContactNamesAsync()
        {
            List<ContactItem> contacts;
            try { contacts = await _contacts.GetAllAsync(); }
            catch { return; }
            foreach (var c in _all)
            {
                var hit = contacts.FirstOrDefault(x => ContactsService.SameNumber(x.Number, c.Peer));
                c.ContactName = hit?.Name ?? "";
                string photo = hit?.PhotoPath ?? "";
                // 头像文件路径按联系人 Id 固定,换头像是覆盖同一文件、路径不变 -> 赋同值不会触发刷新。
                // 先清空再赋值,强制重新绑定,让 PathToImage 转换器按文件新的修改时间重新解码出新头像。
                if (c.PhotoPath == photo) c.PhotoPath = "";
                c.PhotoPath = photo;
            }
            OnPropertyChanged(nameof(CurrentTitle));
            OnPropertyChanged(nameof(CanSaveContact));
        }

        private static void Dispatch(Action a)
        {
            var d = Application.Current?.Dispatcher;
            if (d == null || d.CheckAccess()) a();
            else d.Invoke(a);
        }

        partial void OnSelectedConversationChanged(Conversation? value)
        {
            if (value != null)
            {
                CurrentPeer = value.Peer;
                IsComposingNew = false;
                _ = MarkReadAsync(value);   // 进会话自动标已读
            }
            OnPropertyChanged(nameof(CurrentTitle));
            OnPropertyChanged(nameof(CanSaveContact));
        }

        // 把当前陌生号码存为联系人:跳转到联系人页并打开新建表单
        [RelayCommand]
        private void SaveContact()
        {
            var peer = SelectedConversation?.Peer ?? CurrentPeer;
            if (string.IsNullOrWhiteSpace(peer)) return;
            Views.Pages.ContactsPage.PendingNumber = peer;
            MainWindow.NavigateTo(typeof(Views.Pages.ContactsPage));
        }

        private async Task MarkReadAsync(Conversation c)
        {
            var ids = c.Messages
                .Where(m => m.IsIncoming && !m.IsRead && !string.IsNullOrEmpty(m.LocalId))
                .Select(m => m.LocalId).ToList();
            if (ids.Count == 0) return;
            c.Unread = 0;                       // 立即更新角标
            TotalUnread = _all.Sum(x => x.Unread);
            try { await _sysSms.MarkReadAsync(ids); } catch { }
        }

        [RelayCommand]
        private async Task DeleteConversation(Conversation? c)
        {
            if (c == null) return;
            var ids = c.Messages.Where(m => !string.IsNullOrEmpty(m.LocalId)).Select(m => m.LocalId).ToList();
            try { await _sysSms.DeleteManyAsync(ids); Status = string.Format(Resources.Chat_Status_ConvoDeleted, c.Peer); }
            catch (Exception ex) { Status = string.Format(Resources.Chat_DeleteFailed, ex.Message.Split('\n')[0]); }
            if (SelectedConversation == c) SelectedConversation = null;
            await SyncAsync();
        }

        [RelayCommand]
        private void NewConversation()
        {
            SelectedConversation = null;
            CurrentPeer = "";
            Draft = "";
            IsComposingNew = true;
            Status = Resources.Chat_Status_NewConvo;
        }

        [RelayCommand]
        private async Task Send()
        {
            string number = (CurrentPeer ?? "").Trim();
            string text = Draft ?? "";
            if (number.Length == 0) { Status = Resources.Chat_Status_NeedNumber; return; }
            if (text.Length == 0) { Status = Resources.Chat_Status_NeedText; return; }

            Draft = "";
            try
            {
                SysSendResult sys = await _sysSms.SendViaSystemAsync(number, text);
                if (!sys.Success) Tools.Toast.Tip(string.Format(Resources.Chat_Toast_SendFailedLte, sys.Message));
            }
            catch (Exception ex)
            {
                Tools.Toast.Tip(string.Format(Resources.Chat_Toast_SendError, ex.Message.Split('\n')[0]));
            }

            IsComposingNew = false;
            await SyncAsync();
        }

        // 删除单条消息(SIM 短信走 QMI WMS,其余走系统短信库)
        public async Task DeleteMessageAsync(SmsMessage? m)
        {
            if (m == null) return;
            try
            {
                if (m.IsSim)
                {
                    bool ok = await Task.Run(() => Modem.Current.DeleteSms(m.SimStorage, m.SimIndex));
                    Status = ok ? Resources.Chat_Status_SimDeleted : Resources.Chat_Status_SimDeleteFailed;
                }
                else
                {
                    await _sysSms.DeleteAsync(m.LocalId);
                    Status = Resources.Chat_Status_MsgDeleted;
                }
            }
            catch (Exception ex)
            {
                Status = string.Format(Resources.Chat_DeleteFailed, ex.Message.Split('\n')[0]);
            }
            if (m.IsSim) await RefreshSimCacheAsync(); else await SyncAsync();
        }

        private async Task SyncAsync()
        {
            if (_syncing) return;
            _syncing = true;
            try
            {
                List<SysSms> items = await _sysSms.LoadRecentAsync();

                // SIM 卡短信用缓存并入(缓存由 RefreshSimCacheAsync 在启动/删 SIM 时更新,不在此重读)
                var simRecs = _simCache;
                SimUsed = simRecs.Count;

                // 按号码聚合(系统 + SIM)
                var map = new Dictionary<string, Conversation>();
                Conversation GetConvo(string peer)
                {
                    string key = string.IsNullOrWhiteSpace(peer) ? Resources.Common_Unknown : peer.Trim();
                    if (!map.TryGetValue(key, out var c)) { c = new Conversation(key); map[key] = c; }
                    return c;
                }

                foreach (var m in items)
                {
                    var c = GetConvo(m.Peer);
                    c.Messages.Add(new SmsMessage
                    {
                        Body = m.Body, IsIncoming = m.Incoming, Peer = c.Peer, Time = m.Time,
                        LocalId = m.LocalId, IsRead = m.IsRead, Status = m.Status,
                    });
                }
                foreach (var s in simRecs)
                {
                    var c = GetConvo(s.From);
                    if (!DateTime.TryParse(s.Time, out var t)) t = DateTime.Now;
                    c.Messages.Add(new SmsMessage
                    {
                        Body = s.Text, IsIncoming = true, Peer = c.Peer, Time = t, IsRead = true,
                        IsSim = true, SimStorage = s.Storage, SimIndex = s.Index,
                    });
                }

                // 每个会话内按时间排序,算末条/未读
                var convos = new List<Conversation>();
                foreach (var c in map.Values)
                {
                    var ordered = c.Messages.OrderBy(x => x.Time).ToList();
                    c.Messages.Clear();
                    foreach (var m in ordered) c.Messages.Add(m);
                    var last = ordered[ordered.Count - 1];
                    c.LastText = last.Body;
                    c.LastTime = last.Time.ToString("MM-dd HH:mm");
                    c.SortTime = last.Time;
                    c.Unread = ordered.Count(m => m.IsIncoming && !m.IsRead);
                    convos.Add(c);
                }
                convos = convos.OrderByDescending(c => c.SortTime).ToList();

                // 命中「ExModem」联系人库 -> 显示名字+头像(一次性拉全量,本地按末8位匹配)
                try
                {
                    var contacts = await _contacts.GetAllAsync();
                    foreach (var c in convos)
                    {
                        var hit = contacts.FirstOrDefault(x => ContactsService.SameNumber(x.Number, c.Peer));
                        if (hit != null) { c.ContactName = hit.Name; c.PhotoPath = hit.PhotoPath; }
                    }
                }
                catch { }

                _all.Clear();
                _all.AddRange(convos);
                ApplyFilter();

                Status = string.Format(Resources.Chat_Status_Synced, items.Count, simRecs.Count, convos.Count);
            }
            catch (Exception ex)
            {
                Status = string.Format(Resources.Chat_Status_SyncFailed, ex.Message.Split('\n')[0]);
            }
            finally
            {
                _syncing = false;
            }
        }

        // 按搜索词过滤 _all -> Conversations,并保持选中会话、更新总未读
        private void ApplyFilter()
        {
            string q = (SearchText ?? "").Trim();
            IEnumerable<Conversation> src = _all;
            if (q.Length > 0)
                src = _all.Where(c =>
                    (c.Peer?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    || c.Messages.Any(m => m.Body?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0));

            string keep = SelectedConversation?.Peer ?? CurrentPeer;
            Conversations.Clear();
            foreach (var c in src) Conversations.Add(c);

            if (!string.IsNullOrEmpty(keep))
            {
                var match = Conversations.FirstOrDefault(c => c.Peer == keep);
                if (match != null) SelectedConversation = match;
            }
            TotalUnread = _all.Sum(c => c.Unread);
        }
    }
}
