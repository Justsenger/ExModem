using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExModem.Core;

namespace ExModem.Services
{
    // RAT lock via QMI NAS SET/GET_SYSTEM_SELECTION_PREFERENCE (mode_pref TLV 0x11).
    //   0x3C = GSM|UMTS|LTE|TDSCDMA (no NR)  -> "SMS mode" (LTE, SMS works)
    //   0x7C = GSM|UMTS|LTE|TDSCDMA|NR5G     -> "Fast mode" (5G allowed; 0x7F 的 0x01/0x02 是 CDMA 杂位,本卡无 CDMA,去掉)
    //   0x10 = LTE only                      -> 切 5G 前用它强制一次干净的 LTE 重选
    //   0x40 = NR only                       -> test-only (has ~10-30s cold-attach gap)
    public enum RatMode
    {
        LteSms = 0x3C,
        Fast5G = 0x7C,
        LteOnly = 0x10,
        NrOnly = 0x40,
    }

    public sealed class ModeService
    {
        private const byte NAS = 0x03;
        private const ushort GetSysSelPref = 0x0034;
        private const ushort SetSysSelPref = 0x0033;
        private const byte TlvModePref = 0x11;
        private const byte TlvResult = 0x02;

        // Returns the current mode_pref bits, or -1 on failure.
        public int GetModePref()
        {
            var qmi = new QmiModem();
            if (!qmi.Open()) return -1;
            byte cid = qmi.AllocClient(NAS);
            if (cid == 0) return -1;
            try
            {
                var r = qmi.SendService(NAS, cid, GetSysSelPref, Array.Empty<byte>());
                if (r == null) return -1;
                var tlvs = QmiModem.Parse(r.Payload);
                return tlvs.TryGetValue(TlvModePref, out var v) && v.Length >= 2 ? v[0] | v[1] << 8 : -1;
            }
            finally
            {
                qmi.ReleaseClient(NAS, cid);
            }
        }

        // Sets mode_pref bits. Returns true when the modem reports result=0 (OK).
        public bool SetModePref(int modePref)
        {
            var qmi = new QmiModem();
            if (!qmi.Open()) return false;
            byte cid = qmi.AllocClient(NAS);
            if (cid == 0) return false;
            try
            {
                byte[] tlv = QmiModem.Tlv(TlvModePref, new[] { (byte)(modePref & 0xFF), (byte)(modePref >> 8) });
                var r = qmi.SendService(NAS, cid, SetSysSelPref, tlv);
                if (r == null) return false;
                var tlvs = QmiModem.Parse(r.Payload);
                if (tlvs.TryGetValue(TlvResult, out var res) && res.Length >= 2)
                    return (res[0] | res[1] << 8) == 0;
                return false;
            }
            finally
            {
                qmi.ReleaseClient(NAS, cid);
            }
        }

        public bool LockLteForSms() => SetModePref((int)RatMode.LteSms);
        public bool EnableFast5G() => SetModePref((int)RatMode.Fast5G);

        public Task<bool> SetModePrefAsync(int modePref) => Task.Run(() => SetModePref(modePref));
        public Task<int> GetModePrefAsync() => Task.Run(GetModePref);

        // Human-readable RAT list from mode_pref bits.
        public static string Describe(int bits)
        {
            if (bits < 0) return "Unknown";
            var s = new List<string>();
            if ((bits & 0x04) != 0) s.Add("GSM");
            if ((bits & 0x08) != 0) s.Add("UMTS");
            if ((bits & 0x10) != 0) s.Add("LTE");
            if ((bits & 0x20) != 0) s.Add("TDSCDMA");
            if ((bits & 0x40) != 0) s.Add("NR5G");
            return s.Count == 0 ? "(none)" : string.Join("  ·  ", s);
        }
    }
}
