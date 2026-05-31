using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Chat;
using Windows.Devices.Enumeration;
using Windows.Devices.Sms;
using ExModem.Properties;

namespace ExModem.Services
{
    public sealed class SysSms
    {
        public string Peer = "";
        public string Body = "";
        public bool Incoming;
        public DateTime Time;
        public string LocalId = "";
        public bool IsRead = true;
        public string Status = "";
    }

    public sealed class SysSendResult
    {
        public bool Success;
        public string Via = "";
        public string Message = "";
    }

    // System-interface SMS via Windows.Devices.Sms + Windows.ApplicationModel.Chat.
    // Reading the store works UNPACKAGED (verified). This also tries SENDING via
    // SmsDevice2 (cleaner: OS sends + auto-writes to the store), and writing sent
    // messages into ChatMessageStore so the thread stays consistent.
    public sealed class SystemSmsService
    {
        private ChatMessageStore? _store;

        // Subscribe to OS store changes (new incoming SMS, sent saved, etc.) for auto-refresh.
        public async Task StartWatchAsync(Action onChanged)
        {
            _store = await ChatMessageManager.RequestStoreAsync();
            _store.MessageChanged += (s, e) => onChanged();
        }

        // ---- receive: read OS SMS store ----
        public async Task<List<SysSms>> LoadRecentAsync(int max = 500)
        {
            var list = new List<SysSms>();
            ChatMessageStore store = await ChatMessageManager.RequestStoreAsync();
            ChatMessageReader reader = store.GetMessageReader();

            while (list.Count < max)
            {
                IReadOnlyList<ChatMessage> batch = await reader.ReadBatchAsync();
                if (batch == null || batch.Count == 0) break;

                foreach (ChatMessage m in batch)
                {
                    string peer = m.IsIncoming
                        ? m.From
                        : (m.Recipients.Count > 0 ? m.Recipients[0] : m.From);

                    list.Add(new SysSms
                    {
                        Peer = peer ?? "",
                        Body = m.Body ?? "",
                        Incoming = m.IsIncoming,
                        Time = m.LocalTimestamp.LocalDateTime,
                        LocalId = m.Id ?? "",
                        IsRead = m.IsRead,
                        Status = m.Status.ToString(),
                    });
                }
            }
            return list;
        }

        // ---- send: try the system SmsDevice2 (no QMI). Throws if unavailable/denied. ----
        public async Task<SysSendResult> SendViaSystemAsync(string number, string text)
        {
            var r = new SysSendResult { Via = Resources.Sys_SendVia };

            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(SmsDevice2.GetDeviceSelector());
            if (devices == null || devices.Count == 0)
            {
                r.Message = Resources.Sys_NoSmsDevice;
                return r;
            }

            SmsDevice2 dev = SmsDevice2.FromId(devices[0].Id);
            var msg = new SmsTextMessage2 { To = number, Body = text };
            SmsSendMessageResult sr = await dev.SendMessageAndGetResultAsync(msg);

            r.Success = sr.IsSuccessful;
            r.Message = sr.IsSuccessful ? Resources.Sys_SentViaSystem : Resources.Sys_SendFailed;

            try { await SaveSentToStoreAsync(number, text, sr.IsSuccessful); } catch { }
            return r;
        }

        // ---- delete a message from the OS store by its local id ----
        public async Task DeleteAsync(string localId)
        {
            if (string.IsNullOrEmpty(localId)) return;
            ChatMessageStore store = await ChatMessageManager.RequestStoreAsync();
            await store.DeleteMessageAsync(localId);
        }

        // ---- mark a batch of messages read (进会话自动标已读) ----
        public async Task MarkReadAsync(IEnumerable<string> localIds)
        {
            ChatMessageStore store = await ChatMessageManager.RequestStoreAsync();
            foreach (var id in localIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                try { await store.MarkMessageReadAsync(id); } catch { }
            }
            try { await store.MarkAsSeenAsync(); } catch { }
        }

        // ---- delete a whole conversation by its message ids ----
        public async Task DeleteManyAsync(IEnumerable<string> localIds)
        {
            ChatMessageStore store = await ChatMessageManager.RequestStoreAsync();
            foreach (var id in localIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                try { await store.DeleteMessageAsync(id); } catch { }
            }
        }

        // ---- write a sent message into the OS store so it appears in the thread ----
        public async Task SaveSentToStoreAsync(string number, string text, bool success)
        {
            ChatMessageStore store = await ChatMessageManager.RequestStoreAsync();

            string transportId = "0";
            IReadOnlyList<ChatMessageTransport> transports = await ChatMessageManager.GetTransportsAsync();
            if (transports.Count > 0) transportId = transports[0].TransportId;

            var cm = new ChatMessage { Body = text };
            cm.Recipients.Add(number);
            DateTimeOffset now = DateTimeOffset.Now;
            cm.LocalTimestamp = now;
            cm.NetworkTimestamp = now;
            cm.IsIncoming = false;
            cm.TransportId = transportId;
            cm.MessageOperatorKind = ChatMessageOperatorKind.Sms;
            cm.Status = success ? ChatMessageStatus.Sent : ChatMessageStatus.SendFailed;

            await store.SaveMessageAsync(cm);
        }
    }
}
