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
using System.Threading;

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

        /* Single TC mode specific stuff */
        [ObservableProperty]
        internal NebulaLoginViewModel? nebulaLoginVM;
        [ObservableProperty]
        internal CustomHomeViewModel? customHomeVM;
        /**/

        /* UI Bindings, use the uppercase version, otherwise changes will not register */
        [ObservableProperty]
        internal string appTitle = "Knossos.NET v" + Knossos.AppVersion;
        [ObservableProperty]
        internal int? windowWidth = null;
        [ObservableProperty]
        internal int? windowHeight = null;
        [ObservableProperty]
        internal ModListViewModel? installedModsView;
        [ObservableProperty]
        internal NebulaModListViewModel? nebulaModsView;
        [ObservableProperty]
        internal FsoBuildsViewModel? fsoBuildsView;
        [ObservableProperty]
        internal DeveloperModsViewModel? developerModView;
        [ObservableProperty]
        internal PxoViewModel? pxoView;
        [ObservableProperty]
        internal GlobalSettingsViewModel? globalSettingsView;
        [ObservableProperty]
        internal TaskViewModel taskView = new TaskViewModel();
        [ObservableProperty]
        internal CommunityViewModel? communityView;
        [ObservableProperty]
        internal DebugViewModel? debugView;
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
        [ObservableProperty]
        internal int taskButtomRow = 0;
        [ObservableProperty]
        internal int buttomListRow = 1;


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

        private SortType _sortType = SortType.name; //do not use directly
        internal SortType sharedSortType
        {
            get { return _sortType; }
            set
            {
                if (_sortType != value)
                {
                    //change sort and update globalsettings value
                    //to be saved at app close
                    this.SetProperty(ref _sortType, value);
                    Knossos.globalSettings.sortType = value;
                }
            }
        }

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
            if (!CustomLauncher.IsCustomMode)
            {
                InstalledModsView = new ModListViewModel();
                NebulaModsView = new NebulaModListViewModel();
                FsoBuildsView = new FsoBuildsViewModel();
                DeveloperModView = new DeveloperModsViewModel();
                GlobalSettingsView = new GlobalSettingsViewModel();
                PxoView = new PxoViewModel();
                CommunityView = new CommunityViewModel();
                DebugView = new DebugViewModel();
                FillMenuItemsNormalMode(1);
            }
            else
            {
                //Apply customization for Single TC Mode
                Knossos.globalSettings.mainMenuOpen = CustomLauncher.MenuOpenFirstTime;
                AppTitle = CustomLauncher.WindowTitle + " v" + Knossos.AppVersion;
                WindowHeight = CustomLauncher.WindowHeight;
                WindowWidth = CustomLauncher.WindowWidth;
                CustomHomeVM = new CustomHomeViewModel();
                if (CustomLauncher.MenuDisplayEngineEntry)
                    FsoBuildsView = new FsoBuildsViewModel();
                if(CustomLauncher.MenuDisplayGlobalSettingsEntry)
                    GlobalSettingsView = new GlobalSettingsViewModel();
                if(CustomLauncher.MenuDisplayDebugEntry)
                    DebugView = new DebugViewModel();
                if (CustomLauncher.MenuDisplayCommunityEntry)
                    CommunityView = new CommunityViewModel();
                if (CustomLauncher.MenuDisplayNebulaLoginEntry)
                    NebulaLoginVM = new NebulaLoginViewModel();
                FillMenuItemsCustomMode(CustomLauncher.MenuTaskButtonAtTheEnd ? 0 : 1);
                if(CustomLauncher.MenuTaskButtonAtTheEnd)
                {
                    //Fix the UI for task on the bottom
                    TaskButtomRow = 1;
                    ButtomListRow = 0;
                    MainWindow.instance?.FixMarginButtomTasks();
                }
            }
            Knossos.StartUp(isQuickLaunch, forceUpdate);
            CustomHomeVM?.CheckBasePath();
        }

        private void FillMenuItemsCustomMode(int defaultSelectedIndex)
        {
            Dispatcher.UIThread.Invoke(new Action(() => {

                if (CustomLauncher.MenuTaskButtonAtTheEnd)
                {
                    MenuItems = new ObservableCollection<MainViewMenuItem>{
                        new MainViewMenuItem(CustomHomeVM!, "avares://Knossos.NET/Assets/general/menu_home.png", "Home", "Home")
                    };
                }
                else
                {
                    MenuItems = new ObservableCollection<MainViewMenuItem>{
                        new MainViewMenuItem(TaskView, null, "Tasks", "Overview of current running tasks"),
                        new MainViewMenuItem(CustomHomeVM!, "avares://Knossos.NET/Assets/general/menu_home.png", "Home", "Home")
                    };
                }

                if (CustomLauncher.CustomMenuButtons != null && CustomLauncher.CustomMenuButtons.Any())
                {
                    foreach(var button in CustomLauncher.CustomMenuButtons)
                    {
                        try
                        {
                            switch (button.Type.ToLower())
                            {
                                case "htmlcontent" :
                                    MenuItems.Add(new MainViewMenuItem(new HtmlContentViewModel(button.LinkURL), button.IconPath, button.Name, button.ToolTip));
                                    break;
                                case "axamlcontent":
                                    MenuItems.Add(new MainViewMenuItem(new AxamlExternalContentViewModel(button.LinkURL, button.Name), button.IconPath, button.Name, button.ToolTip));
                                    break;
                                default:
                                    throw new NotImplementedException("button type: "+ button.Type + " is not supported.");
                            }
                        }
                        catch (Exception ex) 
                        {
                            Log.Add(Log.LogSeverity.Error, "MainWindowViewModel.FillMenuItemsCustomMode()", ex);
                        }
                    }
                }

                if (CustomLauncher.MenuDisplayEngineEntry && FsoBuildsView != null)
                {
                    MenuItems.Add(new MainViewMenuItem(FsoBuildsView, "avares://Knossos.NET/Assets/general/menu_engine.png", "Engine", "Download new Freespace Open engine builds"));
                }

                if(CustomLauncher.MenuDisplayNebulaLoginEntry && NebulaLoginVM != null)
                {
                    MenuItems.Add(new MainViewMenuItem(NebulaLoginVM, "avares://Knossos.NET/Assets/general/menu_nebula.png", "Nebula", "Log in with your nebula account"));
                }

                if (CustomLauncher.MenuDisplayCommunityEntry && CommunityView != null)
                {
                    MenuItems.Add(new MainViewMenuItem(CommunityView!, "avares://Knossos.NET/Assets/general/menu_community.png", "Community", "FAQs and Community Resources"));
                }

                if (CustomLauncher.MenuDisplayGlobalSettingsEntry && GlobalSettingsView != null)
                {
                    MenuItems.Add(new MainViewMenuItem(GlobalSettingsView, "avares://Knossos.NET/Assets/general/menu_settings.png", "Settings", "Change launcher and FSO engine settings"));
                }

                if (CustomLauncher.MenuDisplayDebugEntry && DebugView != null)
                {
                    MenuItems.Add(new MainViewMenuItem(DebugView, "avares://Knossos.NET/Assets/general/menu_debug.png", "Debug", "Debug info"));
                }

                if (CustomLauncher.MenuTaskButtonAtTheEnd)
                {
                    MenuItems.Add(new MainViewMenuItem(TaskView, null, "Tasks", "Overview of current running tasks"));
                }

                if (MenuItems != null && MenuItems.Count() - 1 > defaultSelectedIndex)
                {
                    SelectedMenuItem = MenuItems[defaultSelectedIndex];
                }
            }));
        }

        private void FillMenuItemsNormalMode(int defaultSelectedIndex)
        {
            Dispatcher.UIThread.Invoke(new Action(() => { 
                MenuItems = new ObservableCollection<MainViewMenuItem>{
                    new MainViewMenuItem(TaskView, null, "Tasks", "Overview of current running tasks"),
                    new MainViewMenuItem(InstalledModsView!, "avares://Knossos.NET/Assets/general/menu_play.png", "Play", "View and run installed Freepsace Open games and modifications"),
                    new MainViewMenuItem(NebulaModsView!, "avares://Knossos.NET/Assets/general/menu_explore.png", "Explore", "Search and install Freespace Open games and modifications"),
                    new MainViewMenuItem(FsoBuildsView!, "avares://Knossos.NET/Assets/general/menu_engine.png", "Engine", "Download new Freespace Open engine builds"),
                    new MainViewMenuItem(DeveloperModView!, "avares://Knossos.NET/Assets/general/menu_develop.png", "Develop", "Develop new games and modifications for the Freespace Open Engine"),
                    new MainViewMenuItem(CommunityView!, "avares://Knossos.NET/Assets/general/menu_community.png", "Community", "FAQs and Community Resources"),
                    new MainViewMenuItem(PxoView!, "avares://Knossos.NET/Assets/general/menu_multiplayer.png", "Multiplayer", "View multiplayer games using PXO servers"),
                    new MainViewMenuItem(GlobalSettingsView!, "avares://Knossos.NET/Assets/general/menu_settings.png", "Settings", "Change global Freespace Open and Knossos.NET settings"),
                    new MainViewMenuItem(DebugView!, "avares://Knossos.NET/Assets/general/menu_debug.png", "Debug", "Debug info")
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
                if (InstalledModsView != null && CurrentViewModel == InstalledModsView) //Exiting the Play tab.
                {
                    sharedSearch = InstalledModsView.Search;
                }
                if (NebulaModsView != null &&  CurrentViewModel == NebulaModsView) //Exiting the Nebula tab.
                {
                    sharedSearch = NebulaModsView.Search;
                }
                if(GlobalSettingsView != null &&  CurrentViewModel == GlobalSettingsView) //Exiting the settings view
                {
                    GlobalSettingsView.CommitPendingChanges();
                }
                if (CurrentViewModel != null && CurrentViewModel == CustomHomeVM) //CustomHomeView
                {
                    CustomHomeVM.TaskVM = null;
                    TaskView?.ShowButtons(true);
                    CustomHomeVM.ViewClosed();
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
                if (CurrentViewModel != null && CurrentViewModel == NebulaLoginVM) //Nebula Login (Single TC mode)
                {
                    NebulaLoginVM.UpdateUI();
                }
                if (CurrentViewModel != null && CurrentViewModel == CustomHomeVM) //CustomHomeView
                {
                    CustomHomeVM.TaskVM = TaskView;
                    TaskView?.ShowButtons(false);
                    CustomHomeVM.ViewOpened();
                }
                if (CurrentViewModel == GlobalSettingsView) //Settings
                {
                    if(Knossos.inSingleTCMode)
                    {
                        GlobalSettingsView?.CheckDisplaySettingsWarning();
                    }
                    Knossos.globalSettings.Load();
                    GlobalSettingsView?.LoadData();
                    //Knossos.globalSettings.EnableIniWatch();
                    GlobalSettingsView?.UpdateImgCacheSize();
                }
                else
                {
                    //Knossos.globalSettings.DisableIniWatch();
                }

                //Custom Views
                if (CurrentViewModel != null && CustomLauncher.IsCustomMode)
                {
                    if (CurrentViewModel.GetType() == typeof(HtmlContentViewModel))
                    {
                        ((HtmlContentViewModel)CurrentViewModel).Navigate();
                    }
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
            DeveloperModView?.AddMod(devmod);
        }

        /// <summary>
        /// Refresh Installed mods status icon/border
        /// </summary>
        public void RunModStatusChecks()
        {
            InstalledModsView?.RunModStatusChecks();
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
        public void MarkAsUpdateAvailable(string id, bool value = true, string? newVersion = null)
        {
            InstalledModsView?.UpdateIsAvailable(id, value);
            CustomHomeVM?.UpdateIsAvailable(id, value, newVersion);
        }

        /// <summary>
        /// Add a mod to the installed (home) tab
        /// </summary>
        /// <param name="modJson"></param>
        public void AddInstalledMod(Mod modJson)
        {
            InstalledModsView?.AddMod(modJson);
            CustomHomeVM?.AddModVersion(modJson);
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
            NebulaModsView?.AddMod(modJson);
            CustomHomeVM?.AddNebulaModVersion(modJson);
        }

        /// <summary>
        /// First load of Nebula mods
        /// </summary>
        /// <param name="mods"></param>
        /// <param name="clear"></param>
        public void BulkLoadNebulaMods(List<Mod> mods, bool clear)
        {
            if(clear)
                NebulaModsView?.ClearView();
            NebulaModsView?.AddMods(mods);
            if (CustomLauncher.IsCustomMode)
            {
                foreach (var item in mods)
                {
                    if(item.id == CustomLauncher.ModID)
                        CustomHomeVM?.AddNebulaModVersion(item);
                }
            }
        }

        /// <summary>
        /// Relay cancel order to the nebula modcard
        /// </summary>
        /// <param name="id"></param>
        public void CancelModInstall(string id)
        {
            NebulaModsView?.CancelModInstall(id);
            CustomHomeVM?.CancelModInstall(id);
        }

        /// <summary>
        /// Deletes a modcard from the installed (home) tab
        /// </summary>
        /// <param name="id"></param>
        public void RemoveInstalledMod(string id)
        {
            InstalledModsView?.RemoveMod(id);
            CustomHomeVM?.RemoveMod(id);
        }

        /// <summary>
        /// Remove a installed mod version from the modcard UI
        /// </summary>
        /// <param name="mod"></param>
        public void RemoveInstalledModVersion(Mod mod)
        {
            InstalledModsView?.RemoveModVersion(mod);
            CustomHomeVM?.RemoveInstalledModVersion(mod);
        }

        /// <summary>
        /// Load settings to settings tab
        /// </summary>
        public void GlobalSettingsLoadData()
        {
            GlobalSettingsView?.LoadData();
        }

        internal void ApplySettings()
        {
            Dispatcher.UIThread.Invoke(() => {
                IsMenuOpen = Knossos.globalSettings.mainMenuOpen;
                sharedSortType = Knossos.globalSettings.sortType;
                InstalledModsView?.ChangeSort(sharedSortType);
                if(NebulaModsView != null)
                    NebulaModsView.sortType = sharedSortType;
            });
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
            DebugView?.WriteToUIConsole(message);
        }

        /// <summary>
        /// Open screenshot folder button command
        /// </summary>
        internal void OpenScreenshotsFolder()
        {
            try
            {
                var path = Path.Combine(KnUtils.GetFSODataFolderPath(), "screenshots");
                Directory.CreateDirectory(path);
                KnUtils.OpenFolder(path);
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "MainWindowViewModel.OpenScreenshotsFolder", ex);
            }
        }

        internal void TriggerMenuCommand()
        {
            IsMenuOpen = !IsMenuOpen;
            Knossos.globalSettings.mainMenuOpen = IsMenuOpen;
        }

        /// <summary>
        /// Sets a mod id as "installing" so the proper info can be displayed on the UI
        /// </summary>
        public void SetInstalling(string id, CancellationTokenSource cancelToken)
        {
            NebulaModsView?.SetInstalling(id, cancelToken);
            CustomHomeVM?.SetInstalling(id, cancelToken);
        }
    }
}
