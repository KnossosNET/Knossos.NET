using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using SharpCompress;

namespace Knossos.NET.ViewModels
{
    public partial class ModListViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool isNebulaView = false;

        private string search  = string.Empty;

        private string Search
        {
            get { return search; }
            set 
            {
                this.SetProperty(ref search, value);
                if (value.Trim() != string.Empty)
                {
                    foreach(var mod in Mods)
                    {
                        if( mod.Name != null && mod.Name.ToLower().Contains(value.ToLower()))
                        {
                            mod.Visible = true;
                        }
                        else
                        {
                            mod.Visible = false;
                        }
                    }
                }
                else
                {
                    Mods.ForEach(m => m.Visible = true);
                }
            }
        } 

        private ObservableCollection<ModCardViewModel> Mods { get; set; } = new ObservableCollection<ModCardViewModel>();

        public ModListViewModel()
        {
        }

        public ModListViewModel(bool isNebulaView)
        {
            IsNebulaView = isNebulaView;
        }

        public void ReloadRepoCommand()
        {
            Knossos.ResetBasePath();
        }

        public void ClearView()
        {
            Mods.Clear();
        }

        public void RunDependencyCheck()
        {
            Mods.ForEach(m => m.CheckDependencyActiveVersion());
        }

        public void AddMod(Mod modJson)
        {
            var modCard = FindModCard(modJson.id);
            if (modCard == null)
            {
                int i;
                for (i = 0; i < Mods.Count; i++)
                {
                    if (String.Compare(Mods[i].Name, modJson.title) > 0)
                    {
                        break;
                    }
                }
                Mods.Insert(i, new ModCardViewModel(modJson));
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
