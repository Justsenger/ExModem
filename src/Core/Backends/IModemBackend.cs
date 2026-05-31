using System.Collections.Generic;
using ExModem.Services;

namespace ExModem.Core.Backends
{
    // 厂商特有的调制解调器能力面(中立数据类型 SimSmsRecord/SimContact 复用 Services 里的)。
    //   Qualcomm  -> QMI 直通实现(QualcommBackend)
    //   Intel(如 Fibocom L860-GL / XMM7560)-> AT 指令 / 标准 MBIM(IntelBackend,待实测实现)
    // 注:系统短信收发(ChatMessageStore/SmsDevice2)、系统联系人(ContactStore)是厂商中立的,
    //     不在本接口内 —— 它们在 Intel 上本就能用,无需后端区分。
    public interface IModemBackend
    {
        string Name { get; }

        // ---- 网络模式 / RAT(mode_pref 位:GSM04 UMTS08 LTE10 TDSCDMA20 NR5G40)----
        int GetModePref();                 // 当前位,失败 -1
        bool SetModePref(int modePref);

        // ---- 物理存储短信(SIM=0 / 调制解调器=1)----
        (List<SimSmsRecord> sim, List<SimSmsRecord> device) ReadStoredSms();
        List<SimSmsRecord> ReadSimSms();
        bool DeleteSms(byte storage, uint index);
        bool ClearSms(byte storage);
        int GetSmsCapacity();              // SIM 短信容量

        // ---- SIM 电话簿(EF_ADN)----
        (List<SimContact> list, int capacity) ReadSimContacts();
        (bool ok, string msg) AddSimContact(string name, string number);
        (bool ok, string msg) UpdateSimContact(int index, string name, string number);
        bool DeleteSimContact(int index);
    }
}
