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
        [ObservableProperty]
        private NebulaLoginViewModel nebulaLoginVM = new NebulaLoginViewModel();

        private int tabIndex = 0;
        private int TabIndex
        {
            get => tabIndex;
            set
            {
                if (value != tabIndex)
                {
                    this.SetProperty(ref tabIndex, value);
                    if (tabIndex == 0) //MODS
                    {
                    }
                    if (tabIndex == 1) //Tools
                    {
                    }
                    if (tabIndex == 2) //Nebula Login
                    {
                        NebulaLoginVM.UpdateUI();
                    }
                }
            }
        }

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
                        ModEditor.StartModEditor(Mods[selectedIndex]);
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
                    if(mod.modSource == ModSource.nebula)
                        Mods.Add(mod);
                }
                else
                {
                    if (SemanticVersion.Compare(exist.version, mod.version) < 1)
                    {
                        exist = mod;
                    }
                    if (ModEditor != null && ModEditor.ActiveVersion.id == mod.id)
                    {
                        ModEditor.AddModToList(mod);
                    }
                }
            }catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "DeveloperModsViewModel.AddMod()", ex);
            }
        }

        /* Buttons */

        internal async void CreateMod()
        {
            var dialog = new DevModCreateNewView();
            dialog.DataContext = new DevModCreateNewViewModel();
            await dialog.ShowDialog<DevModCreateNewView?>(MainWindow.instance!);
        }
    }
}
