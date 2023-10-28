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
        public static DeveloperModsViewModel? Instance;
        [ObservableProperty]
        internal ObservableCollection<Mod> mods = new ObservableCollection<Mod>();
        [ObservableProperty]
        internal DevModEditorViewModel? modEditor;
        [ObservableProperty]
        internal NebulaLoginViewModel nebulaLoginVM = new NebulaLoginViewModel();
        [ObservableProperty]
        internal DevToolManagerViewModel devToolManager = new DevToolManagerViewModel();

        internal int tabIndex = 0;
        internal int TabIndex
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
                        DevToolManager.LoadTools();
                    }
                    if (tabIndex == 2) //Nebula Login
                    {
                        NebulaLoginVM.UpdateUI();
                    }
                }
            }
        }

        internal int selectedIndex = -1;
        internal int SelectedIndex
        {
            get
            {
                return selectedIndex;
            }
            set
            {
                if (selectedIndex != value || ModEditor == null)
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
            Instance = this;
        }

        public void CloseEditor()
        {
            ModEditor = null;
            if (Mods.Count > 0)
            {
                SelectedIndex = 0;
            }
            else
            {
                SelectedIndex = -1;
            }
            GC.Collect();
        }

        public void DeleteMod(string modid)
        {
            var mod = Mods.FirstOrDefault(m=>m.id == modid);
            if(mod != null)
                Mods.Remove(mod);
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
