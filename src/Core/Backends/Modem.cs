namespace ExModem.Core.Backends
{
    // 当前调制解调器后端的统一入口(厂商相关能力)。
    // 现固定 Qualcomm;将来按检测结果或设置切换到 Intel。
    public static class Modem
    {
        public static IModemBackend Current { get; private set; } = Detect();

        private static IModemBackend Detect()
        {
            // TODO: 真正检测厂商 —— 例如读 modem 制造商/型号(MobileBroadbandModem.DeviceInformation),
            //       或试探 QMI 直通通道是否可用;不可用则回退 IntelBackend。
            // 暂时:默认 Qualcomm(本机 Surface SDX65)。
            return new QualcommBackend();
        }

        public static void Use(IModemBackend backend) => Current = backend;
    }
}
