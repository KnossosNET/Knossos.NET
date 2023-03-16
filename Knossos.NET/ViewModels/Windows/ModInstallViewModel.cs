using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Knossos.NET.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

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
            var allMods = await Nebula.GetAllModsWithID(null);
            if (SelectedMod != null)
            {
                DataLoaded = false;
                SelectedMod.isEnabled=false;
                SelectedMod.isSelected = true;
                Name = SelectedMod.title;
                Version = SelectedMod.version;
                await ProcessMod(SelectedMod,allMods);
            }
            DataLoaded = true;
            allMods.Clear();
            GC.Collect();
        }


        private async Task ProcessMod(Mod mod, List<Mod> allMods)
        {
            var dependencies = mod.GetMissingDependenciesList();
            AddModToList(mod);
            foreach (var dep in dependencies)
            {
                var modDep = await dep.SelectModNebula(allMods);
                if (modDep != null)
                { 
                    modDep.isEnabled = true;
                    modDep.isSelected = true;
                    foreach (var pkg in modDep.packages)
                    {
                        if (dep != null && dep.packages != null)
                        {
                            foreach (var depPkg in dep.packages)
                            {
                                if (depPkg == pkg.name)
                                {
                                    pkg.status = "required";
                                }
                            }
                        }
                    }
                    await ProcessMod(modDep, allMods);
                }
            }
        }

        private void AddModToList(Mod mod)
        {
            Mod? modInList = null;
            foreach (var inList in ModInstallList)
            {
                if (mod.id == inList.id && inList.version == mod.version)
                {
                    modInList = inList;
                    continue;
                }
            }

            if (modInList == null)
            {
                /* This mod was not added to the install list yet */
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
            else
            {
                foreach(var pkgInList in modInList.packages)
                {
                    /* If the mod is already added make sure recommended and requiered packages are properly marked */
                    foreach (var pkg in mod.packages)
                    {
                        if (pkgInList == pkg)
                        {
                            if (pkg.status!.ToLower() == "required")
                            {
                                pkgInList.isEnabled = false;
                                pkgInList.isSelected = true;
                            }
                            else
                            {
                                if (pkg.status!.ToLower() == "recommended")
                                {
                                    pkgInList.isSelected = true;
                                    pkgInList.isEnabled = true;
                                }

                            }
                            continue;
                        }
                    }
                }
            }
        }

        private void InstallMod(Window window)
        {
            foreach (var mod in ModInstallList)
            {
                TaskViewModel.Instance!.InstallMod(mod);
            }
            ModInstallList.Clear();
            window.Close();
        }
    }
}
