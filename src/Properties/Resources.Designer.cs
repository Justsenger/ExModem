//------------------------------------------------------------------------------
// 由 ExModem辅助工具/gen_i18n.py 从 i18n对照表.md 生成,请勿手改;改译文请改 .md 后重跑脚本。
//------------------------------------------------------------------------------
namespace ExModem.Properties
{
    using System;

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("ExModem.gen_i18n", "1.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources
    {
        private static global::System.Resources.ResourceManager resourceMan;
        private static global::System.Globalization.CultureInfo resourceCulture;

        internal Resources() { }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager
        {
            get
            {
                if (object.ReferenceEquals(resourceMan, null))
                {
                    resourceMan = new global::System.Resources.ResourceManager("ExModem.Properties.Resources", typeof(Resources).Assembly);
                }
                return resourceMan;
            }
        }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture
        {
            get { return resourceCulture; }
            set { resourceCulture = value; }
        }

        private static string Get(string name) => ResourceManager.GetString(name, resourceCulture);

        public static string Common_Delete => Get("Common_Delete");
        public static string Common_Save => Get("Common_Save");
        public static string Common_Copy => Get("Common_Copy");
        public static string Common_Send => Get("Common_Send");
        public static string Common_Phone => Get("Common_Phone");
        public static string Common_SimCard => Get("Common_SimCard");
        public static string Common_Sim => Get("Common_Sim");
        public static string Common_Device => Get("Common_Device");
        public static string Common_Unknown => Get("Common_Unknown");
        public static string Common_LoadFailed => Get("Common_LoadFailed");
        public static string Nav_Network => Get("Nav_Network");
        public static string Nav_Contacts => Get("Nav_Contacts");
        public static string Chat => Get("Chat");
        public static string ChatDesc => Get("ChatDesc");
        public static string Settings => Get("Settings");
        public static string Tray_Show => Get("Tray_Show");
        public static string Tray_Exit => Get("Tray_Exit");
        public static string Status_BannerTitle => Get("Status_BannerTitle");
        public static string Status_DeviceInfo => Get("Status_DeviceInfo");
        public static string Status_CurrentNetwork => Get("Status_CurrentNetwork");
        public static string Status_Carrier => Get("Status_Carrier");
        public static string Status_RegState => Get("Status_RegState");
        public static string Status_SignalStrength => Get("Status_SignalStrength");
        public static string Status_SmsCenter => Get("Status_SmsCenter");
        public static string Status_Signal => Get("Status_Signal");
        public static string Status_Manufacturer => Get("Status_Manufacturer");
        public static string Status_Model => Get("Status_Model");
        public static string Status_Firmware => Get("Status_Firmware");
        public static string Status_BasicSettings => Get("Status_BasicSettings");
        public static string Status_DataSwitch => Get("Status_DataSwitch");
        public static string Status_DataSwitchDesc => Get("Status_DataSwitchDesc");
        public static string Status_NetworkMode => Get("Status_NetworkMode");
        public static string Status_NetworkModeDesc => Get("Status_NetworkModeDesc");
        public static string Status_AutoSwitch => Get("Status_AutoSwitch");
        public static string Status_AutoSwitchDesc => Get("Status_AutoSwitchDesc");
        public static string Status_ModeLoading => Get("Status_ModeLoading");
        public static string Status_ModeHint5G => Get("Status_ModeHint5G");
        public static string Status_ModeHintLte => Get("Status_ModeHintLte");
        public static string Status_DataEnabling => Get("Status_DataEnabling");
        public static string Status_DataDisabling => Get("Status_DataDisabling");
        public static string Status_DataEnabled => Get("Status_DataEnabled");
        public static string Status_DataDisabled => Get("Status_DataDisabled");
        public static string Status_SmsModeLte => Get("Status_SmsModeLte");
        public static string Status_Switching5G => Get("Status_Switching5G");
        public static string Status_Switched5G => Get("Status_Switched5G");
        public static string Status_Switch5GFailed => Get("Status_Switch5GFailed");
        public static string Status_SwitchingTo => Get("Status_SwitchingTo");
        public static string Status_SwitchedTo => Get("Status_SwitchedTo");
        public static string Status_SwitchFailed => Get("Status_SwitchFailed");
        public static string Status_AutoOn => Get("Status_AutoOn");
        public static string Status_AutoOff => Get("Status_AutoOff");
        public static string Status_CopyTitle => Get("Status_CopyTitle");
        public static string Status_Copied => Get("Status_Copied");
        public static string Status_CopyFailed => Get("Status_CopyFailed");
        public static string Status_Copy_Adapter => Get("Status_Copy_Adapter");
        public static string Status_Copy_DataClass => Get("Status_Copy_DataClass");
        public static string Status_Copy_ModePref => Get("Status_Copy_ModePref");
        public static string Reg_NotRegistered => Get("Reg_NotRegistered");
        public static string Reg_Searching => Get("Reg_Searching");
        public static string Reg_Home => Get("Reg_Home");
        public static string Reg_Roaming => Get("Reg_Roaming");
        public static string Reg_Partner => Get("Reg_Partner");
        public static string Reg_Denied => Get("Reg_Denied");
        public static string Signal_None => Get("Signal_None");
        public static string Carrier_ChinaMobile => Get("Carrier_ChinaMobile");
        public static string Carrier_ChinaUnicom => Get("Carrier_ChinaUnicom");
        public static string Carrier_ChinaTelecom => Get("Carrier_ChinaTelecom");
        public static string Carrier_ChinaBroadnet => Get("Carrier_ChinaBroadnet");
        public static string Chat_NewConversation => Get("Chat_NewConversation");
        public static string Chat_SearchPlaceholder => Get("Chat_SearchPlaceholder");
        public static string Chat_DeleteConversation => Get("Chat_DeleteConversation");
        public static string Chat_CopyNumberTip => Get("Chat_CopyNumberTip");
        public static string Chat_RecipientPlaceholder => Get("Chat_RecipientPlaceholder");
        public static string Chat_SaveAsContact => Get("Chat_SaveAsContact");
        public static string Chat_InputPlaceholder => Get("Chat_InputPlaceholder");
        public static string Chat_Status_ConvoDeleted => Get("Chat_Status_ConvoDeleted");
        public static string Chat_Status_NewConvo => Get("Chat_Status_NewConvo");
        public static string Chat_Status_NeedNumber => Get("Chat_Status_NeedNumber");
        public static string Chat_Status_NeedText => Get("Chat_Status_NeedText");
        public static string Chat_Toast_SendFailedLte => Get("Chat_Toast_SendFailedLte");
        public static string Chat_Toast_SendError => Get("Chat_Toast_SendError");
        public static string Chat_Status_SimDeleted => Get("Chat_Status_SimDeleted");
        public static string Chat_Status_SimDeleteFailed => Get("Chat_Status_SimDeleteFailed");
        public static string Chat_Status_MsgDeleted => Get("Chat_Status_MsgDeleted");
        public static string Chat_Status_Synced => Get("Chat_Status_Synced");
        public static string Chat_Status_SyncFailed => Get("Chat_Status_SyncFailed");
        public static string Chat_DeleteFailed => Get("Chat_DeleteFailed");
        public static string Msg_StatusFailed => Get("Msg_StatusFailed");
        public static string Msg_StatusSending => Get("Msg_StatusSending");
        public static string Contacts_Title => Get("Contacts_Title");
        public static string Contacts_AddTip => Get("Contacts_AddTip");
        public static string Contacts_SearchPlaceholder => Get("Contacts_SearchPlaceholder");
        public static string Contacts_EmptyHint => Get("Contacts_EmptyHint");
        public static string Contacts_Name => Get("Contacts_Name");
        public static string Contacts_PhoneNumber => Get("Contacts_PhoneNumber");
        public static string Contacts_SaveTo => Get("Contacts_SaveTo");
        public static string Contacts_TitleAdd => Get("Contacts_TitleAdd");
        public static string Contacts_TitleEdit => Get("Contacts_TitleEdit");
        public static string Contacts_SimSyncing => Get("Contacts_SimSyncing");
        public static string Contacts_CountText => Get("Contacts_CountText");
        public static string Contacts_ChooseAvatar => Get("Contacts_ChooseAvatar");
        public static string Contacts_ImageFilter => Get("Contacts_ImageFilter");
        public static string Contacts_NeedNumber => Get("Contacts_NeedNumber");
        public static string Contacts_Saved => Get("Contacts_Saved");
        public static string Contacts_SaveFailed => Get("Contacts_SaveFailed");
        public static string Contacts_SimDeleted => Get("Contacts_SimDeleted");
        public static string Contacts_SimDeleteFailed => Get("Contacts_SimDeleteFailed");
        public static string Contacts_ReadFailed => Get("Contacts_ReadFailed");
        public static string theme => Get("theme");
        public static string switchtheme => Get("switchtheme");
        public static string language => Get("language");
        public static string reboot => Get("reboot");
        public static string about => Get("about");
        public static string light => Get("light");
        public static string dark => Get("dark");
        public static string report => Get("report");
        public static string github => Get("github");
        public static string Toast_NewSms => Get("Toast_NewSms");
        public static string Toast_Copied => Get("Toast_Copied");
        public static string Toast_CodeCopied => Get("Toast_CodeCopied");
        public static string Toast_CopyCode => Get("Toast_CopyCode");
        public static string Sim_DecodeFailed => Get("Sim_DecodeFailed");
        public static string Sim_ReadFailed => Get("Sim_ReadFailed");
        public static string Sim_NeedNumber => Get("Sim_NeedNumber");
        public static string Sim_NoModem => Get("Sim_NoModem");
        public static string Sim_NoPhonebook => Get("Sim_NoPhonebook");
        public static string Sim_PhonebookFull => Get("Sim_PhonebookFull");
        public static string Sim_TooLong => Get("Sim_TooLong");
        public static string Sim_SavedToSim => Get("Sim_SavedToSim");
        public static string Sim_WriteFailed => Get("Sim_WriteFailed");
        public static string Sim_Updated => Get("Sim_Updated");
        public static string Sys_SendVia => Get("Sys_SendVia");
        public static string Sys_NoSmsDevice => Get("Sys_NoSmsDevice");
        public static string Sys_SentViaSystem => Get("Sys_SentViaSystem");
        public static string Sys_SendFailed => Get("Sys_SendFailed");
        public static string Switch_NoNic => Get("Switch_NoNic");
        public static string Switch_Now5G => Get("Switch_Now5G");
        public static string Switch_NowLte => Get("Switch_NowLte");
        public static string Switch_Up5G => Get("Switch_Up5G");
        public static string Switch_DownLte => Get("Switch_DownLte");
        public static string Backend_IntelName => Get("Backend_IntelName");
        public static string Backend_IntelTodo => Get("Backend_IntelTodo");
        public static string App_ErrorTitle => Get("App_ErrorTitle");
    }
}
