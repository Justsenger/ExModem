using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ExModem.Services
{
    public sealed class ContactItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Number { get; set; } = "";
        public string PhotoPath { get; set; } = "";   // 本地缓存的头像 png 路径(供 WPF 显示),空=无
        public bool IsSim { get; set; }               // 来自 SIM 卡电话簿(EF_ADN)
        public int SimIndex { get; set; }             // EF_ADN 记录号
        public object? Native { get; set; }           // 原生 Windows.ApplicationModel.Contacts.Contact(用于可靠改/删)

        public string Initial
        {
            get
            {
                string s = string.IsNullOrWhiteSpace(Name) ? Number.TrimStart('+') : Name;
                return s.Length > 0 ? s.Substring(0, 1) : "?";
            }
        }

        // A-Z / # 分组键(中文转拼音首字母)
        public string GroupKey => ExModem.Tools.Pinyin.Initial(Name).ToString();
    }

    // 联系人 = 存进系统 Unistore(ContactStore)的「ExModem」联系人库。免打包可读写(已验证)。
    // 坑:读写 store 的 FindContactListsAsync 有 bug 返回 0;改用 AllContactsReadOnly 枚举找到库再用 id 取可写句柄。
    public sealed class ContactsService
    {
        // 全应用共享一个实例(共用 store/list 句柄缓存,避免各页面各开一份)
        public static ContactsService Shared { get; } = new();

        // 联系人增删改后广播,供其它页面(如短信页)刷新名字/头像
        public static event Action? Changed;
        public static void NotifyChanged() => Changed?.Invoke();

        private const string ListName = "ExModem";
        private ContactStore? _rw;
        private ContactList? _list;

        private async Task EnsureAsync()
        {
            if (_list != null) return;
            _rw = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
            var all = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AllContactsReadOnly);
            var lists = await all.FindContactListsAsync();
            var mine = lists.FirstOrDefault(l => l.DisplayName == ListName);
            _list = mine != null ? await _rw.GetContactListAsync(mine.Id) : await _rw.CreateContactListAsync(ListName);
        }

        public async Task<List<ContactItem>> GetAllAsync()
        {
            await EnsureAsync();
            var res = new List<ContactItem>();
            // 从可写的 _list 读取(联系人对象可直接改/存),而非 store 级聚合只读视图
            var reader = _list!.GetContactReader();
            var batch = await reader.ReadBatchAsync();
            while (batch.Contacts.Count > 0)
            {
                foreach (var c in batch.Contacts) res.Add(await ToItem(c));
                batch = await reader.ReadBatchAsync();
            }
            return res.OrderBy(x => x.Name, StringComparer.CurrentCulture).ToList();
        }

        private async Task ApplyAndSave(Contact c, string name, string number, string? sourceImagePath)
        {
            c.FirstName = name ?? "";
            c.LastName = "";
            c.Phones.Clear();
            if (!string.IsNullOrWhiteSpace(number))
                c.Phones.Add(new ContactPhone { Number = number.Trim(), Kind = ContactPhoneKind.Mobile });
            if (!string.IsNullOrEmpty(sourceImagePath) && File.Exists(sourceImagePath))
            {
                var f = await StorageFile.GetFileFromPathAsync(sourceImagePath);
                c.SourceDisplayPicture = RandomAccessStreamReference.CreateFromFile(f);
            }
            await _list!.SaveContactAsync(c);
        }

        // 新建联系人
        public async Task SaveNewAsync(string name, string number, string? sourceImagePath)
        {
            await EnsureAsync();
            await ApplyAndSave(new Contact(), name, number, sourceImagePath);
        }

        // 更新已有联系人:优先用原生对象;为空则用 id 重新取。绝不在更新路径里新建,避免副本。
        public async Task UpdateAsync(string id, object? native, string name, string number, string? sourceImagePath)
        {
            await EnsureAsync();
            Contact? c = native as Contact;
            if (c == null && !string.IsNullOrEmpty(id))
                c = await _rw!.GetContactAsync(id);
            if (c == null) return;   // 拿不到现有联系人,宁可不存也不新建副本
            await ApplyAndSave(c, name, number, sourceImagePath);
        }

        public async Task DeleteAsync(object? native)
        {
            await EnsureAsync();
            if (native is Contact c) await _list!.DeleteContactAsync(c);
        }

        // 号码 -> 联系人(名字+头像),匹配不到返回 null
        public async Task<ContactItem?> ResolveAsync(string number)
        {
            try
            {
                await EnsureAsync();
                foreach (var c in await _rw!.FindContactsAsync())
                    foreach (var p in c.Phones)
                        if (SameNumber(p.Number, number))
                            return await ToItem(c);
            }
            catch { }
            return null;
        }

        private async Task<ContactItem> ToItem(Contact c) => new ContactItem
        {
            Id = c.Id,
            Name = c.DisplayName,
            Number = c.Phones.Count > 0 ? c.Phones[0].Number : "",
            Native = c,
            PhotoPath = await CachePhoto(c),
        };

        private static async Task<string> CachePhoto(Contact c)
        {
            try
            {
                // 优先用源图(较清晰),拿不到再退回小缩略图(SmallDisplayPicture 是低清缩略图)
                var pic = c.SourceDisplayPicture ?? c.SmallDisplayPicture;
                if (pic == null) return "";
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ExModem", "avatars");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, c.Id.Replace(',', '_').Replace(':', '_') + ".png");
                using var srcRef = await pic.OpenReadAsync();
                using var src = srcRef.AsStreamForRead();
                using var fs = File.Create(path);
                await src.CopyToAsync(fs);
                return path;
            }
            catch { return ""; }
        }

        private static string Digits(string s) => new string((s ?? "").Where(char.IsDigit).ToArray());

        public static bool SameNumber(string a, string b)
        {
            string da = Digits(a), db = Digits(b);
            if (da.Length == 0 || db.Length == 0) return false;
            if (da == db) return true;
            string ta = da.Length > 8 ? da.Substring(da.Length - 8) : da;   // 末8位,容忍 +86 前缀差异
            string tb = db.Length > 8 ? db.Substring(db.Length - 8) : db;
            return ta == tb;
        }
    }
}
