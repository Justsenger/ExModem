using System.Collections.Generic;
using ExModem.Services;
using ExModem.Properties;

namespace ExModem.Core.Backends
{
    // Intel(Fibocom L860-GL / XMM7560 等)后端占位 —— 不认 QMI,需走 AT 指令或标准 MBIM。
    // 实现路线(拿到 L860-GL 实测后填):
    //   RAT 切换   : AT+XACT=...           (Intel 私有,LTE 模块无 5G)
    //   SIM 短信   : AT+CMGL/CMGR/CMGW/CMGD (标准 27.005)
    //   SIM 电话簿 : AT+CPBS="SM"; AT+CPBR/CPBW (标准 27.007)
    //   信号(中立): MBIM CID_SIGNAL_STATE
    // AT 通道可经 Windows 串口枚举或 MBIM AT-passthrough 获取。
    public sealed class IntelBackend : IModemBackend
    {
        public string Name => Resources.Backend_IntelName;

        public int GetModePref() => -1;
        public bool SetModePref(int modePref) => false;

        public (List<SimSmsRecord> sim, List<SimSmsRecord> device) ReadStoredSms()
            => (new List<SimSmsRecord>(), new List<SimSmsRecord>());
        public List<SimSmsRecord> ReadSimSms() => new List<SimSmsRecord>();
        public bool DeleteSms(byte storage, uint index) => false;
        public bool ClearSms(byte storage) => false;
        public int GetSmsCapacity() => 0;

        public (List<SimContact> list, int capacity) ReadSimContacts() => (new List<SimContact>(), 0);
        public (bool ok, string msg) AddSimContact(string name, string number) => (false, Resources.Backend_IntelTodo);
        public (bool ok, string msg) UpdateSimContact(int index, string name, string number) => (false, Resources.Backend_IntelTodo);
        public bool DeleteSimContact(int index) => false;
    }
}
