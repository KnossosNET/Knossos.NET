using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.Views;
using Knossos.NET.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Knossos.NET.ViewModels
{
    /// <summary>
    /// Develop Tab View Model
    /// </summary>
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
        [ObservableProperty]
        public string latestStable = string.Empty;
        [ObservableProperty]
        public string latestNightly = string.Empty;

        [ObservableProperty]
        public bool nightlyInstalled = false;
        [ObservableProperty]
        public bool stableInstalled = false;

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
                        this.SetProperty(ref selectedIndex, value);
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

        /// <summary>
        /// Close Mod Editor
        /// </summary>
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


        /// <summary>
        /// Updates the Version Manager Version List
        /// If modid is passed it will be only be updated IF loaded mod id matches the modid
        /// </summary>
        /// <param name="modid"></param>
        public void UpdateVersionManager(string? modid = null)
        {
            ModEditor?.UpdateVersionManager(modid); 
        }

        /// <summary>
        /// Resets the mod editor
        /// Reloads the currently loaded mod in editor if it matches the passed id, null for always
        /// </summary>
        /// <param name="modid"></param>
        public void ResetModEditor(string? modid = null)
        {
            if (ModEditor != null && ( modid == null || modid == ModEditor.ActiveVersion.id))
            {
                ModEditor = new DevModEditorViewModel();
                ModEditor.StartModEditor(Mods[selectedIndex]);
            }
        }

        /// <summary>
        /// Delete a mod from the Dev Mod list
        /// </summary>
        /// <param name="modid"></param>
        public void DeleteMod(string modid)
        {
            var mod = Mods.FirstOrDefault(m=>m.id == modid);
            if(mod != null)
                Mods.Remove(mod);
        }

        /// <summary>
        /// Add a mod to the Dev Mod list
        /// Only one version can exist in this list at any given time
        /// </summary>
        /// <param name="mod"></param>
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

        /// <summary>
        /// Clear view
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        internal void ClearView()
        {
            Mods.Clear();
            CloseEditor();
        }

        /* Buttons */

        /// <summary>
        /// Open Create Mod dialog
        /// </summary>
        internal async void CreateMod()
        {
            var dialog = new DevModCreateNewView();
            dialog.DataContext = new DevModCreateNewViewModel(dialog);
            await dialog.ShowDialog<DevModCreateNewView?>(MainWindow.instance!);
        }

        public void UpdateBuildNames(string stableIn, string nightlyIn)
        {
            LatestStable = stableIn;
            LatestNightly = nightlyIn;
            UpdateBuildInstallButtons();
        }

        public void UpdateBuildInstallButtons(){
            if (LatestStable == "")
            {
                StableInstalled = true;
            }
            else 
            {
                var installed = Knossos.GetInstalledBuild("FSO", LatestStable);
                StableInstalled = (installed != null);
                if (StableInstalled)
                    LatestStable = "";
            }
            if (LatestNightly == "")
            {
                NightlyInstalled = true;
            }
            else 
            {
                var installed = Knossos.GetInstalledBuild("FSO", LatestNightly);
                NightlyInstalled = (installed != null);
                if (NightlyInstalled)
                    LatestNightly = "";
            }
        }

        public void InstallLatestNightly()
        {
            if (LatestNightly != ""){
                var nightly = new Mod();
                nightly.id = "FSO";
                nightly.version = LatestNightly;
                nightly.type = ModType.engine;
                nightly.stability = "nightly";

                MainWindowViewModel.Instance!.FsoBuildsView!.RelayInstallBuild(nightly);

                var installed = Knossos.GetInstalledBuild("FSO", LatestNightly);
                if (installed != null){
                    LatestNightly = "";
                    UpdateBuildInstallButtons();
                }
            }
        }

        public void InstallLatestStable()
        {
            if (LatestStable != ""){
                var stable = new Mod();
                stable.id = "FSO";
                stable.version = LatestStable;
                stable.type = ModType.engine;
                stable.stability = "stable";

                MainWindowViewModel.Instance!.FsoBuildsView!.RelayInstallBuild(stable);

                var installed = Knossos.GetInstalledBuild("FSO", LatestStable);
                if (installed != null){
                    LatestStable = "";
                    UpdateBuildInstallButtons();
                }
            }
        }
    }
}
