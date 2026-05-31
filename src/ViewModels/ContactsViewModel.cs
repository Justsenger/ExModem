using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExModem.Core.Backends;
using ExModem.Services;
using ExModem.Properties;

namespace ExModem.ViewModels
{
    public partial class ContactsViewModel : ObservableObject
    {
        private readonly ContactsService _svc = ContactsService.Shared;
        private readonly List<ContactItem> _sys = new();          // 系统联系人
        private readonly List<ContactItem> _simContacts = new();  // SIM 卡联系人(独立缓存,避免每次都重读)

        public ObservableCollection<ContactItem> Contacts { get; } = new();
        public ICollectionView ContactsView { get; }

        [ObservableProperty] private ContactItem? _selectedContact;
        [ObservableProperty] private string _searchText = "";

        [ObservableProperty] private bool _editorActive;     // 右侧编辑面板是否显示内容
        [ObservableProperty] private string _editId = "";
        [ObservableProperty] private string _editName = "";
        [ObservableProperty] private string _editNumber = "";
        [ObservableProperty] private string _editPhotoPath = "";
        [ObservableProperty] private int _saveTargetIndex;   // 0=本机 1=SIM 卡(新建时下拉选择)
        [ObservableProperty] private bool _editingSim;       // 选中的是 SIM 联系人
        private bool _photoPicked;                            // 本次是否换过头像
        private int _editSimIndex;                            // 编辑 SIM 联系人时的记录号
        [ObservableProperty] private string _status = "";
        [ObservableProperty] private bool _busy;
        [ObservableProperty] private int _simUsed;
        [ObservableProperty] private int _simCapacity;
        [ObservableProperty] private bool _simLoading;

        public bool IsNew => EditorActive && string.IsNullOrEmpty(EditId) && !EditingSim;
        public bool CanSave => EditorActive;   // 本机/SIM 都可编辑保存
        public bool CanDelete => EditorActive && !IsNew;   // 已有联系人才可删除
        public bool HasEditPhoto => !string.IsNullOrEmpty(EditPhotoPath);   // 有照片时不显示绿底占位
        public bool ShowHomeChip => EditorActive && !IsNew && !EditingSim;   // 本机联系人 chip
        // 仅当联系人存在本机(非 SIM、且不是"新建→保存到SIM")时可选头像
        public bool CanPickPhoto => EditorActive && !EditingSim && !(IsNew && SaveTargetIndex == 1);
        public string EditTitle => IsNew ? Resources.Contacts_TitleAdd : Resources.Contacts_TitleEdit;   // SIM/本机 由 chip 区分,标题统一
        public string SimCapacityText => SimLoading
            ? Resources.Contacts_SimSyncing
            : $"SIM : {SimUsed} / {(SimCapacity > 0 ? SimCapacity : 250)}";
        public string CountText => string.Format(Resources.Contacts_CountText, Contacts.Count);

        partial void OnEditorActiveChanged(bool v) => RaiseEditor();
        partial void OnEditIdChanged(string v) => RaiseEditor();
        partial void OnEditingSimChanged(bool v) => RaiseEditor();
        private void RaiseEditor()
        {
            OnPropertyChanged(nameof(IsNew));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(EditTitle));
            OnPropertyChanged(nameof(ShowHomeChip));
            OnPropertyChanged(nameof(CanPickPhoto));
            OnPropertyChanged(nameof(CanDelete));
        }
        partial void OnSaveTargetIndexChanged(int v) => OnPropertyChanged(nameof(CanPickPhoto));
        partial void OnEditPhotoPathChanged(string v) => OnPropertyChanged(nameof(HasEditPhoto));
        partial void OnSimUsedChanged(int v) => OnPropertyChanged(nameof(SimCapacityText));
        partial void OnSimCapacityChanged(int v) => OnPropertyChanged(nameof(SimCapacityText));
        partial void OnSimLoadingChanged(bool v) => OnPropertyChanged(nameof(SimCapacityText));
        partial void OnSearchTextChanged(string v) => ApplyFilter();

        public ContactsViewModel()
        {
            var cvs = new CollectionViewSource { Source = Contacts };
            cvs.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ContactItem.GroupKey)));
            ContactsView = cvs.View;
            _ = LoadAsync();
        }

        partial void OnSelectedContactChanged(ContactItem? c)
        {
            if (c == null) return;
            EditingSim = c.IsSim;
            EditId = c.IsSim ? "" : c.Id;
            _editSimIndex = c.SimIndex;
            EditName = c.Name;
            EditNumber = c.Number;
            EditPhotoPath = c.PhotoPath;
            SaveTargetIndex = 0;
            _photoPicked = false;
            EditorActive = true;
        }

        [RelayCommand]
        private void New()
        {
            SelectedContact = null;
            EditingSim = false; EditId = ""; EditName = ""; EditNumber = ""; EditPhotoPath = ""; SaveTargetIndex = 0;
            _photoPicked = false;
            EditorActive = true;
        }

        // 从外部(短信"存为联系人")带号码新建
        public void NewWithNumber(string number)
        {
            SelectedContact = null;
            EditingSim = false; EditId = ""; EditName = ""; EditNumber = number ?? ""; EditPhotoPath = ""; SaveTargetIndex = 0;
            _photoPicked = false;
            EditorActive = true;
        }

        [RelayCommand]
        private void PickPhoto()
        {
            if (!CanPickPhoto) return;
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = Resources.Contacts_ImageFilter + "|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                Title = Resources.Contacts_ChooseAvatar,
            };
            if (dlg.ShowDialog() == true) { EditPhotoPath = dlg.FileName; _photoPicked = true; }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (!CanSave) return;
            if (string.IsNullOrWhiteSpace(EditNumber)) { Status = Resources.Contacts_NeedNumber; return; }
            Busy = true;
            try
            {
                // 仅在本次换过头像时才写头像,避免重复处理旧头像
                string? photo = (_photoPicked && !string.IsNullOrEmpty(EditPhotoPath)) ? EditPhotoPath : null;
                bool savedSim;
                if (IsNew && SaveTargetIndex == 1)
                {
                    savedSim = true;
                    var (ok, msg) = await Task.Run(() => Modem.Current.AddSimContact(EditName, EditNumber));
                    Status = msg;
                    if (!ok) return;
                }
                else if (IsNew)
                {
                    savedSim = false;
                    await _svc.SaveNewAsync(EditName, EditNumber, photo);
                    Status = Resources.Contacts_Saved;
                }
                else if (EditingSim)
                {
                    savedSim = true;
                    int idx = _editSimIndex;
                    var (ok, msg) = await Task.Run(() => Modem.Current.UpdateSimContact(idx, EditName, EditNumber));
                    Status = msg;
                    if (!ok) return;
                }
                else
                {
                    savedSim = false;
                    await _svc.UpdateAsync(EditId, SelectedContact?.Native, EditName, EditNumber, photo);
                    Status = Resources.Contacts_Saved;
                }
                // 保存成功后:只刷新对应来源,并停留在该联系人(不置空)
                if (savedSim)
                {
                    await LoadSimAsync();
                    SelectedContact = Contacts.FirstOrDefault(x => x.IsSim && x.Number == EditNumber.Trim())
                                      ?? Contacts.FirstOrDefault(x => x.IsSim && x.SimIndex == _editSimIndex);
                }
                else
                {
                    string keepId = EditId;
                    await LoadSystemAsync();
                    SelectedContact = (!string.IsNullOrEmpty(keepId)
                        ? Contacts.FirstOrDefault(x => !x.IsSim && x.Id == keepId) : null)
                        ?? Contacts.FirstOrDefault(x => !x.IsSim && x.Name == EditName && x.Number == EditNumber.Trim());
                }
                ContactsService.NotifyChanged();   // 通知短信页刷新名字/头像
            }
            catch (Exception e)
            {
                Status = string.Format(Resources.Contacts_SaveFailed, e.GetType().Name + " " + e.Message + " 0x" + e.HResult.ToString("X8"));
            }
            finally { Busy = false; }
        }

        [RelayCommand]
        private async Task Delete(ContactItem? c)
        {
            c ??= SelectedContact;
            if (c == null) return;
            Busy = true;
            try
            {
                if (c.IsSim)
                {
                    bool ok = await Task.Run(() => Modem.Current.DeleteSimContact(c.SimIndex));
                    Status = ok ? Resources.Contacts_SimDeleted : Resources.Contacts_SimDeleteFailed;
                    EditorActive = false;
                    await LoadSimAsync();
                }
                else
                {
                    await _svc.DeleteAsync(c.Native);
                    EditorActive = false;
                    await LoadSystemAsync();
                }
                ContactsService.NotifyChanged();   // 通知短信页刷新名字/头像
            }
            catch (Exception e) { Status = string.Format(Resources.Chat_DeleteFailed, e.Message.Split('\n')[0]); }
            finally { Busy = false; }
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            await LoadSystemAsync();         // 系统联系人:快,先渲染
            _ = LoadSimAsync();              // SIM:250 次 READ_RECORD 慢(~3s),后台读
        }

        // 仅刷新系统联系人(不动 SIM,避免无谓的 3 秒 SIM 重读)
        private async Task LoadSystemAsync()
        {
            Busy = true;
            try
            {
                var sys = await _svc.GetAllAsync();
                _sys.Clear();
                _sys.AddRange(sys);
                ApplyFilter();
            }
            catch (Exception e) { Status = string.Format(Resources.Contacts_ReadFailed, e.Message.Split('\n')[0]); }
            finally { Busy = false; }
        }

        // 仅刷新 SIM 卡联系人
        private async Task LoadSimAsync()
        {
            SimLoading = true;
            try
            {
                var (simList, cap) = await Task.Run(() => Modem.Current.ReadSimContacts());
                SimCapacity = cap;
                _simContacts.Clear();
                foreach (var s in simList)
                    _simContacts.Add(new ContactItem { Name = s.Name, Number = s.Number, IsSim = true, SimIndex = s.Index });
                SimUsed = _simContacts.Count;
                ApplyFilter();
            }
            catch { }
            finally { SimLoading = false; }
        }

        private void ApplyFilter()
        {
            string q = (SearchText ?? "").Trim();
            IEnumerable<ContactItem> src = _sys.Concat(_simContacts);
            if (q.Length > 0)
                src = src.Where(x =>
                    (x.Name ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                    || (x.Number ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            // 按拼音首字母排序(# 排到最后)
            var ordered = src
                .OrderBy(x => x.GroupKey == "#" ? "[" : x.GroupKey, StringComparer.Ordinal)
                .ThenBy(x => x.Name, StringComparer.CurrentCulture);
            Contacts.Clear();
            foreach (var c in ordered) Contacts.Add(c);
            OnPropertyChanged(nameof(CountText));
        }
    }
}
