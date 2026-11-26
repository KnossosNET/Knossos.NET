using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VP.NET;

namespace Knossos.NET.ViewModels
{
    internal enum ModFileOperation { Verify, Compress, Decompress }
    public enum CompressionScope { Active, All }

    public partial class ModLocalFilesViewModel : ViewModelBase, IDisposable
    {
        private readonly ModPropertiesViewModel owner;
        private readonly bool canOpenFolders;
        private CancellationTokenSource? measurement;
        private bool includeAllVersions;

        [ObservableProperty] private string libraryLocation = "Library not configured";
        [ObservableProperty] private string freeSpace = "—";
        [ObservableProperty] private string capacity = string.Empty;
        [ObservableProperty] private double percentFree;
        [ObservableProperty] private string activeSize = "—";
        [ObservableProperty] private string allVersionsSize = "—";
        [ObservableProperty] private string measurementError = string.Empty;
        [ObservableProperty] private bool isMeasuring;
        [ObservableProperty] private bool hasCompressedFiles;

        public ModPropertiesViewModel Properties => owner;
        public ModVersionsViewModel Operation => owner.Versions;
        public string Name => owner.ActiveVersion.title;
        public string Version => "Version " + owner.ActiveVersion.version;
        public bool ShowFolderButtons => canOpenFolders;
        public bool IsCompressed => owner.ActiveVersion.modSettings.isCompressed;
        public bool ShowDecompress => IsCompressed || HasCompressedFiles;
        public string CompressionLabel => ShowDecompress ? "Decompress Mod" : "Compress Mod";
        public string CompressionHint => ShowDecompress ? "Restore files to their original format." :
            Knossos.globalSettings.modCompression == CompressionSettings.Disabled ? "Compression is disabled in Settings." :
            "Save disk space with compression.";
        public bool HasMeasurementError => MeasurementError.Length > 0;
        partial void OnMeasurementErrorChanged(string value) => OnPropertyChanged(nameof(HasMeasurementError));

        public bool CanVerify => owner.CanConfigure && !Operation.IsApplying && !owner.IsSubmitting && TaskViewModel.Instance != null;
        public bool CanChangeCompression => CanVerify && (ShowDecompress ||
            Knossos.globalSettings.modCompression != CompressionSettings.Disabled);
        private string? ActiveLibrary => Knossos.GetLibraryForPath(owner.ActiveVersion.fullPath);
        public bool CanOpenLibrary => canOpenFolders && Directory.Exists(ActiveLibrary);
        public bool CanOpenMod => canOpenFolders && Directory.Exists(owner.ActiveVersion.fullPath);
        public bool CanMeasure => owner.CanConfigure && !IsMeasuring && !Operation.IsApplying;
        public Task CurrentMeasurement { get; private set; } = Task.CompletedTask;

        internal ModLocalFilesViewModel(ModPropertiesViewModel owner, bool? canOpenFolders = null)
        {
            this.owner = owner;
            this.canOpenFolders = canOpenFolders ?? (!KnUtils.IsAndroid && !KnUtils.IsBrowser);
            Operation.PropertyChanged += OperationChanged;
        }

        internal void Open()
        {
            LibraryLocation = ActiveLibrary ?? "No configured library for this version";
            RefreshCommands();
            if (!IsMeasuring && !Operation.IsApplying)
                CurrentMeasurement = MeasureAsync();
        }

        internal void VersionChanged()
        {
            StopMeasuring();
            ActiveSize = AllVersionsSize = "—";
            LibraryLocation = ActiveLibrary ?? "No configured library for this version";
            FreeSpace = "—";
            Capacity = string.Empty;
            PercentFree = 0;
            HasCompressedFiles = false;
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Version));
            RefreshCommands();
        }

        internal void OperationCompleted()
        {
            RefreshCommands();
            if (owner.IsFilesSelected && owner.CanConfigure)
                CurrentMeasurement = MeasureAsync();
            else
                ActiveSize = AllVersionsSize = "—";
        }

        private void OperationChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ModVersionsViewModel.IsApplying)) return;
            if (Operation.IsApplying) StopMeasuring();
            RefreshCommands();
        }

        internal void RefreshCommands()
        {
            OnPropertyChanged(nameof(IsCompressed));
            OnPropertyChanged(nameof(CompressionLabel));
            OnPropertyChanged(nameof(CompressionHint));
            VerifyCommand.NotifyCanExecuteChanged();
            ChangeCompressionCommand.NotifyCanExecuteChanged();
            OpenLibraryCommand.NotifyCanExecuteChanged();
            OpenModCommand.NotifyCanExecuteChanged();
            MeasureAllCommand.NotifyCanExecuteChanged();
        }

        partial void OnHasCompressedFilesChanged(bool value) => RefreshCommands();
        partial void OnIsMeasuringChanged(bool value) => MeasureAllCommand.NotifyCanExecuteChanged();

        [RelayCommand(CanExecute = nameof(CanOpenLibrary))]
        private void OpenLibrary()
        {
            if (ActiveLibrary is { } library) KnUtils.OpenFolder(library);
        }

        [RelayCommand(CanExecute = nameof(CanOpenMod))]
        private void OpenMod() => KnUtils.OpenFolder(owner.ActiveVersion.fullPath);

        [RelayCommand(CanExecute = nameof(CanVerify))]
        private Task VerifyAsync() => Operation.RunFileOperationAsync(owner.ActiveVersion, ModFileOperation.Verify);

        [RelayCommand(CanExecute = nameof(CanChangeCompression))]
        private async Task ChangeCompressionAsync()
        {
            var active = owner.ActiveVersion;
            var compress = !ShowDecompress;
            var installed = Knossos.GetInstalledModList(active.id)
                .Where(mod => mod.installed && mod.type != ModType.engine)
                .DistinctBy(mod => mod.version)
                .OrderByDescending(mod => mod.version == active.version)
                .ToArray();

            var scope = installed.Length > 1
                ? await owner.ChooseCompressionScopeAsync(active, compress)
                : CompressionScope.Active;
            if (scope == null || !ReferenceEquals(active, owner.ActiveVersion) || !CanChangeCompression) return;

            var selected = scope == CompressionScope.All ? installed : new[] { active };
            if (compress)
            {
                // Warnings apply only to versions selected for compression that are not already compressed.
                var candidates = selected.Where(mod => !mod.modSettings.isCompressed).ToArray();
                if (!await ConfirmCompressionAsync(candidates)) return;
            }

            if (ReferenceEquals(active, owner.ActiveVersion) && CanChangeCompression)
                await Operation.RunFileOperationsAsync(selected, compress ? ModFileOperation.Compress : ModFileOperation.Decompress);
        }

        private Task<bool> ConfirmCompressionAsync(IReadOnlyList<Mod> candidates)
        {
            var warnings = new List<string>();
            var developerVersions = candidates.Where(mod => mod.devMode).Select(mod => mod.version).ToArray();
            if (developerVersions.Length > 0)
                warnings.Add("Developer versions " + string.Join(", ", developerVersions) +
                    " may take more space to upload after compression.");

            foreach (var mod in candidates)
            {
                var build = mod.ResolveInstalledBuild();
                if (build != null && SemanticVersion.Compare(build.version, VPCompression.MinimumFSOVersion) < 0)
                    warnings.Add("Version " + mod.version + " uses FSO " + build.version +
                        ". Full compression support requires FSO " + VPCompression.MinimumFSOVersion + " or newer.");
            }

            return warnings.Count == 0 ? Task.FromResult(true) :
                owner.ConfirmAsync("Continue with compression?", string.Join("\n\n", warnings), "Compress");
        }

        [RelayCommand(CanExecute = nameof(CanMeasure))]
        private Task MeasureAllAsync()
        {
            includeAllVersions = true;
            return CurrentMeasurement = MeasureAsync();
        }

        private async Task MeasureAsync()
        {
            StopMeasuring();
            using var source = new CancellationTokenSource();
            measurement = source;
            var token = source.Token;
            IsMeasuring = true;
            MeasurementError = string.Empty;

            var active = owner.ActiveVersion;
            var library = ActiveLibrary;
            LibraryLocation = library ?? "No configured library for this version";
            var all = includeAllVersions;
            var versions = all ? Knossos.GetInstalledModList(active.id).Append(active)
                .Where(mod => mod.installed && !string.IsNullOrWhiteSpace(mod.fullPath))
                .DistinctBy(mod => Path.TrimEndingDirectorySeparator(mod.fullPath), KnUtils.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
                .ToArray() : Array.Empty<Mod>();
            try
            {
                var drive = await Task.Run(() => ReadLibraryDrive(library), token);
                token.ThrowIfCancellationRequested();
                FreeSpace = drive.HasValue ? KnUtils.FormatBytes(drive.Value.Free) : "—";
                Capacity = drive.HasValue ? "of " + KnUtils.FormatBytes(drive.Value.Total) : string.Empty;
                PercentFree = drive is { Total: > 0 } space ? Math.Clamp(100.0 * space.Free / space.Total, 0, 100) : 0;

                var (activeBytes, compressedFiles) = await Task.Run(() => MeasureVersion(active, token), token);
                token.ThrowIfCancellationRequested();
                HasCompressedFiles = compressedFiles;
                ActiveSize = KnUtils.FormatBytes(activeBytes);
                if (all)
                {
                    var total = await Task.Run(() => versions.Sum(mod => ReferenceEquals(mod, active) ? activeBytes : MeasureVersion(mod, token).Bytes), token);
                    token.ThrowIfCancellationRequested();
                    AllVersionsSize = KnUtils.FormatBytes(total);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    MeasurementError = "Could not read the installation size. Check the library path and try again.";
                    Log.Add(Log.LogSeverity.Warning, "ModLocalFiles.Measure", ex);
                }
            }
            finally
            {
                if (ReferenceEquals(measurement, source))
                {
                    measurement = null;
                    IsMeasuring = false;
                }
            }
        }

        private static (long Free, long Total)? ReadLibraryDrive(string? library)
        {
            if (!Directory.Exists(library)) return null;
            try
            {
#if ANDROID
                using var stats = new Android.OS.StatFs(library!);
                return (stats.AvailableBytes, stats.TotalBytes);
#else
                var location = Path.TrimEndingDirectorySeparator(Path.GetFullPath(library!));
                var comparison = KnUtils.IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                var drive = DriveInfo.GetDrives()
                    .Where(candidate => location.Equals(Path.TrimEndingDirectorySeparator(candidate.Name), comparison) ||
                        location.StartsWith(Path.TrimEndingDirectorySeparator(candidate.Name) + Path.DirectorySeparatorChar, comparison) ||
                        location.StartsWith(candidate.Name, comparison) && Path.EndsInDirectorySeparator(candidate.Name))
                    .MaxBy(candidate => candidate.Name.Length) ?? new DriveInfo(location);
                return drive.IsReady ? (drive.AvailableFreeSpace, drive.TotalSize) : null;
#endif
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "ModLocalFiles.LibrarySpace", ex);
                return null;
            }
        }

        private static (long Bytes, bool HasCompressedFiles) MeasureVersion(Mod mod, CancellationToken token)
        {
            bool hasCompressedFiles = false;
            long SumFolder(string folder, bool recursive)
            {
                long bytes = 0;
                foreach (var file in new DirectoryInfo(folder).EnumerateFiles("*", new EnumerationOptions
                {
                    RecurseSubdirectories = recursive,
                    AttributesToSkip = FileAttributes.ReparsePoint
                }))
                {
                    token.ThrowIfCancellationRequested();
                    bytes += file.Length;
                    hasCompressedFiles |= file.Extension.Equals(".lz41", StringComparison.OrdinalIgnoreCase) ||
                        file.Extension.Equals(".vpc", StringComparison.OrdinalIgnoreCase);
                }
                return bytes;
            }

            token.ThrowIfCancellationRequested();
            if (mod.id != "FS2") return (SumFolder(mod.fullPath, true), hasCompressedFiles);

            // The retail FS2 root can also contain other separately installed mods.
            var data = Path.Combine(mod.fullPath, "data");
            var bytes = SumFolder(mod.fullPath, false) + (Directory.Exists(data) ? SumFolder(data, true) : 0);
            return (bytes, hasCompressedFiles);
        }

        internal void StopMeasuring()
        {
            var previous = measurement;
            measurement = null;
            previous?.Cancel();
            IsMeasuring = false;
        }

        public void Dispose()
        {
            StopMeasuring();
            Operation.PropertyChanged -= OperationChanged;
        }
    }
}
