using Knossos.NET.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Knossos.NET.ViewModels
{
    public partial class CheckableModListViewModel : ViewModelBase
    {
        /* General Mod variables */
        public Mod? mod { get; set; }
        private bool modCheckedBackup;
        
        public delegate void OnModCheckChanged(Mod mod, bool modChecked);

        public OnModCheckChanged? onModCheckChangedHandler;

        /* UI Bindings */
        [ObservableProperty]
        private string? modName;
        [ObservableProperty]
        private bool modChecked;
        [ObservableProperty]
        private bool modCheckEnabled;

        /* Should only be used by the editor preview */
        public CheckableModListViewModel()
        {
            mod = null;
            ModName = "";
            ModChecked = true;
            ModCheckEnabled = true;
        }

        public CheckableModListViewModel(Mod modJson)
        {
            modJson.ClearUnusedData();
            mod = modJson;
            ModName = modJson.title;
            ModChecked = true;
            ModCheckEnabled = true;
        }

        partial void OnModCheckedChanging(bool value)
        {
            if (mod != null)
            {
                onModCheckChangedHandler?.Invoke(mod, value);
            }
        }

        public void SetModCheckedEnabled(bool enabled)
        {
            if (enabled == ModCheckEnabled)
                return;
            
            if (!enabled)
            {
                modCheckedBackup = ModChecked;
                ModChecked = false;
            }
            else
            {
                ModChecked = modCheckedBackup;
            }

            ModCheckEnabled = enabled;
        }
    }
}
