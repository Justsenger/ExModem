using System;
using System.Collections.Generic;
using System.Text;
using ExModem.Core;
using ExModem.Properties;

namespace ExModem.Services
{
    // SIM 卡电话簿 EF_ADN(6F3A) 联系人。经 QMI UIM 文件接口直读(绕开被锁的 PBM)。
    //  UIM(0x0B):GET_FILE_ATTRIBUTES=0x0024(拿 record_count/size)、READ_RECORD=0x0021、WRITE_RECORD=0x0023。
    //  关键:session_type 必须=6(card-slot-1),file path 留空。
    public sealed class SimContact
    {
        public int Index { get; set; }       // EF_ADN 记录号(1-based)
        public string Name { get; set; } = "";
        public string Number { get; set; } = "";
    }

    public sealed class SimContactsService
    {
        private const byte UIM = 0x0B;
        private const ushort EF_ADN = 0x6F3A;
        private QmiModem? _qmi;
        private byte _cid;

        private bool Begin()
        {
            _qmi = new QmiModem();
            if (!_qmi.Open()) { _qmi = null; return false; }
            _cid = _qmi.AllocClient(UIM);
            if (_cid == 0) { _qmi = null; return false; }
            return true;
        }

        private void End()
        {
            try { if (_qmi != null && _cid != 0) _qmi.ReleaseClient(UIM, _cid); } catch { }
            _qmi = null; _cid = 0;
        }

        // 返回 (联系人列表, 容量=记录槽数)
        public (List<SimContact> list, int capacity) ReadAll()
        {
            var res = new List<SimContact>();
            if (!Begin()) return (res, 0);
            int cap = 0;
            try
            {
                // 250 条逐槽读,复用一个命令会话(实测约 2.5× 提速)
                _qmi!.RunBatch(() =>
                {
                    var (rcount, rsize) = GetAttr();
                    cap = rcount;
                    if (rcount == 0 || rsize == 0) return;
                    for (int rec = 1; rec <= rcount; rec++)
                    {
                        var data = ReadRecord(rec, rsize);
                        if (data == null) continue;
                        var c = Parse(data, rec);
                        if (c != null) res.Add(c);
                    }
                });
                return (res, cap);
            }
            finally { End(); }
        }

        // 新增联系人到 SIM(写第一个空槽)。返回 (成功, 提示)。
        public (bool ok, string msg) Add(string name, string number)
        {
            if (string.IsNullOrWhiteSpace(number)) return (false, Resources.Sim_NeedNumber);
            if (!Begin()) return (false, Resources.Sim_NoModem);
            try
            {
                var (rcount, rsize) = GetAttr();
                if (rcount == 0 || rsize == 0) return (false, Resources.Sim_NoPhonebook);
                int slot = -1;
                for (int rec = 1; rec <= rcount; rec++)
                {
                    var d = ReadRecord(rec, rsize);
                    if (d == null) continue;
                    if (Parse(d, rec) == null) { slot = rec; break; }   // 空槽
                }
                if (slot < 0) return (false, Resources.Sim_PhonebookFull);
                byte[]? rec28 = Encode(name ?? "", number, rsize);
                if (rec28 == null) return (false, Resources.Sim_TooLong);
                return WriteRecord(slot, rec28) ? (true, Resources.Sim_SavedToSim) : (false, Resources.Sim_WriteFailed);
            }
            finally { End(); }
        }

        // 编码 EF_ADN 记录:alpha(rsize-14) + [num_len][TON][BCD*10][CCP][Ext]
        private static byte[]? Encode(string name, string number, int rsize)
        {
            int alphaLen = rsize - 14;
            if (alphaLen < 1) return null;
            byte[] alpha = new byte[alphaLen];
            for (int i = 0; i < alphaLen; i++) alpha[i] = 0xFF;

            bool ascii = true;
            foreach (char ch in name) if (ch < 0x20 || ch > 0x7E) { ascii = false; break; }
            if (ascii)
            {
                for (int i = 0; i < name.Length && i < alphaLen; i++) alpha[i] = (byte)name[i];
            }
            else
            {
                alpha[0] = 0x80;
                int p = 1;
                foreach (char ch in name)
                {
                    if (p + 1 >= alphaLen) break;
                    alpha[p++] = (byte)(ch >> 8);
                    alpha[p++] = (byte)(ch & 0xFF);
                }
            }

            // 号码
            string num = number.Trim();
            bool intl = num.StartsWith("+");
            var digits = new StringBuilder();
            foreach (char ch in num) if (ch >= '0' && ch <= '9') digits.Append(ch);
            string ds = digits.ToString();
            if (ds.Length == 0 || ds.Length > 20) return null;
            byte[] numarea = new byte[14];
            for (int i = 0; i < 14; i++) numarea[i] = 0xFF;
            int bcdBytes = (ds.Length + 1) / 2;
            if (bcdBytes > 10) return null;
            numarea[0] = (byte)(1 + bcdBytes);          // num_len = TON + BCD
            numarea[1] = (byte)(intl ? 0x91 : 0x81);
            for (int i = 0; i < bcdBytes; i++)
            {
                int lo = ds[i * 2] - '0';
                int hi = (i * 2 + 1 < ds.Length) ? ds[i * 2 + 1] - '0' : 0x0F;
                numarea[2 + i] = (byte)((hi << 4) | lo);
            }
            // numarea[12]=CCP=FF, [13]=Ext=FF 已是 0xFF

            byte[] rec = new byte[rsize];
            System.Buffer.BlockCopy(alpha, 0, rec, 0, alphaLen);
            System.Buffer.BlockCopy(numarea, 0, rec, alphaLen, 14);
            return rec;
        }

        // 更新指定记录(覆盖写)。返回 (成功, 提示)。
        public (bool ok, string msg) Update(int index, string name, string number)
        {
            if (string.IsNullOrWhiteSpace(number)) return (false, Resources.Sim_NeedNumber);
            if (!Begin()) return (false, Resources.Sim_NoModem);
            try
            {
                var (_, rsize) = GetAttr();
                if (rsize == 0) return (false, Resources.Sim_NoPhonebook);
                byte[]? rec = Encode(name ?? "", number, rsize);
                if (rec == null) return (false, Resources.Sim_TooLong);
                return WriteRecord(index, rec) ? (true, Resources.Sim_Updated) : (false, Resources.Sim_WriteFailed);
            }
            finally { End(); }
        }

        // 删除指定记录(写回全 0xFF)。返回是否成功。
        public bool Delete(int index)
        {
            if (!Begin()) return false;
            try
            {
                var (_, rsize) = GetAttr();
                if (rsize == 0) return false;
                byte[] blank = new byte[rsize];
                for (int i = 0; i < rsize; i++) blank[i] = 0xFF;
                return WriteRecord(index, blank);
            }
            finally { End(); }
        }

        // ---- QMI UIM 原语(调用方须已 Begin) ----
        private (int count, int size) GetAttr()
        {
            byte[] file = { (byte)(EF_ADN & 0xFF), (byte)(EF_ADN >> 8), 0x00 };
            byte[] body = Concat(QmiModem.Tlv(0x01, new byte[] { 6, 0 }), QmiModem.Tlv(0x02, file));
            var r = _qmi!.SendService(UIM, _cid, 0x0024, body);
            if (r == null) return (0, 0);
            var t = QmiModem.Parse(r.Payload);
            if (t.TryGetValue(0x11, out var fa) && fa != null && fa.Length >= 9)
                return (fa[7] | fa[8] << 8, fa[5] | fa[6] << 8);
            return (0, 0);
        }

        private byte[]? ReadRecord(int recNo, int recLen)
        {
            byte[] file = { (byte)(EF_ADN & 0xFF), (byte)(EF_ADN >> 8), 0x00 };
            byte[] rr = { (byte)(recNo & 0xFF), (byte)(recNo >> 8), (byte)(recLen & 0xFF), (byte)(recLen >> 8) };
            byte[] body = Concat(Concat(QmiModem.Tlv(0x01, new byte[] { 6, 0 }), QmiModem.Tlv(0x02, file)), QmiModem.Tlv(0x03, rr));
            var r = _qmi!.SendService(UIM, _cid, 0x0021, body);
            if (r == null) return null;
            var t = QmiModem.Parse(r.Payload);
            if (!t.TryGetValue(0x11, out var v) || v == null || v.Length < 2) return null;
            int len = v[0] | v[1] << 8;
            if (2 + len > v.Length) len = v.Length - 2;
            return Slice(v, 2, len);
        }

        private bool WriteRecord(int recNo, byte[] data)
        {
            byte[] file = { (byte)(EF_ADN & 0xFF), (byte)(EF_ADN >> 8), 0x00 };
            var wr = new List<byte> { (byte)(recNo & 0xFF), (byte)(recNo >> 8), (byte)(data.Length & 0xFF), (byte)(data.Length >> 8) };
            wr.AddRange(data);
            byte[] body = Concat(Concat(QmiModem.Tlv(0x01, new byte[] { 6, 0 }), QmiModem.Tlv(0x02, file)), QmiModem.Tlv(0x03, wr.ToArray()));
            var r = _qmi!.SendService(UIM, _cid, 0x0023, body);
            if (r == null) return false;
            var t = QmiModem.Parse(r.Payload);
            return t.TryGetValue(0x02, out var res) && res != null && res.Length >= 2 && (res[0] | res[1] << 8) == 0;
        }

        // EF_ADN 记录解析:[alpha(R-14)] [num_len(1)] [TON(1)] [BCD 10] [CCP] [Ext]
        private static SimContact? Parse(byte[] r, int rec)
        {
            if (r.Length < 14) return null;
            int numStart = r.Length - 14;
            string name = DecodeName(Slice(r, 0, numStart));
            string num = DecodeNumber(r, numStart);
            if (name.Length == 0 && num.Length == 0) return null;   // 空槽
            return new SimContact { Index = rec, Name = name, Number = num };
        }

        private static string DecodeName(byte[] a)
        {
            if (a.Length == 0) return "";
            if (a[0] == 0x80)   // UCS2 大端
            {
                var sb = new StringBuilder();
                for (int i = 1; i + 1 < a.Length; i += 2)
                {
                    int u = a[i] << 8 | a[i + 1];
                    if (u == 0xFFFF || u == 0) break;
                    sb.Append((char)u);
                }
                return sb.ToString();
            }
            // 其余按 ASCII(GSM 默认字母在字母/数字段与 ASCII 一致),0xFF 结束
            var s = new StringBuilder();
            foreach (byte b in a)
            {
                if (b == 0xFF) break;
                s.Append(b >= 0x20 && b < 0x7F ? (char)b : '?');
            }
            return s.ToString().Trim();
        }

        private static string DecodeNumber(byte[] r, int numStart)
        {
            int L = r[numStart] & 0xFF;
            if (L == 0 || L == 0xFF) return "";
            int ton = r[numStart + 1] & 0xFF;
            int digitBytes = L - 1; if (digitBytes > 10) digitBytes = 10;
            var sb = new StringBuilder();
            for (int k = 0; k < digitBytes; k++)
            {
                int b = r[numStart + 2 + k], lo = b & 0x0F, hi = (b >> 4) & 0x0F;
                if (lo <= 9) sb.Append((char)('0' + lo)); else break;
                if (hi <= 9) sb.Append((char)('0' + hi)); else break;
            }
            return ((ton & 0x70) == 0x10 ? "+" : "") + sb.ToString();
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
