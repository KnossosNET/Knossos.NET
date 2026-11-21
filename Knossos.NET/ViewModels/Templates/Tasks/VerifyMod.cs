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
    public partial class TaskItemViewModel : ViewModelBase
    {
        public async Task<bool> VerifyMod(Mod mod, CancellationTokenSource cancelSource, bool awaitRepairs = false,
            Action<string>? reportError = null, ModInstallOperation? operation = null)
        {
            if (TaskIsSet) throw new InvalidOperationException("This task has already been assigned.");
            TaskIsSet = true;
            cancellationTokenSource = cancelSource;
            var token = cancelSource.Token;
            var tasks = TaskViewModel.Instance!;
            installID = mod.id;
            installVersion = mod.version;
            Name = "Verifying " + mod;
            ShowProgressText = false;
            CancelButtonVisible = true;
            Info = "In Queue";

            try
            {
                await Dispatcher.UIThread.InvokeAsync(() => TaskRoot.Add(this));
                while (tasks.taskQueue.Count > 0 && tasks.taskQueue.Peek() != this)
                    await Task.Delay(1000, token);

                token.ThrowIfCancellationRequested();
                var (damaged, checkedFiles, unknownFiles) = await Task.Run(() => CheckFilesAsync(mod, token), token);
                token.ThrowIfCancellationRequested();
                if (checkedFiles == 0)
                {
                    Info = "No SHA-256 checksums are available for this installation.";
                    IsCompleted = true;
                    return false;
                }

                if (damaged.Count == 0)
                {
                    Info = "Passed · " + checkedFiles + " files checked" + UnknownFilesHint(unknownFiles);
                    IsCompleted = true;
                    return true;
                }

                var repairable = damaged.All(pkg => pkg.files is { Length: > 0 } files && files.All(file => file.urls?.Length > 0));
                if (!repairable || mod.devMode || mod.modSource == ModSource.local)
                {
                    Info = damaged.Count + " packages failed verification. Automatic repair is unavailable.";
                    IsCancelled = true;
                    if (reportError != null)
                        await Dispatcher.UIThread.InvokeAsync(() => reportError(Info));
                    return false;
                }

                // The queued repair cannot start until verification releases its own slot.
                tasks.CompleteQueuedTask(this);
                IsCompleted = true;
                CancelButtonVisible = false;
                Info = damaged.Count + " packages failed verification. Repair queued." + UnknownFilesHint(unknownFiles);
                var replacement = ModPackageCatalog.Copy(mod);
                replacement.packages.ForEach(package => package.isSelected = true);
                var repair = tasks.InstallModAsync(replacement, damaged, cleanInstall: true,
                    reportError: reportError, addOnly: true, operation: operation);
                if (!awaitRepairs) return true;

                var repaired = await repair;
                Info = repaired ? "Damaged packages repaired" + UnknownFilesHint(unknownFiles) : "Repair stopped. See the repair task for details.";
                return repaired;
            }
            catch (OperationCanceledException)
            {
                Info = "Task Cancelled";
                IsCancelled = true;
                return false;
            }
            catch (Exception ex)
            {
                Info = "Verification failed";
                IsCancelled = true;
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.VerifyMod()", ex);
                if (reportError != null)
                    await Dispatcher.UIThread.InvokeAsync(() => reportError(ex.Message));
                return false;
            }
            finally
            {
                CancelButtonVisible = false;
                tasks.CompleteQueuedTask(this);
            }
        }

        private async Task<(List<ModPackage> Damaged, int CheckedFiles, int UnknownFiles)> CheckFilesAsync(Mod mod, CancellationToken token)
        {
            mod.ReLoadJson();
            var unknown = Directory.EnumerateFiles(mod.fullPath, "*", new EnumerationOptions
            {
                RecurseSubdirectories = mod.id != "FS2",
                AttributesToSkip = FileAttributes.ReparsePoint
            }).Select(Path.GetFullPath).ToHashSet(KnUtils.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            unknown.RemoveWhere(file =>
                Path.GetExtension(file).Equals(".json", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(file).Equals(".ini", StringComparison.OrdinalIgnoreCase) ||
                Path.GetRelativePath(mod.fullPath, file).StartsWith("kn_images" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(file).Equals(mod.tile, StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(file).Equals(mod.banner, StringComparison.OrdinalIgnoreCase));

            var damaged = new List<ModPackage>();
            var checkedFiles = 0;
            ProgressCurrent = 0;
            ProgressBarMax = mod.packages.Sum(pkg => pkg.filelist?.Length ?? 0);
            foreach (var pkg in mod.packages)
            {
                var passed = true;
                foreach (var file in pkg.filelist ?? Array.Empty<ModFilelist>())
                {
                    token.ThrowIfCancellationRequested();
                    bool hasHash = file.checksum is { Length: >= 2 } && string.Equals(file.checksum[0], "sha256", StringComparison.OrdinalIgnoreCase);
                    Info = file.filename ?? "Checking file list";
                    ProgressCurrent++;
                    try
                    {
                        var root = mod.devMode && !string.IsNullOrEmpty(pkg.folder) ? InstalledModFile.Resolve(mod.fullPath, pkg.folder) : mod.fullPath;
                        var expected = InstalledModFile.Resolve(root, file.filename);
                        var actual = InstalledModFile.Find(expected);
                        unknown.Remove(actual);

                        if (!hasHash)
                            continue;

                        // Developer VP packages are extracted on installation and have no whole-archive file to check.
                        if (mod.devMode && pkg.isVp && expected.EndsWith(".vp", StringComparison.OrdinalIgnoreCase) && !File.Exists(actual))
                            continue;

                        checkedFiles++;
                        var hash = await InstalledModFile.GetHashAsync(actual, expected, token);
                        if (hash.Equals(file.checksum![1], StringComparison.OrdinalIgnoreCase)) continue;
                        passed = false;
                        Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.VerifyMod()", file.filename + " failed its checksum check.");
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        passed = false;
                        Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.VerifyMod()", ex);
                    }
                }
                if (!passed) damaged.Add(pkg);
            }

            foreach (var file in unknown)
                Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.VerifyMod()", "Unknown file: " + file);
            return (damaged, checkedFiles, unknown.Count);
        }

        private static string UnknownFilesHint(int count) => count == 0 ? string.Empty : " · " + count + " unlisted files";
    }
}
