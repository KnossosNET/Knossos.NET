using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Knossos.NET.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Knossos.NET.ViewModels
{
    public partial class ModInstallViewModel : ViewModelBase
    {
        [ObservableProperty]
        private List<Mod> modVersions = new List<Mod>();
        [ObservableProperty]
        private string title = string.Empty;
        [ObservableProperty]
        private string version = string.Empty;
        [ObservableProperty]
        private string name = string.Empty;
        [ObservableProperty]
        private int selectedIndex = -1;
        [ObservableProperty]
        private bool dataLoaded = false;
        [ObservableProperty]
        ObservableCollection<Mod> modInstallList = new ObservableCollection<Mod>();
        
        private Mod? selectedMod;

        private Mod? SelectedMod
        {
            get { return selectedMod; }
            set
            {
                if (selectedMod != value)
                {
                    selectedMod = value;
                    UpdateSelectedVersion();
                }
            }
        }

        public ModInstallViewModel() 
        {
        }

        public ModInstallViewModel(Mod modJson)
        {
            Title = "Installing " + modJson.title;
            InitialLoad(modJson.id);
        }

        private async void InitialLoad(string id)
        {
            ModVersions=await Nebula.GetAllModsWithID(id);
            SelectedIndex = ModVersions.Count - 1;
        }

        private async void UpdateSelectedVersion()
        {
            ModInstallList.Clear();
            if (SelectedMod != null)
            {
                DataLoaded = false;
                SelectedMod.isEnabled=false;
                SelectedMod.isSelected = true;
                AddModToList(SelectedMod);
                Name = SelectedMod.title;
                Version = SelectedMod.version;
                var dependencies = SelectedMod.GetMissingDependenciesList();
                var depMods = await Nebula.GetDependecyListModData(dependencies);
                depMods.ForEach(mod => {
                    mod.isEnabled = true;
                    mod.isSelected = true;
                    var dep = dependencies.FirstOrDefault(d => d.version == mod.version && d.id == mod.id);
                    foreach (var pkg in mod.packages)
                    {
                        if(dep != null && dep.packages != null)
                        {
                            foreach(var depPkg in dep.packages)
                            {
                                if(depPkg == pkg.name)
                                {
                                    pkg.status = "required";
                                }
                            }
                        }
                    }
                    AddModToList(mod); 
                });
                DataLoaded = true;
            }
            GC.Collect();
        }

        private void AddModToList(Mod mod)
        {
            foreach (var pkg in mod.packages)
            {
                if (pkg.status!.ToLower() == "required")
                {
                    pkg.isEnabled = false;
                    pkg.isSelected = true;
                }
                else
                {
                    if (pkg.status!.ToLower() == "recommended")
                    {
                        pkg.isSelected = true;
                        pkg.isEnabled = true;
                    }
                    else
                    {
                        pkg.isEnabled = true;
                    }

                }
            }
            ModInstallList.Add(mod);
        }

        private void InstallMod(Window window)
        {
            foreach (var mod in ModInstallList)
            {
                TaskViewModel.Instance!.InstallMod(mod);
            }
            window.Close();
        }
    }
}
