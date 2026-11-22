using Avalonia.Threading;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
    public partial class TaskViewModel
    {
        internal async Task<bool> MoveModsAsync(IReadOnlyList<Mod> mods, string library, ModInstallOperation operation, Action<string> reportError)
        {
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(operation.Token);
            var task = new TaskItemViewModel();
            operation.Track(task);
            Dispatcher.UIThread.Invoke(() =>
            {
                TaskList.Add(task);
                taskQueue.Enqueue(task);
            });
            return await task.MoveModsAsync(this, mods, library, cancellation, reportError);
        }
    }

    public partial class TaskItemViewModel
    {
        internal async Task<bool> MoveModsAsync(TaskViewModel tasks, IReadOnlyList<Mod> requested, string library,
            CancellationTokenSource cancellation, Action<string> reportError)
        {
            var token = cancellation.Token;
            TaskIsSet = true;
            cancellationTokenSource = cancellation;
            installID = requested[0].id;
            installVersion = requested.Count == 1 ? requested[0].version : null;
            Name = requested.Count == 1 ? $"Move {requested[0]}" : $"Move all versions of {requested[0].title}";
            TooltipVisible = true;
            Tooltip = library;
            CancelButtonVisible = true;
            ProgressBarMax = requested.Count;
            Info = "In queue";
            int completed = 0;

            try
            {
                while (!Dispatcher.UIThread.Invoke(() => tasks.taskQueue.TryPeek(out var next) && ReferenceEquals(next, this)))
                    await Task.Delay(250, token);

                token.ThrowIfCancellationRequested();
                var libraries = Knossos.GetLibraryPaths();
                var moves = requested.Select(mod => Knossos.GetInstalledMod(mod.id, mod.version)
                    ?? throw new IOException($"{mod} is no longer installed."))
                    .Select(mod => (Mod: mod, Destination: ModLibraryMove.GetDestination(mod, library, libraries))).ToArray();
                if (moves.Select(move => move.Destination).Distinct(Knossos.LibraryPathComparer).Count() != moves.Length)
                    throw new IOException("Multiple versions have the same destination.");

                foreach (var (mod, destination) in moves)
                {
                    token.ThrowIfCancellationRequested();
                    Info = $"Moving version {mod.version} ({completed + 1}/{moves.Length})";
                    var progressBeforeCopy = completed;
                    await Task.Run(() => ModLibraryMove.MoveAsync(mod, library, destination, token,
                        progress => Dispatcher.UIThread.Post(() =>
                        {
                            if (completed == progressBeforeCopy)
                                ProgressCurrent = progressBeforeCopy + progress;
                        })), token);

                    mod.fullPath = destination + Path.DirectorySeparatorChar;
                    mod.folderName = Path.GetFileName(destination);
                    mod.modSettings.Load(mod.fullPath);
                    ProgressCurrent = ++completed;
                    MainViewModel.Instance?.InstalledModsView?.RefreshLibraryFilter();
                }

                Info = $"Moved to {library}";
                IsCompleted = true;
                return true;
            }
            catch (OperationCanceledException)
            {
                IsCancelled = true;
                Info = completed == 0 ? "Move cancelled" : $"Move cancelled after {completed} of {requested.Count} versions";
                return false;
            }
            catch (Exception ex)
            {
                IsCancelled = true;
                Info = completed == 0 ? $"Move failed: {ex.Message}" : $"Moved {completed} of {requested.Count} versions. {ex.Message}";
                Log.Add(Log.LogSeverity.Error, nameof(MoveModsAsync), ex);
                reportError(Info);
                return false;
            }
            finally
            {
                CancelButtonVisible = false;
                tasks.CompleteQueuedTask(this);
            }
        }
    }
}
