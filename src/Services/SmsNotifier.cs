using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExModem.Properties;

namespace ExModem.Services
{
    // 全局新短信桌面通知:监听系统短信库,出现"未见过的来信 id"就回调弹气泡(由 TrayWindow 经 Shell_NotifyIcon 实现)。
    // 独立于短信页运行(App 启动即起),所以后台/最小化也能收到通知。
    public sealed class SmsNotifier
    {
        private readonly Action<string, string> _notify;   // (title, body)
        private readonly SystemSmsService _sms = new();
        private readonly HashSet<string> _seen = new();
        private bool _primed;

        public SmsNotifier(Action<string, string> notify) => _notify = notify;

        public async Task StartAsync()
        {
            // 基线:把现有来信 id 全记下,避免为历史短信弹通知
            try
            {
                foreach (var m in await _sms.LoadRecentAsync())
                    if (m.Incoming && !string.IsNullOrEmpty(m.LocalId)) _seen.Add(m.LocalId);
            }
            catch { }
            _primed = true;

            await _sms.StartWatchAsync(() => { _ = OnChangedAsync(); });
        }

        private async Task OnChangedAsync()
        {
            if (!_primed) return;
            try
            {
                foreach (var m in await _sms.LoadRecentAsync())
                {
                    if (!m.Incoming || string.IsNullOrEmpty(m.LocalId) || _seen.Contains(m.LocalId)) continue;
                    _seen.Add(m.LocalId);
                    _notify(string.IsNullOrEmpty(m.Peer) ? Resources.Toast_NewSms : m.Peer, m.Body);
                }
            }
            catch { }
        }
    }
}
