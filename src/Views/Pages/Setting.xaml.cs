using ExModem.ViewModels;

namespace ExModem.Views.Pages
{
    public partial class Setting
    {
        public Setting()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
        }
    }
}
