using System;
using System.Collections.Generic;
using System.Threading;
using Windows.Foundation;
using Windows.Networking.NetworkOperators;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace ExModem.Core
{
    // Raw QMI/QMUX passthrough over the Qualcomm MBIM vendor device service.
    // Ported from the qmiprobe C# probes (NasPref/SendSms/etc). Works UNPACKAGED
    // (no MSIX / restricted capabilities needed) on Qualcomm WoA modems.
    public sealed class QmiModem
    {
        // Qualcomm MBIM vendor passthrough device service GUID (MBIM_SERVICE_QMUX_EXT).
        private static readonly Guid QmiServiceGuid = new("d1a30bc2-f97a-6e43-bf65-c7e24fb0f0d3");

        private MobileBroadbandModem? _modem;
        private MobileBroadbandDeviceService? _svc;
        private MobileBroadbandDeviceServiceCommandSession? _batchSession;
        private int _tx = 90;

        // 直通通道是串行的:全进程共用一把锁,任意时刻只允许一个 QMI 操作开命令会话,
        // 避免多服务(信号轮询/自动切换/SIM 读取)并发开会话互相踩(Monitor 可重入,RunBatch 内的 SendOnce 同线程再加锁无妨)。
        private static readonly object QmiGate = new object();

        // 在一个持有的命令会话里跑一批 SendService 调用,省掉每条开/关会话的开销(实测约 2.5× 提速)。
        public void RunBatch(Action body)
        {
            if (_svc == null) { body(); return; }
            lock (QmiGate)
            {
                var s = _svc.OpenCommandSession();
                _batchSession = s;
                try { body(); }
                finally { _batchSession = null; try { s.CloseSession(); } catch { } }
            }
        }

        public sealed class QmiResponse
        {
            public ushort MsgId;
            public byte[] Payload = Array.Empty<byte>();
        }

        // Open the modem's QMI passthrough channel. Returns false if no modem / no service.
        public bool Open()
        {
            _modem = MobileBroadbandModem.GetDefault();
            if (_modem == null) return false;
            _svc = _modem.GetDeviceService(QmiServiceGuid);
            if (_svc == null) return false;

            // Warm-up: CTL GET_VERSION_INFO (0x0021) to wake the QMUX channel.
            byte[] warm = { 0x01, 0x0B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x21, 0x00, 0x00, 0x00 };
            for (int i = 0; i < 5; i++)
            {
                var r = SendOnce(warm);
                if (r != null && r.Length > 10) break;
                Thread.Sleep(300);
            }
            return true;
        }

        public string RegistrationState
            => _modem?.CurrentNetwork?.NetworkRegistrationState.ToString() ?? "Unknown";

        // Allocate a QMI client for a service id (CTL 0x0022). Returns client id, 0 on failure.
        public byte AllocClient(byte service)
        {
            var r = SendCtl(0x0022, Tlv(0x01, new[] { service }));
            if (r == null) return 0;
            var t = Parse(r.Payload);
            return t.TryGetValue(0x01, out var c) && c.Length >= 2 ? c[1] : (byte)0;
        }

        public void ReleaseClient(byte service, byte cid)
            => SendCtl(0x0023, Tlv(0x01, new[] { service, cid }));

        // Send a request on a (service, client). Returns parsed response, null on failure.
        public QmiResponse? SendService(byte service, byte cid, ushort msgId, byte[] tlv)
        {
            ushort tx = (ushort)(_tx++);
            byte[] b = new byte[7 + tlv.Length];
            b[0] = 0;
            b[1] = (byte)(tx & 0xFF);
            b[2] = (byte)(tx >> 8);
            b[3] = (byte)(msgId & 0xFF);
            b[4] = (byte)(msgId >> 8);
            b[5] = (byte)(tlv.Length & 0xFF);
            b[6] = (byte)(tlv.Length >> 8);
            System.Buffer.BlockCopy(tlv, 0, b, 7, tlv.Length);
            var r = SendRaw(Wrap(service, cid, b));
            if (r == null || r.Length < 13) return null;
            return new QmiResponse { MsgId = (ushort)(r[9] | r[10] << 8), Payload = Slice(r, 13, r.Length - 13) };
        }

        private QmiResponse? SendCtl(ushort msgId, byte[] tlv)
        {
            byte tx = (byte)(_tx++);
            byte[] b = new byte[6 + tlv.Length];
            b[0] = 0;
            b[1] = tx;
            b[2] = (byte)(msgId & 0xFF);
            b[3] = (byte)(msgId >> 8);
            b[4] = (byte)(tlv.Length & 0xFF);
            b[5] = (byte)(tlv.Length >> 8);
            System.Buffer.BlockCopy(tlv, 0, b, 6, tlv.Length);
            var r = SendRaw(Wrap(0, 0, b));
            if (r == null || r.Length < 12) return null;
            return new QmiResponse { MsgId = (ushort)(r[8] | r[9] << 8), Payload = Slice(r, 12, r.Length - 12) };
        }

        private static byte[] Wrap(byte service, byte cl, byte[] body)
        {
            int fl = 6 + body.Length;
            ushort lf = (ushort)(fl - 1);
            byte[] q = new byte[fl];
            q[0] = 1;
            q[1] = (byte)(lf & 0xFF);
            q[2] = (byte)(lf >> 8);
            q[3] = 0;
            q[4] = service;
            q[5] = cl;
            System.Buffer.BlockCopy(body, 0, q, 6, body.Length);
            return q;
        }

        private byte[]? SendRaw(byte[] q)
        {
            for (int i = 0; i < 3; i++)
            {
                var r = SendOnce(q);
                if (r != null) return r;
                Thread.Sleep(300);
            }
            return null;
        }

        private byte[]? SendOnce(byte[] q)
        {
            if (_svc == null) return null;
            lock (QmiGate)
            {
            bool owned = _batchSession == null;
            var s = _batchSession ?? _svc.OpenCommandSession();
            try
            {
                IBuffer ib = CryptographicBuffer.CreateFromByteArray(q);
                var op = s.SendSetCommandAsync(1, ib);
                var mre = new ManualResetEventSlim();
                op.Completed = (o, st) => mre.Set();
                int sp = 60;
                while (op.Status == AsyncStatus.Started && sp-- > 0)
                {
                    if (mre.Wait(50)) break;
                }
                if (op.Status == AsyncStatus.Started && !mre.Wait(3000)) return null;
                if (op.Status != AsyncStatus.Completed) return null;
                var r = op.GetResults();
                if (r.StatusCode != 0 || r.ResponseData == null || r.ResponseData.Length == 0) return null;
                CryptographicBuffer.CopyToByteArray(r.ResponseData, out byte[] a);
                return a;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (owned) s.CloseSession();
            }
            }
        }

        // ---- TLV helpers ----
        public static byte[] Tlv(byte type, byte[] data)
        {
            byte[] r = new byte[3 + data.Length];
            r[0] = type;
            r[1] = (byte)(data.Length & 0xFF);
            r[2] = (byte)(data.Length >> 8);
            System.Buffer.BlockCopy(data, 0, r, 3, data.Length);
            return r;
        }

        public static Dictionary<byte, byte[]> Parse(byte[] t)
        {
            var d = new Dictionary<byte, byte[]>();
            int i = 0;
            while (i + 3 <= t.Length)
            {
                byte ty = t[i];
                int len = t[i + 1] | t[i + 2] << 8;
                if (i + 3 + len > t.Length) break;
                d[ty] = Slice(t, i + 3, len);
                i += 3 + len;
            }
            return d;
        }

        private static byte[] Slice(byte[] s, int o, int l)
        {
            byte[] r = new byte[l];
            System.Buffer.BlockCopy(s, o, r, 0, l);
            return r;
        }
    }
}
