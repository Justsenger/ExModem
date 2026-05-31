using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using ExModem.Properties;
using ExModem.Services;
using ExModem.Tools;

namespace ExModem.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private bool _isInitializing = true;

        [ObservableProperty] private List<string> _availableThemes;
        [ObservableProperty] private string _selectedTheme;
        [ObservableProperty] private List<string> _availableLanguages;
        [ObservableProperty] private string _selectedLanguage;

        public string CopyrightInfo => "© 2026 | " + Utils.Author + " | " + Utils.Version;

        public SettingsViewModel()
        {
            AvailableThemes = new List<string> { Resources.light, Resources.dark };
            AvailableLanguages = new List<string>();
            foreach (var l in SettingsService.Languages) AvailableLanguages.Add(l.Native);

            LoadCurrentSettings();
            _isInitializing = false;
        }

        private void LoadCurrentSettings()
        {
            _selectedTheme = SettingsService.GetTheme();
            _selectedLanguage = SettingsService.LangNativeName(SettingsService.GetLanguage());
        }

        partial void OnSelectedThemeChanged(string value)
        {
            if (_isInitializing || value == null) return;
            SettingsService.ApplyTheme(value);
        }

        partial void OnSelectedLanguageChanged(string value)
        {
            if (_isInitializing || value == null) return;
            SettingsService.SetLanguageAndRestart(value);
        }
    }
}
