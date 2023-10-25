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
        public FsoBuild? build;
        private Mod? modJson;
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
        private bool avx2 = false;
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
        private bool isValid = false;
        [ObservableProperty]
        private bool isInstalled = false;
        [ObservableProperty]
        private bool isDownloading = false;
        [ObservableProperty]
        public float progressBarMax = 100;
        [ObservableProperty]
        public float progressBarCurrent = 0;
        [ObservableProperty]
        private bool isDevMode = false;
        [ObservableProperty]
        private IBrush backgroundColor = Brushes.Black;
        [ObservableProperty]
        private bool isDetailsButtonVisible = true;

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
            UpdataDisplayData(build);
        }

        private void UpdataDisplayData(FsoBuild build, bool skipInstalledCheck = false)
        {
            Date = build.date;
            IsInstalled = build.isInstalled;
            IsValid = false;
            if (build.id == "FSO")
            {
                //No detail button on official FSO builds
                IsDetailsButtonVisible = false;
            }
            else
            {
                IsDetailsButtonVisible = true;
            }

            IsDevMode = build.devMode;
            if (IsDevMode)
            {
                BackgroundColor = Brushes.DimGray;
            }

            if (IsInstalled || skipInstalledCheck)
            {
                foreach (var exe in build.executables)
                {
                    if (exe.isValid)
                    {
                        IsValid = true;
                        switch (exe.type)
                        {
                            case FsoExecType.Release: Release = true; break;
                            case FsoExecType.Debug: Debug = true; break;
                            case FsoExecType.Fred2Debug:
                            case FsoExecType.Fred2: Fred2 = true; break;
                            case FsoExecType.QtFredDebug:
                            case FsoExecType.QtFred: Qtfred = true; break;
                        }

                        switch (exe.arch)
                        {
                            case FsoExecArch.x86: X86 = true; break;
                            case FsoExecArch.x64: X64 = true; break;
                            case FsoExecArch.x86_avx2: Avx2 = true; X86 = true; break;
                            case FsoExecArch.x64_avx2: Avx2 = true; X64 = true; break;
                            case FsoExecArch.x86_avx: Avx = true; X86 = true; break;
                            case FsoExecArch.x64_avx: Avx = true; X64 = true; break;
                            case FsoExecArch.arm32: Arm32 = true; break;
                            case FsoExecArch.arm64: Arm64 = true; break;
                        }
                    }
                }
            }
            else
            {
                IsValid = true;
            }
        }

        internal async void ViewBuildDetails()
        {
            if (build != null)
            {
                if (modJson == null)
                {
                    modJson = await Nebula.GetModData(build.id, build.version);
                }
                if (MainWindow.instance != null && modJson != null)
                {
                    UpdataDisplayData(new FsoBuild(modJson), true);
                    var dialog = new ModDetailsView();
                    dialog.DataContext = new ModDetailsViewModel(modJson);
                    await dialog.ShowDialog<ModDetailsView?>(MainWindow.instance);
                }
                else
                {
                    await MessageBox.Show(MainWindow.instance, "An error has ocurred while trying to display build details.", "Modjson is null", MessageBox.MessageBoxButtons.OKCancel);
                }
            }
        }

        internal async void DeleteBuildCommand()
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

        public bool CompareIdAndVersionToMod(Mod mod)
        {
            if(mod.id == build!.id && mod.version == build.version)
            { 
                return true; 
            }
            return false;
        }

        internal void CancelDownloadCommand()
        {
            cancellationTokenSource?.Cancel();
            IsDownloading = false;
        }

        public async void DownloadBuildExternal(Mod  mod)
        {
            if (!IsDownloading && !IsInstalled)
            {
                IsDownloading = true;
                cancellationTokenSource = new CancellationTokenSource();
                FsoBuild? newBuild = await TaskViewModel.Instance?.InstallBuild(build!, this, mod)!;
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

        internal async void DownloadBuildCommand()
        {
            if (MainWindow.instance != null && build != null)
            {
                var result = await MessageBox.Show(MainWindow.instance, "This will download and install the FSO Build: " + build?.ToString() + ". Do you want to continue?", "Install FSO engine build", MessageBox.MessageBoxButtons.YesNo);
                if (result == MessageBox.MessageBoxResult.Yes)
                {
                    if (build != null)
                    {
                        if (modJson == null)
                        {
                            modJson = await Nebula.GetModData(build.id, build.version);
                        }
                        //Check compatibility
                        if (modJson != null)
                        {
                            var tempBuild = new FsoBuild(modJson);
                            UpdataDisplayData(tempBuild,true);
                            if (tempBuild != null)
                            {
                                if (IsValid)
                                {
                                    IsDownloading = true;
                                    cancellationTokenSource = new CancellationTokenSource();
                                    FsoBuild? newBuild = await TaskViewModel.Instance?.InstallBuild(build!, this, modJson)!;
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
                                else
                                {
                                    await MessageBox.Show(MainWindow.instance, "This build does not have any executables compatible with your operating system or CPU arch.", "Build is not valid for this computer", MessageBox.MessageBoxButtons.OK);
                                }
                            }
                        }
                        else
                        {
                            await MessageBox.Show(MainWindow.instance, "There was an error while getting build data, check the logs.", "Get modjson failed", MessageBox.MessageBoxButtons.OK);
                        }
                    }
                }
            }
        }

    }
}
