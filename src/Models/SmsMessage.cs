using System;
using ExModem.Properties;

namespace ExModem.Models
{
    public class SmsMessage
    {
        public string Body { get; set; } = "";
        public bool IsIncoming { get; set; }
        public string Peer { get; set; } = "";
        public string LocalId { get; set; } = "";   // ChatMessageStore message id (for delete)
        public DateTime Time { get; set; } = DateTime.Now;
        public string TimeText => Time.ToString("yyyy/MM/dd HH:mm");

        public bool IsRead { get; set; } = true;          // 收到的是否已读
        public string Status { get; set; } = "";          // 发出的:Sent/SendFailed/Sending/...(ChatMessageStatus)

        // SIM 卡上的短信(混入会话时标记):删除走 QMI WMS 而非系统库
        public bool IsSim { get; set; }
        public byte SimStorage { get; set; }
        public uint SimIndex { get; set; }

        // 发送状态(仅发出的、且发送中或失败时显示;无重发)
        public bool IsFailed => !IsIncoming && Status == "SendFailed";
        public bool IsSending => !IsIncoming && Status == "Sending";
        public bool ShowStatus => IsFailed || IsSending;
        public string StatusText => IsFailed ? Resources.Msg_StatusFailed : IsSending ? Resources.Msg_StatusSending : "";

        public string Initial
        {
            get
            {
                var p = (Peer ?? "").TrimStart('+');
                return p.Length > 0 ? p.Substring(0, 1) : "?";
            }
        }
    }
}
