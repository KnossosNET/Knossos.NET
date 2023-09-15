using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.Views;
using Knossos.NET.Views.Windows;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Knossos.NET.ViewModels
{
    public partial class DeveloperModsViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<Mod> mods = new ObservableCollection<Mod>();
        [ObservableProperty]
        private DevModEditorViewModel? modEditor;
        
        private int selectedIndex = -1;
        private int SelectedIndex
        {
            get
            {
                return selectedIndex;
            }
            set
            {
                if (selectedIndex != value)
                {
                    if (value >= 0)
                        selectedIndex = value;
                    else
                        selectedIndex = 0;

                    if (Mods.Count > selectedIndex)
                    {
                        if(ModEditor == null)
                            ModEditor = new DevModEditorViewModel();
                        ModEditor.LoadMod(Mods[selectedIndex]);
                    }
                }
            }
        }



        public DeveloperModsViewModel()
        {
            
        }

        public void AddMod(Mod mod)
        {
            try
            {
                var exist = Mods.FirstOrDefault(m => m.id == mod.id);
                if (exist == null)
                {
                    Mods.Add(mod);
                }
                else
                {
                    if (SemanticVersion.Compare(exist.version, mod.version) < 1)
                    {
                        Mods.Remove(exist);
                        Mods.Add(mod);
                    }
                }
            }catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "DeveloperModsViewModel.AddMod()", ex);
            }
        }
    }
}
