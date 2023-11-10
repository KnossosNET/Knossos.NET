using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System.Threading;

namespace Knossos.NET.ViewModels
{
    public partial class FsoBuildItemViewModel : ViewModelBase
    {
        public FsoBuild? build;
        public CancellationTokenSource? cancellationTokenSource = null;
        [ObservableProperty]
        public string title = string.Empty;
        [ObservableProperty]
        public string? date;
        [ObservableProperty]
        internal string cpuArch = string.Empty;
        [ObservableProperty]
        internal string buildType = string.Empty;
        [ObservableProperty]
        internal bool isValid = false;
        [ObservableProperty]
        internal bool isInstalled = false;
        [ObservableProperty]
        internal bool isDownloading = false;
        [ObservableProperty]
        internal bool isDevMode = false;
        [ObservableProperty]
        internal bool isDetailsButtonVisible = true;

        public FsoBuildItemViewModel() 
        {
            IsValid = true;
            Title = "Test Build Title";
            Date = "19/12/20";
            CpuArch = "x86";
            BuildType = "Release, Fred2";
        }

        public FsoBuildItemViewModel (FsoBuild build)
        {
            this.build = build;
            title = build.ToString();
            UpdataDisplayData(build);
        }

        private void UpdataDisplayData(FsoBuild build, bool skipInstalledCheck = false)
        {
            Date = build.date;
            IsInstalled = build.isInstalled;
            IsValid = false;
            CpuArch = "";
            BuildType = "";
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

            if (IsInstalled || skipInstalledCheck)
            {
                foreach (var exe in build.executables)
                {
                    if (exe.isValid)
                    {
                        IsValid = true;
                        switch (exe.arch)
                        {
                            case FsoExecArch.x86: if (!CpuArch.Contains("X86")) CpuArch += "X86   "; break;
                            case FsoExecArch.x64: if (!CpuArch.Contains("X64")) CpuArch += "X64   "; break;
                            case FsoExecArch.x86_avx2: if (!CpuArch.Contains("X86")) CpuArch += "X86   "; if (!CpuArch.Contains("AVX2 ")) CpuArch += "AVX2   "; break;
                            case FsoExecArch.x64_avx2: if (!CpuArch.Contains("X64")) CpuArch += "X64   "; if (!CpuArch.Contains("AVX2 ")) CpuArch += "AVX2   "; break;
                            case FsoExecArch.x86_avx: if (!CpuArch.Contains("X86")) CpuArch += "X86   "; if (!CpuArch.Contains("AVX ")) CpuArch += "AVX   "; break;
                            case FsoExecArch.x64_avx: if (!CpuArch.Contains("X64")) CpuArch += "X64   "; if (!CpuArch.Contains("AVX ")) CpuArch += "AVX   "; break;
                            case FsoExecArch.arm32: if (!CpuArch.Contains("ARM32")) CpuArch += "ARM32   "; break;
                            case FsoExecArch.arm64: if (!CpuArch.Contains("ARM64")) CpuArch += "ARM64   "; break;
                        }

                        if (!BuildType.Contains(exe.type.ToString()))
                            BuildType += exe.type.ToString() + "  ";
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
                if (MainWindow.instance != null && build.modData != null)
                {
                    await build.modData.LoadFulLNebulaData();
                    UpdataDisplayData(new FsoBuild(build.modData), true);
                    var dialog = new ModDetailsView();
                    dialog.DataContext = new ModDetailsViewModel(build.modData, dialog);
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
            if (MainWindow.instance != null && build != null)
            {
                var resp = await MessageBox.Show(MainWindow.instance, "Deleting FSO build: " + build.ToString() + ". This can't be undone.", "Delete FSO build", MessageBox.MessageBoxButtons.OKCancel);
                if(resp == MessageBox.MessageBoxResult.OK)
                {
                    Log.Add(Log.LogSeverity.Information, "FsoBuildItemViewModel.DeleteBuildCommand()", "Deleting FSO build " + build.ToString());
                    FsoBuildsViewModel.Instance!.DeleteBuild(build,this);
                    var result = await Nebula.GetModData(build.id, build.version);
                    if (result != null)
                    {
                        FsoBuildsViewModel.Instance!.AddBuildToUi(new FsoBuild(result));
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
                await mod.LoadFulLNebulaData();
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
                        //Check compatibility
                        if (build.modData != null)
                        {
                            await build.modData.LoadFulLNebulaData();
                            var tempBuild = new FsoBuild(build.modData);
                            UpdataDisplayData(tempBuild,true);
                            if (tempBuild != null)
                            {
                                if (IsValid)
                                {
                                    IsDownloading = true;
                                    cancellationTokenSource = new CancellationTokenSource();
                                    FsoBuild? newBuild = await TaskViewModel.Instance?.InstallBuild(build!, this, build.modData)!;
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
