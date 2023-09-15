using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
    public partial class DevModVersionsViewModel : ViewModelBase
    {
        [ObservableProperty]
        private List<Mod> mods = new List<Mod>();

        private int selectedIndex = -1;
        private int SelectedIndex
        {
            get
            {
                return selectedIndex;
            }
            set
            {
                if(selectedIndex != value)
                {
                    selectedIndex = value;
                    if(selectedIndex < Mods.Count && selectedIndex >= 0 && editor != null)
                    {
                        editor.ChangeActiveVersion(Mods[value]);
                    }                    
                }
            }
        }

        private DevModEditorViewModel? editor;

        public DevModVersionsViewModel() 
        { 
        }

        public DevModVersionsViewModel(DevModEditorViewModel editor)
        {
            this.editor = editor;
            mods = editor.GetModList();
            foreach(var m in mods)
            {
                if(editor.ActiveVersion == m)
                {
                    selectedIndex = mods.IndexOf(m);
                }
            }
        }
    }
}
