using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using ExModem.Core;
using Windows.Devices.Sms;
using Windows.Networking.NetworkOperators;
using ExModem.Properties;

namespace ExModem.Services
{
    public sealed class CellStatus
    {
        public string Adapter = "—";
        public string Provider = "—";
        public string RegisterState = "—";
        public string DataClass = "—";
        public string SignalPercent = "—";
        public string Rssi = "—";
        public int SignalLevel;     // 0-4 信号格
        public int ModePref = -1;

        // 设备信息(基本不变,读一次缓存)
        public string Imei = "—";
        public string Iccid = "—";
        public string Imsi = "—";
        public string Firmware = "—";
        public string Smsc = "—";
        public string Phone = "—";
        public string Manufacturer = "—";
        public string Model = "—";
    }

    // 读蜂窝参数 + 设备信息。运营商/注册/RAT 用 WinRT(语言无关)并映射成中文友好名;
    // 信号/RSSI 用正则提数值;设备信息(IMEI/ICCID/IMSI/固件/SMSC)缓存一次。
    public sealed class SignalService
    {
        private MobileBroadbandModem? _modem;
        private string? _adapterName;

        // 设备信息缓存
        private bool _devLoaded;
        private string _imei = "—", _iccid = "—", _imsi = "—", _firmware = "—", _smsc = "—", _phone = "—";
        private string _manufacturer = "—", _model = "—";

        public string GetAdapterName()
        {
            if (string.IsNullOrEmpty(_adapterName))
            {
                _adapterName = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(n => n.NetworkInterfaceType is NetworkInterfaceType.Wwanpp or NetworkInterfaceType.Wwanpp2)
                    ?.Name ?? "";
            }
            return _adapterName ?? "";
        }

        private MobileBroadbandModem? Modem()
        {
            try { _modem ??= MobileBroadbandModem.GetDefault(); }
            catch { _modem = null; }
            return _modem;
        }

        public CellStatus Read(bool includeModePref)
        {
            var st = new CellStatus();
            string name = GetAdapterName();
            st.Adapter = string.IsNullOrEmpty(name) ? "—" : name;

            try
            {
                var modem = Modem();
                MobileBroadbandNetwork? net = modem?.CurrentNetwork;
                if (net != null)
                {
                    st.Provider = Carrier(net.RegisteredProviderName, net.RegisteredProviderId);
                    st.RegisterState = Reg(net.NetworkRegistrationState);
                    st.DataClass = DataClassFriendly(net.RegisteredDataClass);
                }
                LoadDeviceInfo(modem);
            }
            catch
            {
                _modem = null;
            }

            st.Imei = _imei; st.Iccid = _iccid; st.Imsi = _imsi;
            st.Firmware = _firmware; st.Smsc = _smsc; st.Phone = _phone;
            st.Manufacturer = _manufacturer; st.Model = _model;

            string iface = RunNetsh("mbn", "show", "interfaces");
            string pct = Match(iface, @"(\d{1,3})\s*%");
            st.SignalPercent = pct == "" ? "—" : pct + "%";
            st.SignalLevel = LevelFromPct(pct);
            string dbm = Match(iface, @"(-?\d+)\s*dBm");
            st.Rssi = dbm == "" ? "—" : dbm + " dBm";

            // 当前网络精确制式(含 5G SA/NSA):netsh mbn show connection 的 Provider Data Class
            // 比 WinRT 的 DataClasses(只能猜 0x80=5G)更准,有则覆盖。
            string conn = RunNetsh("mbn", "show", "connection", "interface=" + name);
            var dc = DataClassFromConn(conn);
            if (dc != null) st.DataClass = dc;

            if (includeModePref) st.ModePref = new ModeService().GetModePref();
            return st;
        }

        private void LoadDeviceInfo(MobileBroadbandModem? modem)
        {
            if (_devLoaded || modem == null) return;
            try
            {
                var d = modem.DeviceInformation;
                if (d != null)
                {
                    _imei = NonEmpty(d.MobileEquipmentId);
                    _iccid = NonEmpty(d.SimIccId);
                    _imsi = NonEmpty(d.SubscriberId);
                    _firmware = NonEmpty(d.FirmwareInformation);
                    _manufacturer = NonEmpty(d.Manufacturer);
                    _model = NonEmpty(d.Model);
                    if (d.TelephoneNumbers != null && d.TelephoneNumbers.Count > 0)
                        _phone = NonEmpty(d.TelephoneNumbers[0]);
                }
            }
            catch { }
            try
            {
                var sms = SmsDevice2.GetDefault();
                if (sms != null) _smsc = NonEmpty(sms.SmscAddress);
            }
            catch { }
            _devLoaded = true;
        }

        private static string NonEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? "—" : s.Trim();

        // 从 netsh mbn show connection 文本里按 RAT 关键字判定(语言无关,扫值不扫标签)。
        private static string? DataClassFromConn(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            if (Regex.IsMatch(text, @"5G\s*\(\s*SA\s*\)", RegexOptions.IgnoreCase)) return "5G(SA)";
            if (Regex.IsMatch(text, @"5G\s*\(\s*NSA\s*\)", RegexOptions.IgnoreCase)) return "5G(NSA)";
            if (Regex.IsMatch(text, @"\b5G\b", RegexOptions.IgnoreCase)) return "5G";
            if (Regex.IsMatch(text, @"\bLTE\b", RegexOptions.IgnoreCase)) return "4G";
            if (Regex.IsMatch(text, @"HSPA|HSDPA|HSUPA|UMTS|WCDMA", RegexOptions.IgnoreCase)) return "3G";
            if (Regex.IsMatch(text, @"GPRS|EDGE", RegexOptions.IgnoreCase)) return "2G";
            return null;
        }

        // 蜂窝数据开关(netsh mbn set/show dataenablement)。只关数据,射频不动→短信不受影响。
        public bool? GetDataEnabled()
        {
            string name = GetAdapterName();
            if (string.IsNullOrEmpty(name)) return null;
            string o = RunNetsh("mbn", "show", "dataenablement", "interface=" + name);
            if (string.IsNullOrEmpty(o)) return null;
            if (Regex.IsMatch(o, "Disabl|禁用", RegexOptions.IgnoreCase)) return false;
            if (Regex.IsMatch(o, "Enabl|启用", RegexOptions.IgnoreCase)) return true;
            return null;
        }

        public void SetDataEnabled(bool on)
        {
            string name = GetAdapterName();
            if (string.IsNullOrEmpty(name)) return;
            RunNetsh("mbn", "set", "dataenablement", "interface=" + name, "profileset=internet", "mode=" + (on ? "yes" : "no"));
        }

        // 5G NR 信号:netsh 在 NR 上只给 RSSI 哨兵(0%/-113),NR 真实 RSRP 在 QMI NAS
        // GET_SIGNAL_INFO(0x004D) 的 NR 专属 OEM TLV 0x60 首字节(实测:LTE 无此 TLV,5G 才有)。
        // 仅在 5G 时调用;后台线程跑(QMI Open 会暖通道,别放 UI 线程)。返回 (dBm, 0-4 格, 百分比)。
        private QmiModem? _qmi;
        private bool _qmiOpen;
        public (int dbm, int level, int pct)? ReadNrSignalViaQmi()
        {
            try
            {
                _qmi ??= new QmiModem();
                if (!_qmiOpen) _qmiOpen = _qmi.Open();
                if (!_qmiOpen) return null;
                byte cid = _qmi.AllocClient(0x03);   // NAS
                if (cid == 0) { _qmiOpen = false; return null; }
                try
                {
                    var r = _qmi.SendService(0x03, cid, 0x004D, System.Array.Empty<byte>());
                    if (r == null) return null;
                    var t = QmiModem.Parse(r.Payload);
                    if (t.TryGetValue(0x60, out var v) && v.Length >= 1)
                    {
                        int dbm = (sbyte)v[0];
                        if (dbm <= -40 && dbm >= -140)
                        {
                            int level = dbm >= -85 ? 4 : dbm >= -95 ? 3 : dbm >= -105 ? 2 : 1;
                            int pct = System.Math.Max(5, System.Math.Min(100, (int)System.Math.Round((dbm + 120) / 50.0 * 100)));
                            return (dbm, level, pct);
                        }
                    }
                    return null;
                }
                finally { _qmi.ReleaseClient(0x03, cid); }
            }
            catch { _qmiOpen = false; return null; }
        }

        // 运营商:优先按 MCC/MNC,其次按短名;国外名一般本身可读,直接返回。
        private static string Carrier(string name, string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                switch (id)
                {
                    case "46000": case "46002": case "46004": case "46007": case "46008": return Resources.Carrier_ChinaMobile;
                    case "46001": case "46006": case "46009": return Resources.Carrier_ChinaUnicom;
                    case "46003": case "46005": case "46011": return Resources.Carrier_ChinaTelecom;
                    case "46015": return Resources.Carrier_ChinaBroadnet;
                }
            }
            string n = (name ?? "").Trim();
            if (n.Length == 0) return "—";
            string up = n.ToUpperInvariant();
            if (up.Contains("CMCC") || up.Contains("CHINA MOBILE")) return Resources.Carrier_ChinaMobile;
            if (up.Contains("CUCC") || up.Contains("UNICOM")) return Resources.Carrier_ChinaUnicom;
            if (up.Contains("CTCC") || up.Contains("CHINA TELECOM") || up.Contains("CHN-CT")) return Resources.Carrier_ChinaTelecom;
            return n;
        }

        private static string Reg(NetworkRegistrationState s) => s switch
        {
            NetworkRegistrationState.None => Resources.Reg_NotRegistered,
            NetworkRegistrationState.Deregistered => Resources.Reg_NotRegistered,
            NetworkRegistrationState.Searching => Resources.Reg_Searching,
            NetworkRegistrationState.Home => Resources.Reg_Home,
            NetworkRegistrationState.Roaming => Resources.Reg_Roaming,
            NetworkRegistrationState.Partner => Resources.Reg_Partner,
            NetworkRegistrationState.Denied => Resources.Reg_Denied,
            _ => Resources.Common_Unknown,
        };

        // 5G NR 在 19041 托管枚举里没有名字,实测本机回报裸位 0x80(=128);老 Custom 是 0x80000000。
        // 两者都视为 5G,且优先判 5G(NSA 时可能同时置 LTE 位)。
        private static string DataClassFriendly(DataClasses dc)
        {
            uint v = (uint)dc;
            if (v == 0) return Resources.Signal_None;
            if ((v & 0x80) != 0 || (v & 0x80000000) != 0) return "5G";
            if ((v & 0x20) != 0) return "4G";                       // LteAdvanced
            if ((v & (0x04u | 0x08u | 0x10u)) != 0) return "3G";    // Umts/Hsdpa/Hsupa
            if ((v & (0x01u | 0x02u)) != 0) return "2G";            // Gprs/Edge
            return Resources.Common_Unknown;
        }

        private static int LevelFromPct(string pct)
        {
            if (!int.TryParse(pct, out int p) || p <= 0) return 0;
            if (p >= 75) return 4;
            if (p >= 50) return 3;
            if (p >= 25) return 2;
            return 1;
        }

        private static string Match(string text, string pattern)
        {
            var m = Regex.Match(text, pattern);
            return m.Success ? m.Groups[1].Value : "";
        }

        private static string RunNetsh(params string[] args)
        {
            try
            {
                var psi = new ProcessStartInfo("netsh")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                foreach (var a in args) psi.ArgumentList.Add(a);
                using var p = Process.Start(psi)!;
                string o = p.StandardOutput.ReadToEnd();
                p.WaitForExit(3000);
                return o;
            }
            catch
            {
                return "";
            }
        }
    }
}
