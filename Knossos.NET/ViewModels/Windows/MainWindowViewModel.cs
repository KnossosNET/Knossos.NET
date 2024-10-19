using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using Avalonia.Threading;

namespace Knossos.NET.ViewModels
{
    public record MainViewMenuItem(ViewModelBase vm, string? iconRoute, string label, string tooltip);

    /// <summary>
    /// Main Windows View Mode
    /// Everything starts here
    /// All other parts of the UI are attached here, except for popup windows
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase 
    {
        public static MainWindowViewModel? Instance { get; set; }

        /* UI Bindings, use the uppercase version, otherwise changes will not register */
        [ObservableProperty]
        internal string appTitle = "Knossos.NET v" + Knossos.AppVersion;
        [ObservableProperty]
        internal ModListViewModel installedModsView = new ModListViewModel();
        [ObservableProperty]
        internal NebulaModListViewModel nebulaModsView = new NebulaModListViewModel();
        [ObservableProperty]
        internal FsoBuildsViewModel fsoBuildsView = new FsoBuildsViewModel();
        [ObservableProperty]
        internal DeveloperModsViewModel developerModView = new DeveloperModsViewModel();
        [ObservableProperty]
        internal PxoViewModel pxoView = new PxoViewModel();
        [ObservableProperty]
        internal GlobalSettingsViewModel globalSettingsView = new GlobalSettingsViewModel();
        [ObservableProperty]
        internal TaskViewModel taskView = new TaskViewModel();
        [ObservableProperty]
        internal CommunityViewModel communityView = new CommunityViewModel();
        [ObservableProperty]
        internal DebugViewModel debugView = new DebugViewModel();
        [ObservableProperty]
        internal TaskInfoButtonViewModel? taskInfoButton;
        [ObservableProperty]
        internal bool isMenuOpen = true;
        [ObservableProperty]
        internal ObservableCollection<MainViewMenuItem>? menuItems;
        [ObservableProperty]
        private MainViewMenuItem? selectedMenuItem;
        [ObservableProperty]
        internal ViewModelBase? currentViewModel;



        internal string sharedSearch = string.Empty;

        public string LatestNightly = string.Empty;
        public string LatestStable = string.Empty;

        public enum SortType
        {
            name,
            release,
            update,
            unsorted
        }

        internal SortType sharedSortType = SortType.name;

        public MainWindowViewModel()
        {
            Instance = this;
            TaskInfoButton = new TaskInfoButtonViewModel(this.TaskView);
            string[] args = Environment.GetCommandLineArgs();
            bool isQuickLaunch = false;
            bool forceUpdate = false;
            foreach (var arg in args)
            {
                if (arg.ToLower() == "-playmod")
                {
                    isQuickLaunch = true;
                }
                if (arg.ToLower() == "-forceupdate")
                {
                    forceUpdate = true;
                }
            }
            FillMenuItemsNormalMode(1);
            Knossos.StartUp(isQuickLaunch, forceUpdate);
        }

        public void FillMenuItemsNormalMode(int defaultSelectedIndex)
        {
            Dispatcher.UIThread.Invoke(new Action(() => { 
                MenuItems = new ObservableCollection<MainViewMenuItem>{
                    new MainViewMenuItem(TaskView, null, "Tasks", "Overview of current running tasks"),
                    new MainViewMenuItem(InstalledModsView, "avares://Knossos.NET/Assets/general/menu_play.png", "Play", "View and run installed Freepsace Open games and modifications"),
                    new MainViewMenuItem(NebulaModsView, "avares://Knossos.NET/Assets/general/menu_explore.png", "Explore", "Search and install Freespace Open games and modifications"),
                    new MainViewMenuItem(FsoBuildsView, "avares://Knossos.NET/Assets/general/menu_engine.png", "Engine", "Download new Freespace Open engine builds"),
                    new MainViewMenuItem(DeveloperModView, "avares://Knossos.NET/Assets/general/menu_develop.png", "Develop", "Develop new games and modifications for the Freespace Open Engine"),
                    new MainViewMenuItem(CommunityView, "avares://Knossos.NET/Assets/general/menu_community.png", "Community", "FAQs and Community Resources"),
                    new MainViewMenuItem(PxoView, "avares://Knossos.NET/Assets/general/menu_multiplayer.png", "Multiplayer", "View multiplayer games using PXO servers"),
                    new MainViewMenuItem(GlobalSettingsView, "avares://Knossos.NET/Assets/general/menu_settings.png", "Settings", "Change global Freespace Open and Knossos.NET settings"),
                    new MainViewMenuItem(DebugView, "avares://Knossos.NET/Assets/general/menu_debug.png", "Debug", "Debug info")
                };
                if (MenuItems != null && MenuItems.Count() - 1 > defaultSelectedIndex)
                {
                    SelectedMenuItem = MenuItems[defaultSelectedIndex];
                }
            }));
        }

        /// <summary>
        /// When the user clicks a sidebar menu item this code is called
        /// </summary>
        /// <param name="value"></param>
        partial void OnSelectedMenuItemChanged(MainViewMenuItem? value)
        {
            if (value != null)
            {
                // Things to do on tab exit
                if (CurrentViewModel == InstalledModsView) //Exiting the Play tab.
                {
                    sharedSearch = InstalledModsView.Search;

                    // Change and save to the new sort type.
                    if (sharedSortType != InstalledModsView.sortType && InstalledModsView.sortType != SortType.unsorted)
                    {
                        sharedSortType = InstalledModsView.sortType;
                        Knossos.globalSettings.Save(false);
                    }
                }
                if (CurrentViewModel == NebulaModsView) //Exiting the Nebula tab.
                {
                    sharedSearch = NebulaModsView.Search;

                    if (sharedSortType != NebulaModsView.sortType && NebulaModsView.sortType != SortType.unsorted)
                    {
                        sharedSortType = NebulaModsView.sortType;
                        Knossos.globalSettings.Save(false);
                    }
                }

                CurrentViewModel = value.vm;

                //Run code when entering a new view
                if (CurrentViewModel == InstalledModsView) //Play Tab
                {
                    InstalledModsView.Search = sharedSearch;
                    InstalledModsView.ChangeSort(sharedSortType);
                }
                if (CurrentViewModel == NebulaModsView) //Nebula Mods
                {
                    NebulaModsView.OpenTab(sharedSearch, sharedSortType);
                }
                if (CurrentViewModel == DeveloperModView) //Dev Tab
                {
                    DeveloperModsViewModel.Instance?.MaybeChangeSorting();
                    DeveloperModView.UpdateBuildInstallButtons();
                }
                if (CurrentViewModel == CommunityView) //Community Tab
                {
                    Task.Run(async () => { await CommunityView.LoadFAQRepo(); });
                }
                if (CurrentViewModel == PxoView) //PXO
                {
                    PxoViewModel.Instance!.InitialLoad();
                }
                if (CurrentViewModel == GlobalSettingsView) //Settings
                {
                    Knossos.globalSettings.Load();
                    GlobalSettingsView.LoadData();
                    Knossos.globalSettings.EnableIniWatch();
                    GlobalSettingsView.UpdateImgCacheSize();
                }
                else
                {
                    Knossos.globalSettings.DisableIniWatch();
                }
            }
        }

        /* External Commands */
        /// <summary>
        /// Add mod to DevMod tab
        /// </summary>
        /// <param name="devmod"></param>
        public void AddDevMod(Mod devmod)
        {
            DeveloperModView.AddMod(devmod);
        }

        /// <summary>
        /// Refresh Installed mods status icon/border
        /// </summary>
        public void RunModStatusChecks()
        {
            InstalledModsView.RunModStatusChecks();
        }

        /// <summary>
        /// Clear all views
        /// </summary>
        public void ClearViews()
        {
            InstalledModsView?.ClearView();
            FsoBuildsView?.ClearView();
            NebulaModsView?.ClearView();
            DeveloperModView?.ClearView();
        }

        /// <summary>
        /// Informs in the installed mod card that a update is available
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        public void MarkAsUpdateAvailable(string id, bool value = true)
        {
            InstalledModsView.UpdateIsAvailable(id, value);
        }

        /// <summary>
        /// Add a mod to the installed (home) tab
        /// </summary>
        /// <param name="modJson"></param>
        public void AddInstalledMod(Mod modJson)
        {
            InstalledModsView.AddMod(modJson);
        }

        /// <summary>
        /// Check to see if the build provided is the most recent nightly
        /// </summary>
        /// <param name="buildId"></param>
        /// <param name="nightly"></param> 
        public void AddMostRecent(string buildId, bool nightly)
        {
            if (nightly){
                LatestNightly = buildId;
            } else {
                LatestStable = buildId;
            }
        }

        /// <summary>
        /// Add a mod to the nebula (explore) tab
        /// </summary>
        /// <param name="modJson"></param>
        public void AddNebulaMod(Mod modJson)
        {
            NebulaModsView.AddMod(modJson);
        }

        /// <summary>
        /// First load of Nebula mods
        /// </summary>
        /// <param name="mods"></param>
        /// <param name="clear"></param>
        public void BulkLoadNebulaMods(List<Mod> mods, bool clear)
        {
            if(clear)
                NebulaModsView.ClearView();
            NebulaModsView.AddMods(mods);
        }

        /// <summary>
        /// Relay cancel order to the nebula modcard
        /// </summary>
        /// <param name="id"></param>
        public void CancelModInstall(string id)
        {
            NebulaModsView.CancelModInstall(id);
        }

        /// <summary>
        /// Deletes a modcard from the installed (home) tab
        /// </summary>
        /// <param name="id"></param>
        public void RemoveInstalledMod(string id)
        {
            InstalledModsView.RemoveMod(id);
        }

        /// <summary>
        /// Remove a installed mod version from the modcard UI
        /// </summary>
        /// <param name="mod"></param>
        public void RemoveInstalledModVersion(Mod mod)
        {
            InstalledModsView.RemoveModVersion(mod);
        }

        /// <summary>
        /// Load settings to settings tab
        /// </summary>
        public void GlobalSettingsLoadData()
        {
            GlobalSettingsView.LoadData();
        }

        internal void applySettingsToList()
        {
            if (InstalledModsView != null)
            {
                InstalledModsView.ChangeSort(sharedSortType);
            }
        }

        public void UpdateBuildInstallButtons(){
            DeveloperModView?.UpdateBuildNames(LatestStable, LatestNightly);
            QuickSetupViewModel.Instance?.UpdateBuildName(LatestStable);
        }

        /// <summary>
        /// Write a string to UI console on debug tab
        /// </summary>
        /// <param name="message"></param>
        public void WriteToUIConsole(string message)
        {
            DebugView.WriteToUIConsole(message);
        }

        /// <summary>
        /// Open screenshot folder button command
        /// </summary>
        internal void OpenScreenshotsFolder()
        {
            try
            {
                KnUtils.OpenFolder(KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "screenshots");
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "MainWindowViewModel.OpenScreenshotsFolder", ex);
            }
        }

        internal void TriggerMenuCommand()
        {
            IsMenuOpen = !IsMenuOpen;
        }
    }
}
