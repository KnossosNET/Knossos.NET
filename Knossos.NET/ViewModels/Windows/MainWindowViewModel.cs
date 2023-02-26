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
        /* UI Bindings, use the uppercase version, otherwise changes will not register */
        [ObservableProperty]
        private ModListViewModel installedModsView = new ModListViewModel();
        [ObservableProperty]
        private ModListViewModel nebulaModsView = new ModListViewModel();
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

        public MainWindowViewModel()
        {
            Knossos.SetMainView(this);
            Knossos.StartUp();
        }

        /* External Commands */
        public void ClearBasePathViews()
        {
            InstalledModsView?.ClearView();
            FsoBuildsView?.ClearView();
        }

        public void AddInstalledMod(Mod modJson)
        {
            InstalledModsView.AddMod(modJson);
        }

        public void RemoveInstalledMod(string id)
        {
            InstalledModsView.RemoveMod(id);
        }


        public void LoadAllBuilds()
        {
            FsoBuildsView.LoadAllBuilds();
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
        private void OpenLog()
        {
            if (File.Exists(SysInfo.GetKnossosDataFolderPath() + @"\Knossos_log.log"))
            {
                try
                {
                    var cmd = new Process();
                    cmd.StartInfo.FileName = SysInfo.GetKnossosDataFolderPath() + @"\Knossos_log.log";
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
                    MessageBox.Show(MainWindow.instance, "Log File " + SysInfo.GetKnossosDataFolderPath() + @"\Knossos_log.log not found.","File not found",MessageBox.MessageBoxButtons.OK);
            }
        }

        private void OpenSettings()
        {
            if (File.Exists(SysInfo.GetKnossosDataFolderPath() + @"\settings.json"))
            {
                try
                {
                    var cmd = new Process();
                    cmd.StartInfo.FileName = SysInfo.GetKnossosDataFolderPath() + @"\settings.json";
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
                    MessageBox.Show(MainWindow.instance, "Log File " + SysInfo.GetKnossosDataFolderPath() + @"\settings.json not found.", "File not found", MessageBox.MessageBoxButtons.OK);
            }
        }

        private void OpenFS2Log()
        {
            if (File.Exists(SysInfo.GetFSODataFolderPath() + @"\data\fs2_open.log"))
            {
                try
                {
                    var cmd = new Process();
                    cmd.StartInfo.FileName = SysInfo.GetFSODataFolderPath() + @"\data\fs2_open.log";
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
                    MessageBox.Show(MainWindow.instance, "Log File " + SysInfo.GetFSODataFolderPath() + @"\data\fs2_open.log not found.", "File not found", MessageBox.MessageBoxButtons.OK);
            }
        }

        private void OpenFS2Ini()
        {
            if (File.Exists(SysInfo.GetFSODataFolderPath() + @"\fs2_open.ini"))
            {
                try
                {
                    var cmd = new Process();
                    cmd.StartInfo.FileName = SysInfo.GetFSODataFolderPath() + @"\fs2_open.ini";
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
                    MessageBox.Show(MainWindow.instance, "Log File " + SysInfo.GetFSODataFolderPath() + @"\fs2_open.ini not found.", "File not found", MessageBox.MessageBoxButtons.OK);
            }
        }
    }
}
