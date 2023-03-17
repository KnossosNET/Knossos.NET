using System.Collections.ObjectModel;
using System.Threading;
using Knossos.NET.Classes;
using Knossos.NET.Models;

namespace Knossos.NET.ViewModels
{
    public partial class ModListViewModel : ViewModelBase
    {
        private ObservableCollection<ModCardViewModel> Mods { get; set; } = new ObservableCollection<ModCardViewModel>();

        public ModListViewModel()
        {

        }


        public void ClearView()
        {
            Mods.Clear();
        }

        public void AddMod(Mod modJson)
        {
            var modCard = FindModCard(modJson.id);
            if (modCard == null)
            {
                Mods.Add(new ModCardViewModel(modJson));
            }
            else
            {
                modCard.AddModVersion(modJson);
            }
            
        }

        public void UpdateIsAvalible(string id,bool value)
        {
            var modCard = FindModCard(id);
            if (modCard != null)
            {
                modCard.UpdateIsAvalible(value);
            }
        }

        public void SetInstalling(string id, CancellationTokenSource cancelToken)
        {
            var modCard = FindModCard(id);
            if (modCard != null)
            {
                modCard.InstallingMod(cancelToken);
            }
        }

        public void RemoveMod(string id)
        {
            var modCard = FindModCard(id);
            if (modCard != null)
            {
                Mods.Remove(modCard);
            }
        }

        public void CancelModInstall(string id)
        {
            var modCard = FindModCard(id);
            if (modCard != null)
            {
                modCard.CancelInstall();
            }
        }

        private ModCardViewModel? FindModCard(string modId) 
        {
            foreach (var mod in Mods)
            {
                if(mod.ID == modId)
                    return mod;
            }
            return null;
        }
    }
}
