using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
        private ObservableCollection<Mod> modInstallList = new ObservableCollection<Mod>();
        [ObservableProperty]
        private bool isInstalled = false;
        [ObservableProperty]
        private bool compress = false;
        [ObservableProperty]
        private bool compressVisible = Knossos.globalSettings.modCompression == CompressionSettings.Manual? true : false;
        [ObservableProperty]
        private bool canSelectDevMode = true;
        [ObservableProperty]
        private bool installInDevMode = false;

        private Mod? selectedMod;
        [ObservableProperty]
        private bool hasWriteAccess = false;
        [ObservableProperty]
        private bool forceDevMode = false;

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

        public ModInstallViewModel(Mod modJson, string? preSelectedVersion = null, bool forceDevModeOn = false)
        {
            Title = "Installing: " + modJson.title;
            CanSelectDevMode = !forceDevModeOn;
            InstallInDevMode = forceDevModeOn;
            forceDevMode = forceDevModeOn;
            InitialLoad(modJson.id, preSelectedVersion);
        }

        private async void InitialLoad(string id, string? preSelectedVersion = null)
        {
            if (Nebula.userIsLoggedIn)
            {
                var ids = await Nebula.GetEditableModIDs();
                if (ids != null && ids.Any() && ids.FirstOrDefault(x => x == id) != null)
                {
                    HasWriteAccess = true;
                }
            }
            ModVersions =await Nebula.GetAllModsWithID(id);
            if(ModVersions.Any())
            {
                if(preSelectedVersion == null)
                {
                    SelectedIndex = ModVersions.Count() - 1;
                }
                else
                {
                    for(var i=0; i < ModVersions.Count(); i++)
                    {
                        if (ModVersions[i].version == preSelectedVersion)
                        {
                            SelectedIndex = i;
                            continue;
                        }
                    }
                    /* Version not found? maybe deleted from nebula Select the newerest */
                    if(SelectedIndex == -1)
                    {
                        SelectedIndex = ModVersions.Count() - 1;
                    }
                }
            }
            else
            {
                Log.Add(Log.LogSeverity.Warning, "ModInstallViewModel.InitialLoad()", "Unable to find this mod id: "+ id +" on repo.json");
                await MessageBox.Show(MainWindow.instance!,"Unable to find this mod ID on nebula repo, maybe the mod was removed from nebula.","Unable to find mod on repo",MessageBox.MessageBoxButtons.OK);
            }
        }

        private async void UpdateSelectedVersion()
        {
            ModInstallList.Clear();
            DataLoaded = false;
            Compress = false;
            List <Mod> allMods;
            if(SelectedMod != null)
            {
                await SelectedMod.LoadFulLNebulaData();
            }
            if(SelectedMod != null && !SelectedMod.GetMissingDependenciesList().Any())
            {
                allMods = ModVersions.ToList();
            }
            else
            {
                allMods = await Nebula.GetAllModsWithID(null);
            }
            if (SelectedMod != null && allMods.Any())
            {
                var installed=Knossos.GetInstalledMod(SelectedMod.id,SelectedMod.version);
                if(installed != null)
                {
                    /* If its installed just mark installed packages as required, and optional the non installed ones */
                    IsInstalled = true;
                    SelectedMod.fullPath = installed.fullPath;
                    foreach (var displayPkg in SelectedMod.packages)
                    {
                        displayPkg.status = "optional";
                        foreach (var pkg in installed.packages)
                        {
                            if (pkg.name == displayPkg.name)
                            {
                                displayPkg.status = "required";
                            }
                        }
                    }
                    InstallInDevMode = installed.devMode;
                    CanSelectDevMode = false;
                }
                else
                {
                    IsInstalled = false;

                    if(ForceDevMode)
                    {
                        CanSelectDevMode = false;
                        InstallInDevMode = true;
                    }
                    else
                    {
                        CanSelectDevMode = true;
                        InstallInDevMode = false;
                    }
                }
                SelectedMod.isEnabled=false;
                SelectedMod.isSelected = true;
                Compress=SelectedMod.modSettings.isCompressed;
                Name = SelectedMod.title;
                Version = SelectedMod.version;
                await ProcessMod(SelectedMod,allMods);
            }
            DataLoaded = true;
            allMods.Clear();
            GC.Collect();
        }

        private async Task ProcessMod(Mod mod, List<Mod> allMods, List<Mod>? processed = null)
        {
            if (processed == null)
                processed = new List<Mod>();
            var dependencies = mod.GetMissingDependenciesList(false, true);
            AddModToList(mod);
            await Parallel.ForEachAsync(dependencies, new ParallelOptions { MaxDegreeOfParallelism = 2 }, async (dep, token) =>
            {
                if (processed.IndexOf(mod) == -1)
                {
                    processed.Add(mod);
                    var modDep = await dep.SelectModNebula(allMods);
                    if (modDep != null)
                    {
                        modDep.isEnabled = true;
                        modDep.isSelected = true;
                        Parallel.ForEach(modDep.packages, (pkg, token) =>
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
                        });
                        await modDep.LoadFulLNebulaData();
                        await ProcessMod(modDep, allMods, processed);
                    }
                }
            });
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

        internal void VerifyCommand()
        {
            if(SelectedMod != null)
            {
                SelectedMod.modSettings.Load(SelectedMod.fullPath);
                if (SelectedMod.modSettings.isCompressed)
                {
                    MessageBox.Show(MainWindow.instance!, "This mod is compressed, mod verify is not avalible for compressed mods, uncompress it and try again.", "Verify error", MessageBox.MessageBoxButtons.OK);
                    return;
                }
                TaskViewModel.Instance!.VerifyMod(SelectedMod);
            }
        }

        internal void InstallMod(object window)
        {
            foreach (var mod in ModInstallList)
            {
                if (mod == SelectedMod)
                    mod.devMode = InstallInDevMode;
                if(mod.isSelected)
                    TaskViewModel.Instance!.InstallMod(mod, null, Compress);
            }
            ModInstallList.Clear();

            var w = (Window)window;
            w.Close();
        }
    }
}
