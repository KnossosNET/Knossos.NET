using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Knossos.NET.ViewModels
{
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
        internal string uiConsoleOutput = string.Empty;

        internal string sharedSearch = string.Empty;

        public enum SortType
        {
            name,
            release,
            update
        }

        internal SortType sharedSortType = SortType.name;
        internal int tabIndex = 0;
        internal int TabIndex
        {
            get => tabIndex;
            set
            {
                /* Execute code when user changes tab */
                if (value != tabIndex)
                {
                    // Things to do on tab exit
                    if (tabIndex == 0) //Exiting the Play tab.
                    {
                        sharedSearch = InstalledModsView.Search;

                        // Change and save to the new sort type.
                        if (sharedSortType != InstalledModsView.sortType)
                        {
                            sharedSortType = InstalledModsView.sortType;
                            Knossos.globalSettings.Save();
                        }
                    }
                    if (tabIndex == 1) //Exiting the Nebula tab.
                    {
                        sharedSearch = NebulaModsView.Search;

                        if (sharedSortType != NebulaModsView.sortType)
                        {
                            sharedSortType = NebulaModsView.sortType;
                            Knossos.globalSettings.Save();
                        }
                    }

                    // Things to do on tab entrance
                    this.SetProperty(ref tabIndex, value);
                    if (tabIndex == 0) //Play Tab
                    {
                        InstalledModsView.Search = sharedSearch;
                        InstalledModsView.ChangeSort(sharedSortType);
                    }
                    if (tabIndex == 1) //Nebula Mods
                    {
                        NebulaModsView.OpenTab(sharedSearch, sharedSortType);
                    }
                    if (tabIndex == 4) //PXO
                    {
                        PxoViewModel.Instance!.InitialLoad();
                    }
                    if (tabIndex == 5) //Settings
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
        }

        public MainWindowViewModel()
        {
            Instance = this;
            string[] args = Environment.GetCommandLineArgs();
            bool isQuickLaunch = false;
            foreach (var arg in args)
            {
                if (arg.ToLower() == "-playmod")
                {
                    isQuickLaunch = true;
                }
            }
            Knossos.StartUp(isQuickLaunch);
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
        /// Informs in the installed mod card that a update is avalible
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        public void MarkAsUpdateAvalible(string id, bool value = true)
        {
            InstalledModsView.UpdateIsAvalible(id, value);
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
        /// Load settings to settings tab
        /// </summary>
        public void GlobalSettingsLoadData()
        {
            GlobalSettingsView.LoadData();
        }

        /// <summary>
        /// Write a string to UI console on debug tab
        /// </summary>
        /// <param name="message"></param>
        public void WriteToUIConsole(string message)
        {
            UiConsoleOutput += "\n"+ message;
        }

        /* Debug Section */
        internal void OpenLog()
        {
            if (File.Exists(KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "Knossos.log"))
            {
                try
                {
                    var cmd = new Process();
                    cmd.StartInfo.FileName = KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "Knossos.log";
                    cmd.StartInfo.UseShellExecute = true;
                    cmd.Start();
                    cmd.Dispose();
                }
                catch (Exception ex) 
                {
                    Log.Add(Log.LogSeverity.Error, "MainWindowViewModel.ReloadLog",ex);
                }
            }
            else
            {
                if(MainWindow.instance != null)
                    MessageBox.Show(MainWindow.instance, "Log File " + KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "Knossos.log not found.","File not found",MessageBox.MessageBoxButtons.OK);
            }
        }

        internal void OpenSettings()
        {
            if (File.Exists(KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "settings.json"))
            {
                try
                {
                    var cmd = new Process();
                    cmd.StartInfo.FileName = KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar +"settings.json";
                    cmd.StartInfo.UseShellExecute = true;
                    cmd.Start();
                    cmd.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Add(Log.LogSeverity.Error, "MainWindowViewModel.ReloadLog", ex);
                }
            }
            else
            {
                if (MainWindow.instance != null)
                    MessageBox.Show(MainWindow.instance, "Log File " + KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "settings.json not found.", "File not found", MessageBox.MessageBoxButtons.OK);
            }
        }

        internal void OpenFS2Log()
        {
            if (File.Exists(KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar + "fs2_open.log"))
            {
                try
                {
                    var cmd = new Process();
                    cmd.StartInfo.FileName = KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "data"+ Path.DirectorySeparatorChar + "fs2_open.log";
                    cmd.StartInfo.UseShellExecute = true;
                    cmd.Start();
                    cmd.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Add(Log.LogSeverity.Error, "MainWindowViewModel.ReloadFS2Log", ex);
                }
            }
            else
            {
                if (MainWindow.instance != null)
                    MessageBox.Show(MainWindow.instance, "Log File " + KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar+ "data"+ Path.DirectorySeparatorChar+"fs2_open.log not found.", "File not found", MessageBox.MessageBoxButtons.OK);
            }
        }

        internal void OpenFS2Ini()
        {
            if (File.Exists(KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar+ "fs2_open.ini"))
            {
                try
                {
                    var cmd = new Process();
                    cmd.StartInfo.FileName = KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "fs2_open.ini";
                    cmd.StartInfo.UseShellExecute = true;
                    cmd.Start();
                    cmd.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Add(Log.LogSeverity.Error, "MainWindowViewModel.ReloadFS2ini", ex);
                }
            }
            else
            {
                if (MainWindow.instance != null)
                    MessageBox.Show(MainWindow.instance, "Log File " + KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "fs2_open.ini not found.", "File not found", MessageBox.MessageBoxButtons.OK);
            }
        }

        internal async void UploadFS2Log()
        {
            if (File.Exists(KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar + "fs2_open.log"))
            {
                try
                {
                    var logString = File.ReadAllText(KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar + "fs2_open.log",System.Text.Encoding.UTF8);
                    if(logString.Trim() != string.Empty)
                    {
                        var status = await Nebula.UploadLog(logString);
                        if(!status)
                        {
                            if (MainWindow.instance != null)
                                await MessageBox.Show(MainWindow.instance, "An error has ocurred while uploading the log file, check the log below.", "Upload log error", MessageBox.MessageBoxButtons.OK);
                        }
                    }
                    else
                    {
                        if (MainWindow.instance != null)
                            await MessageBox.Show(MainWindow.instance, "The log file is empty.", "Error", MessageBox.MessageBoxButtons.OK);
                    }
                }
                catch (Exception ex)
                {
                    Log.Add(Log.LogSeverity.Error, "MainWindowViewModel.UploadFS2Log", ex);
                }
            }
            else
            {
                if (MainWindow.instance != null)
                    await MessageBox.Show(MainWindow.instance, "Log File " + KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar + "fs2_open.log not found.", "File not found", MessageBox.MessageBoxButtons.OK);
            }
        }
        internal async void UploadKnossosConsole()
        {
            try
            {
                var status = await Nebula.UploadLog(UiConsoleOutput);
                if (!status)
                {
                    if (MainWindow.instance != null)
                        await MessageBox.Show(MainWindow.instance, "An error has ocurred while uploading the console output, check the log below.", "Upload error", MessageBox.MessageBoxButtons.OK);
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "MainWindowViewModel.UploadKnossosConsole", ex);
            }
        }
    }
}
