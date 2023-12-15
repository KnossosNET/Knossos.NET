using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
    /// <summary>
    /// View Model for the Mod Install Window
    /// </summary>
    public partial class ModInstallViewModel : ViewModelBase
    {
        private Window? dialogWindow;

        [ObservableProperty]
        internal List<Mod> modVersions = new List<Mod>();
        [ObservableProperty]
        internal string title = string.Empty;
        [ObservableProperty]
        internal string version = string.Empty;
        [ObservableProperty]
        internal string name = string.Empty;
        [ObservableProperty]
        internal int selectedIndex = -1;
        [ObservableProperty]
        internal bool dataLoaded = false;
        [ObservableProperty]
        internal ObservableCollection<Mod> modInstallList = new ObservableCollection<Mod>();
        [ObservableProperty]
        internal bool isInstalled = false;
        [ObservableProperty]
        internal bool compress = false;
        [ObservableProperty]
        internal bool compressVisible = Knossos.globalSettings.modCompression == CompressionSettings.Manual? true : false;
        [ObservableProperty]
        internal bool canSelectDevMode = true;
        [ObservableProperty]
        internal bool installInDevMode = false;
        [ObservableProperty]
        internal bool hasWriteAccess = false;
        [ObservableProperty]
        internal bool forceDevMode = false;
        [ObservableProperty]
        internal string installSize = string.Empty;
        [ObservableProperty]
        internal string freeSpace = string.Empty;
        [ObservableProperty]
        internal bool cleanupVisible = false;
        [ObservableProperty]
        internal bool cleanupEnabled = true;
        [ObservableProperty]
        internal bool isMetaUpdate = false;


        internal Mod? selectedMod;
        internal Mod? SelectedMod
        {
            get { return selectedMod; }
            set
            {
                if (selectedMod != value)
                {
                    this.SetProperty(ref selectedMod, value);
                    UpdateSelectedVersion();
                }
            }
        }

        public ModInstallViewModel() 
        {
        }

        /// <summary>
        /// Mod Install View Model
        /// Dialog is the window this datacontext is attached to, used to close the window when user clicks "install"
        /// </summary>
        /// <param name="modJson"></param>
        /// <param name="dialog"></param>
        /// <param name="preSelectedVersion"></param>
        /// <param name="forceDevModeOn"></param>
        public ModInstallViewModel(Mod modJson, Window dialog, string? preSelectedVersion = null, bool forceDevModeOn = false)
        {
            dialogWindow = dialog;
            Title = "Installing: " + modJson.title;
            CanSelectDevMode = !forceDevModeOn;
            InstallInDevMode = forceDevModeOn;
            forceDevMode = forceDevModeOn;
            InitialLoad(modJson.id, preSelectedVersion);
        }

        /// <summary>
        /// Initial Load, if preselected version is pass, that would be the displayed version when the UI is show
        /// </summary>
        /// <param name="id"></param>
        /// <param name="preSelectedVersion"></param>
        private async void InitialLoad(string id, string? preSelectedVersion = null)
        {
            if (Nebula.userIsLoggedIn)
            {
                var ids = await Nebula.GetEditableModIDs();
                if (ids != null && ids.Any() && ids.FirstOrDefault(x => x == id) != null)
                {
                    HasWriteAccess = true;
                    InstallInDevMode = true;
                }
            }
            ModVersions = await Nebula.GetAllModsWithID(id);
            if(ModVersions.Any())
            {
                ModVersions.Sort(Mod.CompareVersion);
                if (preSelectedVersion == null)
                {
                    SelectedIndex = ModVersions.Count() - 1;
                }
                else
                {
                    var preSelect = ModVersions.FirstOrDefault(m=>m.version == preSelectedVersion);
                    if (preSelect != null)
                    {
                        SelectedIndex = ModVersions.IndexOf(preSelect);
                    }
                    else
                    {
                        /* Version not found? maybe deleted from nebula Select the newerest */
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

        /// <summary>
        /// Load to UI all mod packages (and dependencies) for the current selected mod version
        /// </summary>
        private async void UpdateSelectedVersion()
        {
            ModInstallList.Clear();
            IsMetaUpdate = false;
            DataLoaded = false;
            CleanupVisible = false;
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
                    SelectedMod.installed = true;
                    InstallInDevMode = installed.devMode;
                    CanSelectDevMode = false;
                    IsMetaUpdate = Mod.IsMetaUpdate(SelectedMod,installed);
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
                        InstallInDevMode = HasWriteAccess;
                    }
                }
                SelectedMod.isEnabled=false;
                SelectedMod.isSelected = true;
                Compress=SelectedMod.modSettings.isCompressed;
                Name = SelectedMod.title;
                Version = SelectedMod.version;
                await ProcessMod(SelectedMod,allMods);
            }
            if (!IsInstalled && SelectedMod != null && ModVersions.Count > 1 && ModVersions.IndexOf(SelectedMod) > 0)
            {
                CleanupVisible = true;
            }
            DataLoaded = true;
            allMods.Clear();
            GC.Collect();
        }

        /// <summary>
        /// Add a mod to the install list/tree
        /// Loads full data from nebula if not already loaded for the relevant mods
        /// Detects any missing dependency of this mod and add them to install too
        /// Then it recursively calls this function again on all dependencies
        /// Making a full mod tree dependency resolution.
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="allMods"></param>
        /// <param name="processed"></param>
        private async Task ProcessMod(Mod mod, List<Mod> allMods, List<Mod>? processed = null)
        {
            if (processed == null)
                processed = new List<Mod>();
            var dependencies = mod.GetMissingDependenciesList(false, true);
            //Display this mod on install list
            AddModToList(mod);
            //Add this mod here to avoid possible looping
            processed.Add(mod);
            foreach (var dep in dependencies)
            {
                var modDep = await dep.SelectModNebula(allMods);
                if (modDep != null)
                {
                    //Load Nebula data first to check the packages
                    await modDep.LoadFulLNebulaData();
                    //Make sure to mark all needed pkgs this mod need as required
                    modDep.isEnabled = true;
                    modDep.isSelected = true;
                    foreach(var pkg in modDep.packages)
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
                    //If process this depmod own dependencies if we havent done already
                    //Otherwise re-add it to the list to enabled any potential new pkg needed
                    if (processed.IndexOf(modDep) == -1)
                    {
                        await ProcessMod(modDep, allMods, processed);
                    }
                    else
                    {
                        AddModToList(modDep);
                    }
                }
            }
        }

        /// <summary>
        /// Add mod to list, it cheks if the mod is already added and if thats the case
        /// then it checks the packages of that mod to make sure recommended and requiered packages are properly marked
        /// </summary>
        /// <param name="mod"></param>
        private void AddModToList(Mod mod)
        {
            var modInList = ModInstallList.FirstOrDefault(m=>m.id == mod.id && m.version == mod.version);

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
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Starts Mod Verify Task
        /// </summary>
        internal void VerifyCommand()
        {
            if(SelectedMod != null)
            {
                SelectedMod.modSettings.Load(SelectedMod.fullPath);
                if (SelectedMod.modSettings.isCompressed)
                {
                    MessageBox.Show(MainWindow.instance!, "This mod is compressed, mod verify is not available for compressed mods, uncompress it and try again.", "Verify error", MessageBox.MessageBoxButtons.OK);
                    return;
                }
                TaskViewModel.Instance!.VerifyMod(SelectedMod);
            }
        }

        /// <summary>
        /// Calls install mod on all mods in install list
        /// And closes the window
        /// </summary>
        internal void InstallMod()
        {
            foreach (var mod in ModInstallList)
            {
                var cleanOldVersions = false;
                if (mod == SelectedMod)
                {
                    mod.devMode = InstallInDevMode;
                    //Only for the mod we are installing, we need to check if the option is visible AND enabled
                    if(CleanupVisible && CleanupEnabled)
                        cleanOldVersions = true;
                }
                if (mod.isSelected)
                    TaskViewModel.Instance!.InstallMod(mod, null, Compress, cleanOldVersions);
            }
            ModInstallList.Clear();

            if (dialogWindow != null)
            { 
                dialogWindow.Close(); 
            }      
        }

        internal async void UpdateSpace()
        {
            try
            {
                await Task.Delay(500);
                long size = 0;
                foreach (var mod in ModInstallList)
                {
                    if(mod.isSelected)
                    {
                        if (mod.packages != null)
                        {
                            Mod? installed = null;
                            if (mod.installed)
                            {
                                installed = Knossos.GetInstalledMod(mod.id, mod.version);
                            }
                            foreach (var pkg in mod.packages)
                            {
                                if (pkg.isSelected && pkg.files != null)
                                {
                                    if(installed != null)
                                    {
                                        if (installed.IsPackageInstalled(pkg.name))
                                            continue;
                                    }
                                    foreach (var file in pkg.files)
                                    {
                                        size += file.filesize;
                                    }
                                }
                            }
                        }
                    }
                }
                await Dispatcher.UIThread.InvokeAsync(() => {
                    InstallSize = "Download Size: " + KnUtils.FormatBytes(size) + " (*)";
                    if(Knossos.GetKnossosLibraryPath() != null)
                       FreeSpace = "Free space in library: " + KnUtils.FormatBytes(KnUtils.CheckDiskSpace(Knossos.GetKnossosLibraryPath()!));
                });
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "ModInstallViewModel.UpdateSpace()", ex);
            }
        }
    }
}
