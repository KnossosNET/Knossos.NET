using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
    public partial class ModPropertiesViewModel
    {
        [ObservableProperty] private bool isMoveModal;
        [ObservableProperty] private bool canMoveAllVersions;
        [ObservableProperty] private bool moveAllVersions;
        [ObservableProperty] private IReadOnlyList<string> moveLibraries = Array.Empty<string>();
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmMoveCommand))]
        private string? selectedMoveLibrary;

        private bool CanMove() =>
            !disposed && !Versions.IsApplying && !IsSubmitting &&
            Knossos.GetInstalledMod(ActiveVersion.id, ActiveVersion.version) is { } installed &&
            Knossos.GetLibraryForPath(installed.fullPath) is { } source &&
            Knossos.GetLibraryPaths().Any(path => !Knossos.LibraryPathComparer.Equals(source, path) && Directory.Exists(path));

        [RelayCommand(CanExecute = nameof(CanMove))]
        private void Move()
        {
            if (IsModalOpen) return;
            ShowMessage("Move to Different Library", "Choose where to move the selected version.");
            CanMoveAllVersions = Knossos.GetInstalledModList(ActiveVersion.id).Count > 1;
            MoveAllVersions = false;
            RefreshMoveLibraries();
            IsMoveModal = true;
        }

        partial void OnMoveAllVersionsChanged(bool value)
        {
            ModalText = value ? "Choose a destination for all installed versions. Versions already there will stay in place."
                : "Choose where to move the selected version.";
            RefreshMoveLibraries();
        }

        private Mod[] GetMoveTargets()
        {
            var all = Knossos.GetInstalledModList(ActiveVersion.id);
            return (MoveAllVersions ? all : all.Where(mod => mod.version == ActiveVersion.version)).ToArray();
        }

        private void RefreshMoveLibraries()
        {
            var targets = GetMoveTargets().Select(mod => Knossos.GetLibraryForPath(mod.fullPath)).ToArray();
            var previous = SelectedMoveLibrary;
            MoveLibraries = Knossos.GetLibraryPaths().Where(path => Directory.Exists(path) &&
                targets.Any(source => source != null && !Knossos.LibraryPathComparer.Equals(source, path))).ToArray();
            SelectedMoveLibrary = MoveLibraries.FirstOrDefault(path => Knossos.LibraryPathComparer.Equals(previous, path))
                ?? MoveLibraries.FirstOrDefault();
        }

        private void OnMoveLibraryPathsChanged()
        {
            MoveCommand.NotifyCanExecuteChanged();
            if (IsMoveModal) RefreshMoveLibraries();
        }

        private bool CanConfirmMove() => SelectedMoveLibrary != null;

        [RelayCommand(CanExecute = nameof(CanConfirmMove))]
        private async Task ConfirmMoveAsync()
        {
            if (!IsMoveModal || SelectedMoveLibrary is not { } destination) return;
            var targets = GetMoveTargets().Where(mod => Knossos.GetLibraryForPath(mod.fullPath) is { } source &&
                !Knossos.LibraryPathComparer.Equals(source, destination)).ToArray();
            if (targets.Length == 0) return;
            SaveOptions();
            CloseModal();
            await Versions.RunLibraryMoveAsync(targets, destination);
        }

        internal void RefreshVersionPathsAfterMove(string previousActivePath)
        {
            foreach (var version in versions)
            {
                if (Knossos.GetInstalledMod(version.id, version.version) is { } installed && !ReferenceEquals(version, installed))
                {
                    version.fullPath = installed.fullPath;
                    version.folderName = installed.folderName;
                    version.modSettings = installed.modSettings;
                }
            }
            options?.Dispose();
            options = null;
            if (!Knossos.LibraryPathComparer.Equals(previousActivePath, ActiveVersion.fullPath))
                _ = Information.LoadAsync(ActiveVersion);
            else
                Information.RefreshLibraryLocation();
        }
    }

    public partial class ModVersionsViewModel
    {
        internal async Task RunLibraryMoveAsync(IReadOnlyList<Mod> targets, string destination)
        {
            if (disposed || IsApplying || owner.IsSubmitting || TaskViewModel.Instance is not { } tasks) return;
            BeginOperation(targets);
            var previousActivePath = owner.ActiveVersion.fullPath;
            Status = "Moving installed versions…";
            try
            {
                var moved = await tasks.MoveModsAsync(targets, destination, operation!,
                    error => owner.ShowMessage("Move failed", error));
                owner.RefreshVersionPathsAfterMove(previousActivePath);
                if (initialized) await ReloadAsync();
                Status = moved ? $"Moved to {destination}" : ActiveTask?.Info ?? "Move stopped";
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, nameof(RunLibraryMoveAsync), ex);
                owner.ShowMessage("Move failed", ex.Message);
            }
            finally { EndOperation(); }
        }
    }
}
