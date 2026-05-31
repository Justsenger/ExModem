using System;
using System.Collections.Generic;
using System.Text;
using ExModem.Core;
using ExModem.Properties;

namespace ExModem.Services
{
    // 物理存储在 SIM 卡(storage=0)/调制解调器(storage=1)上的短信。经 QMI WMS 直通读写。
    // RAW_READ 同步返回 PDU(无需异步指示),所以在直通通道下可用。
    //  - LIST  0x0031: 0x01 storage + 0x11 message_mode(=1 GW)
    //  - READ  0x0022: 0x01 {storage, index(u32)} + 0x10 message_mode(=1)
    //  - DELETE0x0024: 0x01 storage + [0x10 index(u32)] + 0x12 message_mode(=1)   ← mode 是 0x12!
    public sealed class SimSmsRecord
    {
        public byte Storage { get; set; }       // 0=SIM 1=设备
        public uint Index { get; set; }
        public string From { get; set; } = "";
        public string Time { get; set; } = "";
        public string Text { get; set; } = "";
        public string StorageName => Storage == 0 ? Resources.Common_Sim : Resources.Common_Device;
    }

    public sealed class SimSmsService
    {
        private const byte WMS = 0x05;
        private QmiModem? _qmi;
        private byte _cid;

        // 开一个 WMS 会话:Open + AllocClient。必须配对 End() 释放 client(否则泄漏 client 槽,会把 modem 通道耗尽)。
        private bool Begin()
        {
            _qmi = new QmiModem();
            if (!_qmi.Open()) { _qmi = null; return false; }
            _cid = _qmi.AllocClient(WMS);
            if (_cid == 0) { _qmi = null; return false; }
            return true;
        }

        private void End()
        {
            try { if (_qmi != null && _cid != 0) _qmi.ReleaseClient(WMS, _cid); } catch { }
            _qmi = null; _cid = 0;
        }

        // 读取并解码 SIM(0)+设备(1) 全部短信。单会话内做完,结束即释放 client。
        public (List<SimSmsRecord> sim, List<SimSmsRecord> device) ReadBoth()
        {
            var sim = new List<SimSmsRecord>();
            var dev = new List<SimSmsRecord>();
            if (!Begin()) return (sim, dev);
            try
            {
                ReadInto(0, sim);
                ReadInto(1, dev);
            }
            finally { End(); }
            return (sim, dev);
        }

        // 只读 SIM(供"短信"页合并)
        public List<SimSmsRecord> ReadSim()
        {
            var sim = new List<SimSmsRecord>();
            if (!Begin()) return sim;
            try { ReadInto(0, sim); }
            finally { End(); }
            return sim;
        }

        // SIM 短信容量(EF_SMS 记录数)。走 QMI UIM(0x0B) GET_FILE_ATTRIBUTES(0x0024),
        // file_id=6F3C + session_type + 空 path(带 path 会 err 0x30)。失败返回 0。
        public int GetSimCapacity()
        {
            var qmi = new QmiModem();
            if (!qmi.Open()) return 0;
            byte cid = qmi.AllocClient(0x0B);
            if (cid == 0) return 0;
            try
            {
                byte[] file = { 0x3C, 0x6F, 0x00 };          // file_id 6F3C LE + path_len=0
                byte[] sinfo = { 0x00, 0x00 };               // session_type=0(primary gw) + aid_len=0
                byte[] body = Concat(QmiModem.Tlv(0x01, sinfo), QmiModem.Tlv(0x02, file));
                var r = qmi.SendService(0x0B, cid, 0x0024, body);
                if (r == null) return 0;
                var t = QmiModem.Parse(r.Payload);
                if (t.TryGetValue(0x11, out var fa) && fa != null && fa.Length >= 9)
                    return fa[7] | fa[8] << 8;               // record_count
                return 0;
            }
            catch { return 0; }
            finally { try { qmi.ReleaseClient(0x0B, cid); } catch { } }
        }

        private void ReadInto(byte storage, List<SimSmsRecord> res)
        {
            foreach (var i in ListIndices(storage))
            {
                var pdu = RawRead(storage, i);
                var rec = new SimSmsRecord { Storage = storage, Index = i };
                if (pdu != null)
                {
                    try { var d = DecodeDeliver(pdu); rec.From = d.from; rec.Time = d.time; rec.Text = d.text; }
                    catch { rec.Text = Resources.Sim_DecodeFailed; }
                }
                else rec.Text = Resources.Sim_ReadFailed;
                res.Add(rec);
            }
        }

        // 私有:列出某存储索引(调用方须已 Begin)
        private List<uint> ListIndices(byte storage)
        {
            var outp = new List<uint>();
            byte[] body = Concat(QmiModem.Tlv(0x01, new byte[] { storage }), QmiModem.Tlv(0x11, new byte[] { 1 }));
            var r = _qmi!.SendService(WMS, _cid, 0x0031, body);
            if (r == null) return outp;
            var t = QmiModem.Parse(r.Payload);
            if (!t.TryGetValue(0x01, out var list) || list == null || list.Length < 4) return outp;
            int n = list[0] | list[1] << 8 | list[2] << 16 | list[3] << 24;
            for (int k = 0; k < n; k++)
            {
                int off = 4 + k * 5;
                if (off + 5 > list.Length) break;
                outp.Add((uint)(list[off] | list[off + 1] << 8 | list[off + 2] << 16 | list[off + 3] << 24));
            }
            return outp;
        }

        private byte[]? RawRead(byte storage, uint index)
        {
            byte[] raw = { storage, (byte)(index & 0xFF), (byte)(index >> 8), (byte)(index >> 16), (byte)(index >> 24) };
            byte[] body = Concat(QmiModem.Tlv(0x01, raw), QmiModem.Tlv(0x10, new byte[] { 1 }));
            var r = _qmi!.SendService(WMS, _cid, 0x0022, body);
            if (r == null) return null;
            var t = QmiModem.Parse(r.Payload);
            if (!t.TryGetValue(0x01, out var v) || v == null || v.Length < 5) return null;
            int len = v[2] | v[3] << 8;
            if (4 + len > v.Length) len = v.Length - 4;
            return Slice(v, 4, len);
        }

        // 删除指定索引;返回是否成功
        public bool Delete(byte storage, uint index)
        {
            if (!Begin()) return false;
            try
            {
                byte[] body = Concat(Concat(
                    QmiModem.Tlv(0x01, new byte[] { storage }),
                    QmiModem.Tlv(0x10, U32(index))),
                    QmiModem.Tlv(0x12, new byte[] { 1 }));
                return Ok(_qmi!.SendService(WMS, _cid, 0x0024, body));
            }
            finally { End(); }
        }

        // 清空某存储(0x01 storage + 0x12 mode)
        public bool DeleteAll(byte storage)
        {
            if (!Begin()) return false;
            try
            {
                byte[] body = Concat(QmiModem.Tlv(0x01, new byte[] { storage }), QmiModem.Tlv(0x12, new byte[] { 1 }));
                return Ok(_qmi!.SendService(WMS, _cid, 0x0024, body));
            }
            finally { End(); }
        }

        private static bool Ok(QmiModem.QmiResponse? r)
        {
            if (r == null) return false;
            var t = QmiModem.Parse(r.Payload);
            return t.TryGetValue(0x02, out var res) && res != null && res.Length >= 2 && (res[0] | res[1] << 8) == 0;
        }

        private static byte[] U32(uint v) => new byte[] { (byte)(v & 0xFF), (byte)(v >> 8), (byte)(v >> 16), (byte)(v >> 24) };

        // ---- GSM SMS-DELIVER 解码(从 qmiprobe/SimSms.cs 移植) ----
        private struct Dec { public string from, time, text; }
        private static Dec DecodeDeliver(byte[] p)
        {
            int i = 0;
            int smscLen = p[i++]; i += smscLen;
            byte flags = p[i++];
            bool udhi = (flags & 0x40) != 0;
            int addrLenDigits = p[i++];
            byte ton = p[i++];
            int addrBytes = (addrLenDigits + 1) / 2;
            string from = DecodeAddr(p, i, addrLenDigits, ton); i += addrBytes;
            i++;                       // PID
            byte dcs = p[i++];
            string time = DecodeScts(p, i); i += 7;
            int udl = p[i++];
            byte[] ud = Slice(p, i, Math.Min(p.Length - i, dcs == 0x08 ? udl : (udl * 7 + 7) / 8 + 8));
            string text = DecodeUd(ud, dcs, udl, udhi);
            return new Dec { from = from, time = time, text = text };
        }

        private static string DecodeAddr(byte[] p, int off, int digits, byte ton)
        {
            if ((ton & 0x70) == 0x50)  // alphanumeric
                return GsmUnpack(Slice(p, off, (digits + 1) / 2), (digits * 4) / 7);
            var sb = new StringBuilder();
            int n = (digits + 1) / 2;
            for (int k = 0; k < n; k++)
            {
                int b = p[off + k], lo = b & 0x0F, hi = (b >> 4) & 0x0F;
                if (lo < 10) sb.Append((char)('0' + lo));
                if (hi < 10) sb.Append((char)('0' + hi));
            }
            string s = sb.ToString();
            if (s.Length > digits) s = s.Substring(0, digits);
            return ((ton & 0x70) == 0x10 ? "+" : "") + s;
        }

        private static string DecodeScts(byte[] p, int off)
        {
            Func<int, string> d = b => { int lo = p[off + b] & 0x0F, hi = (p[off + b] >> 4) & 0x0F; return "" + lo + hi; };
            return "20" + d(0) + "-" + d(1) + "-" + d(2) + " " + d(3) + ":" + d(4) + ":" + d(5);
        }

        private static string DecodeUd(byte[] ud, byte dcs, int udl, bool udhi)
        {
            int udhLen = 0;
            if (udhi && ud.Length > 0) udhLen = ud[0] + 1;
            if (dcs == 0x08)
            {
                byte[] body = Slice(ud, udhLen, Math.Max(0, ud.Length - udhLen));
                try { return Encoding.BigEndianUnicode.GetString(body); } catch { return "(ucs2?)"; }
            }
            int skipSeptets = udhi ? (udhLen * 8 + 6) / 7 : 0;
            string all = GsmUnpack(ud, udl);
            return skipSeptets < all.Length ? all.Substring(skipSeptets) : all;
        }

        private const string Gsm = "@£$¥èéùìòÇ\nØø\rÅåΔ_ΦΓΛΩΠΨΣΘΞÆæßÉ !\"#¤%&'()*+,-./0123456789:;<=>?¡ABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÑÜ§¿abcdefghijklmnopqrstuvwxyzäöñüà";
        private static string GsmUnpack(byte[] data, int septets)
        {
            var sb = new StringBuilder();
            int carry = 0, carryBits = 0, produced = 0;
            for (int i = 0; i < data.Length && produced < septets; i++)
            {
                carry |= data[i] << carryBits; carryBits += 8;
                while (carryBits >= 7 && produced < septets)
                {
                    int sept = carry & 0x7F; carry >>= 7; carryBits -= 7;
                    sb.Append(sept < Gsm.Length ? Gsm[sept] : '?'); produced++;
                }
            }
            return sb.ToString();
        }

        private static byte[] Concat(byte[] a, byte[] b)
        {
            byte[] r = new byte[a.Length + b.Length];
            System.Buffer.BlockCopy(a, 0, r, 0, a.Length);
            System.Buffer.BlockCopy(b, 0, r, a.Length, b.Length);
            return r;
        }

        private static byte[] Slice(byte[] s, int o, int l)
        {
            if (l < 0) l = 0;
            byte[] r = new byte[l];
            System.Buffer.BlockCopy(s, o, r, 0, l);
            return r;
        }
    }
}
