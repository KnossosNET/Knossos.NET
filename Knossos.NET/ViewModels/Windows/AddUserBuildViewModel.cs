using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{ 
    /// <summary>
    /// Add user build view model
    /// This is on FsoBuilds tab -> Custom builds
    /// </summary>
    public partial class AddUserBuildViewModel : ViewModelBase
    {
        [ObservableProperty]
        internal string scanResults = string.Empty;
        [ObservableProperty]
        internal string release = string.Empty;
        [ObservableProperty]
        internal string debugFile = string.Empty;
        [ObservableProperty]
        internal string fred2 = string.Empty;
        [ObservableProperty]
        internal string fred2Debug = string.Empty;
        [ObservableProperty]
        internal string qtFred = string.Empty;
        [ObservableProperty]
        internal string qtFredDebug = string.Empty;
        [ObservableProperty]
        internal string buildVersion = string.Empty;
        [ObservableProperty]
        internal string buildName = string.Empty;
        [ObservableProperty]
        internal bool stage2 = false;
        [ObservableProperty]
        internal bool x86 = false;
        [ObservableProperty]
        internal bool x64 = false;
        [ObservableProperty]
        internal bool aVX = false;
        [ObservableProperty]
        internal bool aVX2 = false;
        [ObservableProperty]
        internal bool arm32 = false;
        [ObservableProperty]
        internal bool arm64 = false;
        [ObservableProperty]
        internal bool riscv32 = false;
        [ObservableProperty]
        internal bool riscv64 = false;
        [ObservableProperty]
        internal int copyProgress = 0;
        [ObservableProperty]
        internal int maxFiles = 100;
        [ObservableProperty]
        internal bool isCopying;

        [ObservableProperty]
        internal string buildNewPath = string.Empty;
        [ObservableProperty]
        internal string? selectedLibrary;

        internal IReadOnlyList<string> LibraryPaths { get; } = Knossos.GetLibraryPaths();
        internal bool ShowLibrarySelector => LibraryPaths.Count > 1;

        [ObservableProperty]
        internal bool collapseData = false;
        [ObservableProperty]
        internal bool collapseArch = false;
        [ObservableProperty]
        internal bool collapseExecs = false;

        private string buildId = string.Empty;
        private string? folderPath = null;
        private KnossosWindow? window = null;

        public AddUserBuildViewModel() => SelectedLibrary = LibraryPaths.FirstOrDefault();

        public AddUserBuildViewModel(KnossosWindow window) : this()
        {
            this.window = window;
        }

        partial void OnSelectedLibraryChanged(string? value) => UpdateBuildNewPath();

        private void UpdateBuildNewPath() =>
            BuildNewPath = SelectedLibrary != null && buildId.Length > 0
                ? Path.Combine(SelectedLibrary, "bin", buildId)
                : string.Empty;

        internal async void OpenFolderCommand()
        {
            try
            {
                if (await OpenDir() is { } sourceFolder)
                {
                    folderPath = sourceFolder;
                    ScanResults = "Folder:\n" + folderPath;
                    List<string> execs;
                    if (KnUtils.IsWindows)
                    {
                        execs=Directory.GetFiles(folderPath, "*.exe", SearchOption.AllDirectories).ToList();
                    }
                    else if (KnUtils.IsMacOS)
                    {
                        execs=Directory.GetDirectories(folderPath, "*.app", SearchOption.AllDirectories).ToList();
                    }
                    else
                    {
                        execs = Directory.GetDirectories(folderPath, "*.AppImage", SearchOption.AllDirectories).ToList();
                        var fileList = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
                        var filesNoExt = fileList.Where(path => Path.GetExtension(path).Length == 0 )?.ToList();
                        if(filesNoExt != null && filesNoExt.Any())
                            execs.AddRange(filesNoExt);
                    }
                    ScanResults += "\nDetected Executables: " + execs.Count();
                    foreach (string exe in execs)
                    {
                        var file = new FileInfo(exe);
                        if (file != null)
                        {
                            if (file.Name.ToLower().Contains("debug") || file.Name.ToLower().Contains("fastdbg"))
                            {
                                if (file.Name.ToLower().Contains("fs2_open"))
                                {
                                    DebugFile = Path.GetRelativePath(folderPath, exe);
                                    if (BuildVersion == string.Empty)
                                    {
                                        ParseVersion(file.Name);
                                    }
                                    ParseArch(file.Name);
                                }
                                else
                                {
                                    if (file.Name.ToLower().Contains("fred2_open"))
                                    {
                                        Fred2Debug = Path.GetRelativePath(folderPath, exe);
                                        if (BuildVersion == string.Empty)
                                        {
                                            ParseVersion(file.Name);
                                        }
                                        ParseArch(file.Name);
                                    }
                                    else
                                    {
                                        if (file.Name.ToLower().Contains("qtfred"))
                                        {
                                            QtFredDebug = Path.GetRelativePath(folderPath, exe);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (file.Name.ToLower().Contains("fs2_open"))
                                {
                                    Release = Path.GetRelativePath(folderPath, exe);
                                    if (BuildVersion == string.Empty)
                                    {
                                        ParseVersion(file.Name);
                                    }
                                    ParseArch(file.Name);
                                }
                                else
                                {
                                    if (file.Name.ToLower().Contains("fred2_open"))
                                    {
                                        Fred2 = Path.GetRelativePath(folderPath, exe);
                                        if (BuildVersion == string.Empty)
                                        {
                                            ParseVersion(file.Name);
                                        }
                                        ParseArch(file.Name);
                                    }
                                    else
                                    {
                                        if (file.Name.ToLower().Contains("qtfred"))
                                        {
                                            QtFred = Path.GetRelativePath(folderPath, exe);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    Stage2 = true;
                    buildId = @"user_build_" + KnUtils.GetTimestamp(DateTime.Now);
                    UpdateBuildNewPath();
                }
            }catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "AddUserBuildViewModel.OpenFolderCommand()", ex);
            }
        }

        private void ParseArch(string filename)
        {
            if(filename.ToLower().Contains("x86"))
            {
                X86 = true;
            }
            if (filename.ToLower().Contains("x64"))
            {
                X64 = true;
            }
            if (filename.ToLower().Contains("avx2"))
            {
                AVX2 = true;
            }else if (filename.ToLower().Contains("avx"))
            {
                AVX = true;
            }
            if (filename.ToLower().Contains("arm32"))
            {
                Arm32 = true;
            }
            if (filename.ToLower().Contains("arm64"))
            {
                Arm64 = true;
            }
            if (filename.ToLower().Contains("riscv64"))
            {
                Riscv64 = true;
            }
            if (filename.ToLower().Contains("riscv32"))
            {
                Riscv32 = true;
            }
        }

        private void ParseVersion(string filename)
        {
            var parts = filename.Split('_');
            if (parts.Length > 5)
            {
                BuildVersion = parts[2] + "."+ parts[3] + "." + parts[4];
            }
        }

        private async Task<string?> OpenDir()
        {
            FolderPickerOpenOptions options = new FolderPickerOpenOptions();
            options.AllowMultiple = false;
            options.Title = "Select the folder containing the FSO execs files";

            var topmostWindow = KnUtils.GetTopLevel();

            var result = await topmostWindow.StorageProvider.OpenFolderPickerAsync(options);
            if (result != null && result.Count > 0)
                return result[0].Path.LocalPath.ToString();
            else
                return null;
        }

        internal async void ChangeReleaseCommand()
        {
            var file = await GetPath(folderPath);
            if(file != null)
            {
                Release = file;
                if (BuildVersion == string.Empty)
                {
                    ParseVersion(file);
                }
                ParseArch(file);
            }
        }
        internal async void ChangeDebugCommand()
        {
            var file = await GetPath(folderPath);
            if (file != null)
            {
                DebugFile = file;
                if (BuildVersion == string.Empty)
                {
                    ParseVersion(file);
                }
                ParseArch(file);
            }
        }
        internal async void ChangeFred2Command()
        {
            var file = await GetPath(folderPath);
            if (file != null)
            {
                Fred2 = file;
                if (BuildVersion == string.Empty)
                {
                    ParseVersion(file);
                }
                ParseArch(file);
            }
        }
        internal async void ChangeFred2DebugCommand()
        {
            var file = await GetPath(folderPath);
            if (file != null)
            {
                Fred2Debug = file;
                if (BuildVersion == string.Empty)
                {
                    ParseVersion(file);
                }
                ParseArch(file);
            }
        }
        internal async void ChangeQtFredCommand()
        {
            var file = await GetPath(folderPath);
            if (file != null)
            {
                QtFred = file;
            }
        }
        internal async void ChangeQtFredDebugCommand()
        {
            var file = await GetPath(folderPath);
            if (file != null)
            {
                QtFredDebug = file;
            }
        }

        public async Task<string?> GetPath(string? folderRoot)
        {
            if(folderRoot != null)
            {
                FilePickerOpenOptions options = new FilePickerOpenOptions();
                options.AllowMultiple = false;
                options.Title = "Select the executable file";
                options.SuggestedStartLocation = await KnUtils.GetTopLevel().StorageProvider.TryGetFolderFromPathAsync(folderRoot);

                var topmostWindow = KnUtils.GetTopLevel();

                var result = await topmostWindow.StorageProvider.OpenFilePickerAsync(options);

                if (result != null && result.Count > 0)
                {
                    return Path.GetRelativePath(folderRoot, result[0].Path.LocalPath.ToString());
                }
            }
            return null;
        }

        internal async void AddCommand()
        {
            if(IsCopying || !Verify())
            {
                return;
            }
            
            if (folderPath != null)
            {
                var library = SelectedLibrary;
                var destination = BuildNewPath;
                if (library == null || !Knossos.GetLibraryPaths().Contains(library, Knossos.LibraryPathComparer) ||
                    !Directory.Exists(library))
                {
                    await MessageBox.Show(MainWindow.instance, "Choose an available library before importing this build.",
                        "Library unavailable", MessageBox.MessageBoxButtons.OK);
                    return;
                }
                if (!Knossos.IsPathInLibrary(library, destination) ||
                    KnUtils.IsSubPath(folderPath, Path.GetRelativePath(folderPath, destination)))
                {
                    await MessageBox.Show(MainWindow.instance, "The destination must be inside the selected library and outside the source folder.",
                        "Invalid import destination", MessageBox.MessageBoxButtons.OK);
                    return;
                }

                CollapseData = true;
                CollapseExecs = true;
                CollapseArch = true;
                if(await CopyBuildAsync(folderPath, destination))
                {
                    Mod mod = new Mod();
                    mod.fullPath = destination + Path.DirectorySeparatorChar;
                    mod.folderName = buildId;
                    mod.firstRelease = mod.lastUpdate = DateTime.Now.Year.ToString("0000") + "-" + DateTime.Now.Month.ToString("00") + "-" + DateTime.Now.Day.ToString("00");
                    mod.installed = true;
                    mod.id = buildId;
                    mod.title = BuildName;
                    mod.description = BuildName;
                    mod.type = ModType.engine;
                    mod.stability = "stable";
                    mod.modFlag.Add(buildId);
                    mod.version = BuildVersion;

                    var package = new ModPackage();
                    package.name = "user_build";
                    package.status = "required";
                    package.environment = "";
                    if(KnUtils.IsWindows)
                    {
                        package.environment += "windows";
                    }
                    else
                    {
                        if (KnUtils.IsLinux)
                        {
                            package.environment += "linux";
                        }
                        else
                        {
                            if (KnUtils.IsMacOS)
                            {
                                package.environment += "macosx";
                            }
                        }
                    }
                    if(X86)
                    {
                        package.environment += " && x86";
                    }
                    if (X64)
                    {
                        package.environment += " && x86_64";
                    }
                    if (AVX)
                    {
                        package.environment += " && avx";
                    }
                    if (AVX2)
                    {
                        package.environment += " && avx2";
                    }
                    if (Arm32)
                    {
                        package.environment += " && arm32";
                    }
                    if (Arm64)
                    {
                        package.environment += " && arm64";
                    }
                    if (Riscv32)
                    {
                        package.environment += " && riscv32";
                    }
                    if (Riscv64)
                    {
                        package.environment += " && riscv64";
                    }
                    package.folder = buildId;
                    package.isVp = false;
                    package.executables = new List<ModExecutable>();
                    package.checkNotes = "";
                    var properties = new ModProperties();
                    properties.arm64 = Arm64;
                    properties.arm32 = Arm32;
                    properties.riscv32 = Riscv32;
                    properties.riscv64 = Riscv64;
                    if (!Arm32 && !Arm64)
                    {
                        if (!AVX && !AVX2)
                        {
                            properties.sse2 = true;
                            properties.avx = false;
                            properties.avx2 = false;
                        }
                        else
                        {
                            properties.sse2 = false;
                            if (!AVX2)
                                properties.avx = true;
                            else
                                properties.avx2 = true;
                        }
                    }
                    properties.x64 = X64;

                    if(Release != string.Empty)
                    {
                        var newFile = new ModExecutable();
                        newFile.file = FsoBuild.GetRealExeName(folderPath, Release);
                        newFile.properties= properties;
                        package.executables.Add(newFile);
                    }
                    if (DebugFile != string.Empty)
                    {
                        var newFile = new ModExecutable();
                        newFile.file = FsoBuild.GetRealExeName(folderPath, DebugFile);
                        newFile.label = "FastDebug";
                        newFile.properties = properties;
                        package.executables.Add(newFile);
                    }
                    if (Fred2 != string.Empty)
                    {
                        var newFile = new ModExecutable();
                        newFile.file = FsoBuild.GetRealExeName(folderPath, Fred2);
                        newFile.label = "FRED2";
                        newFile.properties = properties;
                        package.executables.Add(newFile);
                    }
                    if (Fred2Debug != string.Empty)
                    {
                        var newFile = new ModExecutable();
                        newFile.file = FsoBuild.GetRealExeName(folderPath, Fred2Debug);
                        newFile.label = "Fred FastDebug";
                        newFile.properties = properties;
                        package.executables.Add(newFile);
                    }
                    if (QtFred != string.Empty)
                    {
                        var newFile = new ModExecutable();
                        newFile.file = FsoBuild.GetRealExeName(folderPath, QtFred);
                        newFile.label = "QTFred";
                        newFile.properties = properties;
                        package.executables.Add(newFile);
                    }
                    if (QtFredDebug != string.Empty)
                    {
                        var newFile = new ModExecutable();
                        newFile.file = FsoBuild.GetRealExeName(folderPath, QtFredDebug);
                        newFile.label = "QTFred FastDebug";
                        newFile.properties = properties;
                        package.executables.Add(newFile);
                    }
                    mod.packages.Add(package);
                    mod.SaveJson();

                    var userBuild = new FsoBuild(mod);
                    Knossos.AddBuild(userBuild);
                    FsoBuildsViewModel.Instance?.AddBuildToUi(userBuild);

                    Dispatcher.UIThread.Invoke(() => { window?.Close(); });
                }
                else
                {
                    CollapseData = false;
                    CollapseExecs = false;
                    CollapseArch = false;
                    await MessageBox.Show(null, "An error has ocurred while copying files", "Filecopy error", MessageBox.MessageBoxButtons.OK);
                }
            }
        }

        private async Task<bool> CopyBuildAsync(string source, string destination)
        {
            // The payload stays below the library scanner's discovery depth while copying.
            var staging = Path.Combine(Path.GetDirectoryName(destination)!, ".import-" + Guid.NewGuid().ToString("N"));
            var payload = Path.Combine(staging, "build");
            string[] ignoreList = { ".pdb", ".lib", ".exp", ".a", ".map", ".vp", ".vpc" };
            using var cancellation = new CancellationTokenSource();
            IsCopying = true;
            CopyProgress = 0;
            try
            {
                if (Directory.Exists(destination) || File.Exists(destination))
                    throw new IOException($"The destination folder '{destination}' already exists.");
                if (!await KnUtils.CopyDirectoryAsync(source, payload, true, cancellation, copyCallback, ignoreList))
                    return false;
                Directory.Move(payload, destination);
                return true;
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, nameof(CopyBuildAsync), ex);
                return false;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(staging))
                        Directory.Delete(staging, recursive: true);
                }
                catch (Exception ex)
                {
                    Log.Add(Log.LogSeverity.Warning, nameof(CopyBuildAsync), ex);
                }
                IsCopying = false;
            }
        }

        private bool Verify()
        { 
            if(!X86 && !X64 && !AVX && !Arm32 && !Arm64 && !Riscv32 && !Riscv64)
            {
                MessageBox.Show(MainWindow.instance, "You must select at least one cpu arch.", "Verify Fail", MessageBox.MessageBoxButtons.OK);
                return false;
            }
            if(Release == string.Empty && DebugFile == string.Empty && Fred2 == string.Empty && Fred2Debug == string.Empty && QtFred == string.Empty && QtFredDebug == string.Empty)
            {
                MessageBox.Show(MainWindow.instance, "You must select at least one executable", "Verify Fail", MessageBox.MessageBoxButtons.OK);
                return false;
            }
            try
            {
                new SemanticVersion(BuildVersion);
            }
            catch 
            {
                MessageBox.Show(MainWindow.instance, "Build version is not a valid semantic version string", "Verify Fail", MessageBox.MessageBoxButtons.OK);
                return false;
            }
            if(BuildName.Trim() == string.Empty)
            {
                MessageBox.Show(MainWindow.instance, "Build name is empty", "Verify Fail", MessageBox.MessageBoxButtons.OK);
                return false;
            }
            return true;
        }

        private async void copyCallback(string filename)
        {
            await Dispatcher.UIThread.InvokeAsync(() => {
                CopyProgress++;
            });
        }
    }
}
