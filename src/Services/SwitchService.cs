using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using ExModem.Core.Backends;
using ExModem.Properties;

namespace ExModem.Services
{
    // Smart auto RAT switcher: idle -> LTE (SMS works), sustained heavy traffic -> 5G.
    // Throughput hysteresis on the cellular adapter avoids flapping. RAT 走 Modem.Current(后端无关)。
    public sealed class SwitchService
    {
        private readonly Action<string> _log;
        private CancellationTokenSource? _cts;

        public double UpMbps = 18;    // sustained >= this -> consider boosting to 5G
        public double DownMbps = 2;   // sustained <= this -> drop back to LTE
        public int UpSeconds = 5;     // how long high before boosting
        public int DownSeconds = 20;  // how long low before dropping

        public SwitchService(Action<string> log)
        {
            _log = log;
        }

        public bool IsRunning => _cts != null;

        public void Start()
        {
            Stop();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            Task.Run(() => Loop(token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
        }

        private async Task Loop(CancellationToken ct)
        {
            var nic = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.NetworkInterfaceType is NetworkInterfaceType.Wwanpp or NetworkInterfaceType.Wwanpp2);
            if (nic == null) { _log(Resources.Switch_NoNic); return; }

            long last = Bytes(nic);
            DateTime lastT = DateTime.UtcNow;
            bool on5g = HasNr(Modem.Current.GetModePref());
            DateTime? highSince = null, lowSince = null;
            _log(on5g ? Resources.Switch_Now5G : Resources.Switch_NowLte);

            while (!ct.IsCancellationRequested)
            {
                try { await Task.Delay(2000, ct); } catch { break; }

                long now = Bytes(nic);
                DateTime t = DateTime.UtcNow;
                double dt = (t - lastT).TotalSeconds;
                double mbps = dt > 0 ? (now - last) * 8.0 / dt / 1_000_000.0 : 0;
                last = now; lastT = t;

                if (mbps >= UpMbps)
                {
                    lowSince = null;
                    highSince ??= t;
                    if (!on5g && (t - highSince.Value).TotalSeconds >= UpSeconds)
                    {
                        _log(string.Format(Resources.Switch_Up5G, mbps.ToString("F0")));
                        if (Modem.Current.SetModePref(0x7C)) on5g = true;
                        highSince = null;
                    }
                }
                else if (mbps <= DownMbps)
                {
                    highSince = null;
                    lowSince ??= t;
                    if (on5g && (t - lowSince.Value).TotalSeconds >= DownSeconds)
                    {
                        _log(Resources.Switch_DownLte);
                        if (Modem.Current.SetModePref(0x3C)) on5g = false;
                        lowSince = null;
                    }
                }
                else
                {
                    highSince = null;
                    lowSince = null;
                }
            }
        }

        private static long Bytes(NetworkInterface n)
        {
            try { var s = n.GetIPv4Statistics(); return s.BytesReceived + s.BytesSent; }
            catch { return 0; }
        }

        private static bool HasNr(int modePref) => modePref > 0 && (modePref & 0x40) != 0;
    }
}
