using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExModem.Core.Backends;
using ExModem.Services;
using ExModem.Properties;

namespace ExModem.ViewModels
{
    public partial class StatusViewModel : ObservableObject
    {
        private readonly SignalService _signal = new();
        private readonly SwitchService _auto;
        private readonly DispatcherTimer _timer;

        [ObservableProperty] private string _adapter = "—";
        [ObservableProperty] private string _provider = "—";
        [ObservableProperty] private string _registerState = "—";
        [ObservableProperty] private string _dataClass = "—";
        [ObservableProperty] private string _signalPercent = "—";
        [ObservableProperty] private string _rssi = "—";
        [ObservableProperty] private string _modePrefText = Resources.Status_ModeLoading;
        [ObservableProperty] private string _actionStatus = "";
        [ObservableProperty] private bool _autoEnabled;
        [ObservableProperty] private bool _busy;
        [ObservableProperty] private bool _isOn5G;   // 当前 mode_pref 是否含 5G NR
        [ObservableProperty] private int _signalLevel;   // 0-4 信号格
        // 已设模式 → 按"代"点亮的能力徽章(mode_pref 各位)
        [ObservableProperty] private bool _allow2G;
        [ObservableProperty] private bool _allow3G;
        [ObservableProperty] private bool _allow4G;
        [ObservableProperty] private bool _allow5G;
        [ObservableProperty] private string _imei = "—";
        [ObservableProperty] private string _iccid = "—";
        [ObservableProperty] private string _imsi = "—";
        [ObservableProperty] private string _firmware = "—";
        [ObservableProperty] private string _smsc = "—";
        [ObservableProperty] private string _manufacturer = "—";
        [ObservableProperty] private string _model = "—";
        [ObservableProperty] private bool _dataEnabled = true;   // 蜂窝数据开关

        // 当前模式高亮:哪个模式生效,哪个按钮就是 Primary
        public Wpf.Ui.Controls.ControlAppearance LteAppearance
            => IsOn5G ? Wpf.Ui.Controls.ControlAppearance.Secondary : Wpf.Ui.Controls.ControlAppearance.Primary;
        public Wpf.Ui.Controls.ControlAppearance Fast5GAppearance
            => IsOn5G ? Wpf.Ui.Controls.ControlAppearance.Primary : Wpf.Ui.Controls.ControlAppearance.Secondary;
        public string CurrentModeText
            => IsOn5G ? Resources.Status_ModeHint5G : Resources.Status_ModeHintLte;

        // 分段开关:4G 段在非 5G 时点亮
        public bool IsOn4G => !IsOn5G;

        partial void OnIsOn5GChanged(bool value)
        {
            OnPropertyChanged(nameof(LteAppearance));
            OnPropertyChanged(nameof(Fast5GAppearance));
            OnPropertyChanged(nameof(CurrentModeText));
            OnPropertyChanged(nameof(IsOn4G));
        }

        public StatusViewModel()
        {
            _auto = new SwitchService(m => SafeSet(() => ActionStatus = m));
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => RefreshFast();
            AutoEnabled = SettingsService.GetAutoSwitch();   // 恢复上次的自动切换开关(会触发 OnAutoEnabledChanged 启动)
            _ = RefreshAllAsync();
        }

        // 仅在状态页可见时轮询,避免后台每秒空转
        public void StartPolling()
        {
            RefreshFast();
            _ = RefreshDataEnabledAsync();   // 数据开关状态不常变,进页读一次即可,不放进每秒轮询
            _timer.Start();
        }

        public void StopPolling() => _timer.Stop();

        // 蜂窝数据开关:进页/切换后回读真值;用 _suppressData 避免回读触发命令形成回路
        private bool _suppressData;
        private async Task RefreshDataEnabledAsync()
        {
            var v = await Task.Run(() => _signal.GetDataEnabled());   // netsh 放后台
            if (v.HasValue) { _suppressData = true; DataEnabled = v.Value; _suppressData = false; }
        }

        partial void OnDataEnabledChanged(bool value)
        {
            if (_suppressData) return;
            _ = ApplyDataAsync(value);
        }

        private async Task ApplyDataAsync(bool on)
        {
            ActionStatus = on ? Resources.Status_DataEnabling : Resources.Status_DataDisabling;
            await Task.Run(() => _signal.SetDataEnabled(on));
            await Task.Delay(700);
            await RefreshDataEnabledAsync();   // 回读确认(失败则开关自动弹回真实状态)
            ActionStatus = DataEnabled ? Resources.Status_DataEnabled : Resources.Status_DataDisabled;
        }

        private static void SafeSet(Action a)
        {
            var d = Application.Current?.Dispatcher;
            if (d == null || d.CheckAccess()) a();
            else d.Invoke(a);
        }

        private async void RefreshFast()
        {
            // netsh(spawn 进程,数十~上百 ms)放后台线程,别卡 UI(否则切到本页就顿一下)
            CellStatus st;
            try { st = await Task.Run(() => _signal.Read(false)); }
            catch { return; }
            Adapter = st.Adapter;
            Provider = st.Provider;
            RegisterState = st.RegisterState;
            DataClass = st.DataClass;
            // 5G 下 netsh 给 0%/-113 哨兵(NR 不进 RSSI 字段),此时别覆盖——由 3s 后台 QMI 读填真值
            bool nrSentinel = (st.DataClass != null && st.DataClass.Contains("5G"))
                              && (st.SignalPercent == "0%" || st.SignalPercent == "—");
            if (!nrSentinel)
            {
                SignalPercent = st.SignalPercent;
                SignalLevel = st.SignalLevel;
                Rssi = st.Rssi;
            }
            Imei = st.Imei;
            Iccid = st.Iccid;
            Imsi = st.Imsi;
            Firmware = st.Firmware;
            Smsc = st.Smsc;
            Manufacturer = st.Manufacturer;
            Model = st.Model;

            // 滑块跟随真实模式:每 3 秒读一次 mode_pref(QMI),自动切换在后台改了模式时滑块也会动。
            // 切换中(Busy)不读,避免温和重选途中(临时 0x10)把滑块抖到 4G。
            if (!Busy && (++_tick % 3 == 0)) _ = RefreshModePrefAsync();
        }

        private int _tick;
        private async Task RefreshModePrefAsync()
        {
            int mp = await Task.Run(() => Modem.Current.GetModePref());
            if (mp >= 0 && !Busy) IsOn5G = (mp & 0x40) != 0;

            // 5G 下用 QMI 读 NR 真实信号(netsh 读不到),覆盖被 RefreshFast 跳过的哨兵值
            if (DataClass != null && DataClass.Contains("5G"))
            {
                var nr = await Task.Run(() => _signal.ReadNrSignalViaQmi());
                if (nr != null)
                {
                    Rssi = nr.Value.dbm + " dBm";
                    SignalPercent = nr.Value.pct + "%";
                    SignalLevel = nr.Value.level;
                }
            }
        }

        [RelayCommand]
        private async Task RefreshAll() => await RefreshAllAsync();

        private async Task RefreshAllAsync()
        {
            RefreshFast();
            int mp = await Task.Run(() => Modem.Current.GetModePref());
            ModePrefText = mp < 0 ? Resources.Common_LoadFailed : ModeService.Describe(mp);
            if (mp >= 0)
            {
                IsOn5G = (mp & 0x40) != 0;
                Allow2G = (mp & 0x04) != 0;
                Allow3G = (mp & (0x08 | 0x20)) != 0;
                Allow4G = (mp & 0x10) != 0;
                Allow5G = (mp & 0x40) != 0;
            }
        }

        [RelayCommand]
        private async Task LockLte()
        {
            AutoEnabled = false;
            await DoSwitch(0x3C, Resources.Status_SmsModeLte);
        }

        [RelayCommand]
        private async Task Fast5G()
        {
            AutoEnabled = false;
            Busy = true;
            ActionStatus = Resources.Status_Switching5G;

            // 关键修复:NR 只有在“刚完成一次干净的 LTE 注册”后才能稳定附着。
            // 直接放开 NR 常脱网(0%/-113dBm,且一直卡着)。故温和重选:
            //   1) 先强制纯 LTE(0x10,必与当前不同→触发重选)
            //   2) 轮询等 LTE 注册成功
            //   3) 再开 NR(0x7C=全制式含 5G)
            await Task.Run(() => Modem.Current.SetModePref(0x10));

            for (int i = 0; i < 16; i++)   // 最多 ~8s
            {
                var st = await Task.Run(() => _signal.Read(false));
                Provider = st.Provider; RegisterState = st.RegisterState; DataClass = st.DataClass;
                SignalPercent = st.SignalPercent; SignalLevel = st.SignalLevel; Rssi = st.Rssi;
                // 注:字符串耦合 SignalService.Reg()/DataClassFriendly() 的输出
                if (st.RegisterState == Resources.Reg_Home && st.DataClass == "4G") break;
                await Task.Delay(500);
            }
            await Task.Delay(800);   // 让 LTE 再稳一下

            bool ok = await Task.Run(() => Modem.Current.SetModePref(0x7C));
            ActionStatus = ok ? Resources.Status_Switched5G : Resources.Status_Switch5GFailed;
            if (ok) IsOn5G = true;
            await RefreshAllAsync();
            Busy = false;
        }

        private async Task DoSwitch(int modePref, string label)
        {
            Busy = true;
            ActionStatus = string.Format(Resources.Status_SwitchingTo, label);
            bool ok = await Task.Run(() => Modem.Current.SetModePref(modePref));
            ActionStatus = ok ? string.Format(Resources.Status_SwitchedTo, label) : Resources.Status_SwitchFailed;
            if (ok) IsOn5G = (modePref & 0x40) != 0;
            await RefreshAllAsync();
            Busy = false;
        }

        partial void OnAutoEnabledChanged(bool value)
        {
            SettingsService.SetAutoSwitch(value);   // 持久化到 config
            if (value)
            {
                _auto.Start();
                ActionStatus = Resources.Status_AutoOn;
            }
            else
            {
                _auto.Stop();
                ActionStatus = Resources.Status_AutoOff;
            }
        }

        [RelayCommand]
        private void Copy()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Resources.Status_CopyTitle);
            sb.AppendLine($"{Resources.Status_Copy_Adapter}: {Adapter}");
            sb.AppendLine($"{Resources.Status_Carrier}: {Provider}");
            sb.AppendLine($"{Resources.Status_RegState}: {RegisterState}");
            sb.AppendLine($"{Resources.Status_Copy_DataClass}: {DataClass}");
            sb.AppendLine($"{Resources.Status_Signal}: {SignalPercent}    RSSI: {Rssi}");
            sb.AppendLine($"{Resources.Status_Copy_ModePref}: {ModePrefText}");
            sb.AppendLine($"IMEI: {Imei}");
            sb.AppendLine($"ICCID: {Iccid}");
            sb.AppendLine($"IMSI: {Imsi}");
            sb.AppendLine($"{Resources.Status_SmsCenter} (SMSC): {Smsc}");
            sb.AppendLine($"{Resources.Status_Firmware}: {Firmware}");
            try { Clipboard.SetText(sb.ToString()); ActionStatus = Resources.Status_Copied; }
            catch { ActionStatus = Resources.Status_CopyFailed; }
        }
    }
}
