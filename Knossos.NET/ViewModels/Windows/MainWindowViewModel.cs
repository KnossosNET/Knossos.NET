using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System;
using System.Diagnostics;
using System.IO;

namespace Knossos.NET.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase 
    {
        public static MainWindowViewModel? Instance { get; set; }

        /* UI Bindings, use the uppercase version, otherwise changes will not register */
        [ObservableProperty]
        private string appTitle = "Knossos.NET v" + Knossos.AppVersion;
        [ObservableProperty]
        private ModListViewModel installedModsView = new ModListViewModel(false);
        [ObservableProperty]
        private ModListViewModel nebulaModsView = new ModListViewModel(true);
        [ObservableProperty]
        private FsoBuildsViewModel fsoBuildsView = new FsoBuildsViewModel();
        [ObservableProperty]
        private DeveloperModsViewModel developerModView = new DeveloperModsViewModel();
        [ObservableProperty]
        private PxoViewModel pxoView = new PxoViewModel();
        [ObservableProperty]
        private GlobalSettingsViewModel globalSettingsView = new GlobalSettingsViewModel();
        [ObservableProperty]
        private TaskViewModel taskView = new TaskViewModel();
        [ObservableProperty]
        private string uiConsoleOutput = string.Empty;

        private int tabIndex = 0;
        private int TabIndex
        {
            get => tabIndex;
            set
            {
                if (value != tabIndex)
                {
                    this.SetProperty(ref tabIndex, value);
                    if(tabIndex == 4) //PXO
                    {
                        PxoViewModel.Instance!.InitialLoad();
                    }
                    if (tabIndex == 5) //Settings
                    {
                        Knossos.globalSettings.Load();
                        GlobalSettingsView.LoadData();
                        Knossos.globalSettings.EnableIniWatch();
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
        public void RunDependenciesCheck()
        {
            InstalledModsView.RunDependencyCheck();
        }

        public void ClearViews()
        {
            InstalledModsView?.ClearView();
            FsoBuildsView?.ClearView();
            NebulaModsView?.ClearView();
        }

        public void MarkAsUpdateAvalible(string id, bool value = true)
        {
            InstalledModsView.UpdateIsAvalible(id, value);
        }

        public void AddInstalledMod(Mod modJson)
        {
            InstalledModsView.AddMod(modJson);
        }

        public void AddNebulaMod(Mod modJson)
        {
            NebulaModsView.AddMod(modJson);
        }

        public void CancelModInstall(string id)
        {
            NebulaModsView.CancelModInstall(id);
        }

        public void RemoveInstalledMod(string id)
        {
            InstalledModsView.RemoveMod(id);
        }

        public void GlobalSettingsLoadData()
        {
            GlobalSettingsView.LoadData();
        }

        public void WriteToUIConsole(string message)
        {
            UiConsoleOutput += "\n"+ message;
        }

        /* Debug Section */
        internal void OpenLog()
        {
            if (File.Exists(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "Knossos.log"))
            {
                try
                {
                    var cmd = new Process();
                    cmd.StartInfo.FileName = SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "Knossos.log";
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
                    MessageBox.Show(MainWindow.instance, "Log File " + SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "Knossos.log not found.","File not found",MessageBox.MessageBoxButtons.OK);
            }
        }

        internal void OpenSettings()
        {
            if (File.Exists(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "settings.json"))
            {
                try
                {
                    var cmd = new Process();
                    cmd.StartInfo.FileName = SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar +"settings.json";
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
                    MessageBox.Show(MainWindow.instance, "Log File " + SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "settings.json not found.", "File not found", MessageBox.MessageBoxButtons.OK);
            }
        }

        internal void OpenFS2Log()
        {
            if (File.Exists(SysInfo.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar + "fs2_open.log"))
            {
                try
                {
                    var cmd = new Process();
                    cmd.StartInfo.FileName = SysInfo.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "data"+ Path.DirectorySeparatorChar + "fs2_open.log";
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
                    MessageBox.Show(MainWindow.instance, "Log File " + SysInfo.GetFSODataFolderPath() + Path.DirectorySeparatorChar+ "data"+ Path.DirectorySeparatorChar+"fs2_open.log not found.", "File not found", MessageBox.MessageBoxButtons.OK);
            }
        }

        internal void OpenFS2Ini()
        {
            if (File.Exists(SysInfo.GetFSODataFolderPath() + Path.DirectorySeparatorChar+ "fs2_open.ini"))
            {
                try
                {
                    var cmd = new Process();
                    cmd.StartInfo.FileName = SysInfo.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "fs2_open.ini";
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
                    MessageBox.Show(MainWindow.instance, "Log File " + SysInfo.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "fs2_open.ini not found.", "File not found", MessageBox.MessageBoxButtons.OK);
            }
        }

        internal async void UploadFS2Log()
        {
            if (File.Exists(SysInfo.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar + "fs2_open.log"))
            {
                try
                {
                    var logString = File.ReadAllText(SysInfo.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar + "fs2_open.log",System.Text.Encoding.UTF8);
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
                    await MessageBox.Show(MainWindow.instance, "Log File " + SysInfo.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar + "fs2_open.log not found.", "File not found", MessageBox.MessageBoxButtons.OK);
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
