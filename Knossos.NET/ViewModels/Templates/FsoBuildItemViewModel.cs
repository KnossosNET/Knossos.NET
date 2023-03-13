using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System.Diagnostics;
using System.Threading;

namespace Knossos.NET.ViewModels
{
    public partial class FsoBuildItemViewModel : ViewModelBase
    {
        private FsoBuild? build;
        private FsoBuildsViewModel? buildsView;
        public CancellationTokenSource? cancellationTokenSource = null;
        [ObservableProperty]
        private string title;
        [ObservableProperty]
        private string? date;
        [ObservableProperty]
        private bool x86 = false;
        [ObservableProperty]
        private bool x64 = false;
        [ObservableProperty]
        private bool avx = false;
        [ObservableProperty]
        private bool arm32 = false;
        [ObservableProperty]
        private bool arm64 = false;
        [ObservableProperty]
        private bool release = false;
        [ObservableProperty]
        private bool debug = false;
        [ObservableProperty]
        private bool fred2 = false;
        [ObservableProperty]
        private bool qtfred = false;
        [ObservableProperty]
        private string? tooltip = null;
        [ObservableProperty]
        private bool isValid = false;
        [ObservableProperty]
        private bool isInstalled = false;
        [ObservableProperty]
        private bool isDownloading = false;
        [ObservableProperty]
        public float progressBarMax = 100;
        [ObservableProperty]
        public float progressBarCurrent = 0;

        public FsoBuildItemViewModel() 
        {
            title = "Test Build Title";
            date = "19/12/20";
        }

        public FsoBuildItemViewModel (FsoBuild build, FsoBuildsViewModel view)
        {
            this.build = build;
            buildsView = view;
            title = build.ToString();
            date = build.date;
            isInstalled = build.isInstalled;
            if(build.stability == FsoStability.Custom)
            {
                Tooltip = build.description;
            }

            foreach(var exe in build.executables)
            {
                if(exe.isValid)
                {
                    isValid = true;
                    switch(exe.type)
                    {
                        case FsoExecType.Release: release = true; break;
                        case FsoExecType.Debug: debug = true; break;
                        case FsoExecType.Fred2Debug:
                        case FsoExecType.Fred2: fred2 = true; break;
                        case FsoExecType.QtFredDebug:
                        case FsoExecType.QtFred: qtfred = true; break;
                    }
                    
                    switch(exe.arch)
                    {
                        case FsoExecArch.x86: x86 = true; break;
                        case FsoExecArch.x64: x64 = true; break;
                        case FsoExecArch.x86_avx: avx = true; x86 = true; break;
                        case FsoExecArch.x64_avx: avx = true; x64 = true; break;
                        case FsoExecArch.arm32: arm32 = true; break;
                        case FsoExecArch.arm64: arm64 = true; break;
                    }
                }
            }
            if(build.folderPath != string.Empty)
                tooltip = build.folderPath;
        }

        private async void DeleteBuildCommand()
        {
            if (MainWindow.instance != null && build != null && buildsView != null)
            {
                var resp = await MessageBox.Show(MainWindow.instance, "Deleting FSO build: " + build.ToString() + ". This can't be undone.", "Delete FSO build", MessageBox.MessageBoxButtons.OKCancel);
                if(resp == MessageBox.MessageBoxResult.OK)
                {
                    Log.Add(Log.LogSeverity.Information, "FsoBuildItemViewModel.DeleteBuildCommand()", "Deleting FSO build " + build.ToString());
                    buildsView.DeleteBuild(build,this);
                    var result = await Nebula.GetModData(build.id, build.version);
                    if (result != null)
                    {
                        buildsView.AddBuildToUi(new FsoBuild(result));
                    }
                }
            }
        }

        private void CancelDownloadCommand()
        {
            cancellationTokenSource?.Cancel();
            IsDownloading = false;
        }

        private async void DownloadBuildCommand()
        {
            if (MainWindow.instance != null && build != null)
            {
                var result = await MessageBox.Show(MainWindow.instance, "This will download and install the FSO Build: " + build?.ToString() + ". Do you want to continue?", "Install FSO engine build", MessageBox.MessageBoxButtons.YesNo);
                if (result == MessageBox.MessageBoxResult.Yes)
                {
                    IsDownloading = true;
                    cancellationTokenSource = new CancellationTokenSource();
                    FsoBuild? newBuild = await TaskViewModel.Instance?.InstallBuild(build!,this)!;
                    if (newBuild != null)
                    {
                        //Install completed
                        IsInstalled = true;
                        build = newBuild;
                    }
                    IsDownloading = false;
                    cancellationTokenSource?.Dispose();
                    cancellationTokenSource = null;
                }
            }
        }

    }
}
