using System.Collections.Generic;
using ExModem.Services;

namespace ExModem.Core.Backends
{
    // Qualcomm(QMI 直通)实现:委托给现有的 QMI 服务。
    public sealed class QualcommBackend : IModemBackend
    {
        private readonly ModeService _mode = new();
        private readonly SimSmsService _sms = new();
        private readonly SimContactsService _pb = new();

        public string Name => "Qualcomm (QMI)";

        public int GetModePref() => _mode.GetModePref();
        public bool SetModePref(int modePref) => _mode.SetModePref(modePref);

        public (List<SimSmsRecord> sim, List<SimSmsRecord> device) ReadStoredSms() => _sms.ReadBoth();
        public List<SimSmsRecord> ReadSimSms() => _sms.ReadSim();
        public bool DeleteSms(byte storage, uint index) => _sms.Delete(storage, index);
        public bool ClearSms(byte storage) => _sms.DeleteAll(storage);
        public int GetSmsCapacity() => _sms.GetSimCapacity();

        public (List<SimContact> list, int capacity) ReadSimContacts() => _pb.ReadAll();
        public (bool ok, string msg) AddSimContact(string name, string number) => _pb.Add(name, number);
        public (bool ok, string msg) UpdateSimContact(int index, string name, string number) => _pb.Update(index, name, number);
        public bool DeleteSimContact(int index) => _pb.Delete(index);
    }
}
