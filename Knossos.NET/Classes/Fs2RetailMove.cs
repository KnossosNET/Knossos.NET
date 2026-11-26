using Knossos.NET.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Knossos.NET.Classes
{
    internal static class Fs2RetailMove
    {
        internal const string DataDirectory = "data";
        private static readonly StringComparer fileNames = Fs2RetailFiles.FileNameComparer;

        private sealed record RetailFile(string RelativePath, long Length, DateTime LastWriteTimeUtc)
        {
            internal bool IsMetadata => fileNames.Equals(RelativePath, Fs2RetailFiles.Metadata);
        }

        private sealed record RetailDirectory(string RelativePath, DateTime LastWriteTimeUtc);

        internal static bool IsRetail(Mod mod) => mod.id == "FS2" && mod.parent == "FS2" && mod.type == ModType.tc;

        internal static async Task MoveAsync(string source, string destination, CancellationToken token, Action<float> progress)
        {
            source = Path.TrimEndingDirectorySeparator(Path.GetFullPath(source));
            destination = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destination));
            if (Knossos.LibraryPathComparer.Equals(source, destination))
                throw new IOException("FreeSpace 2 is already in this library.");

            var rootFiles = FindRootFiles(source);
            var dataFiles = new List<RetailFile>();
            var dataDirectories = new List<RetailDirectory>();
            CollectData(source, dataFiles, dataDirectories, token);
            var files = rootFiles.Concat(dataFiles).OrderBy(file => file.IsMetadata).ToArray();
            var destinationPaths = files.Select(file => file.RelativePath);
            Fs2RetailFiles.CheckDestination(destination, destinationPaths);
            token.ThrowIfCancellationRequested();

            var transactionName = ModLibraryMove.TemporaryDirectoryPrefix + "retail-" + Guid.NewGuid().ToString("N");
            var sourceTransaction = Path.Combine(source, transactionName);
            var destinationTransaction = Path.Combine(Path.GetDirectoryName(destination)!, transactionName);
            // Store inactive copies beneath the library scanner's discovery depth.
            var backup = Path.Combine(sourceTransaction, "files", "original");
            var stage = Path.Combine(destinationTransaction, "files", "staged");
            var sourceData = Path.Combine(source, DataDirectory);
            var backupData = Path.Combine(backup, DataDirectory);
            var published = new List<RetailFile>();
            var originals = new List<RetailFile>();
            var createdDirectories = new List<string>();
            bool dataBackedUp = false;
            bool keepRecoveryFiles = false;
            try
            {
                await StageAsync(source, stage, files, token, progress).ConfigureAwait(false);
                Fs2RetailFiles.CheckDestination(destination, destinationPaths);
                CheckOriginals(source, files, dataDirectories);
                token.ThrowIfCancellationRequested();

                // Publish the retail files and data tree without overwriting existing content.
                // The installation becomes visible to Knossos when its metadata arrives.
                CreateDestinationDirectory(destination, createdDirectories);
                foreach (var directory in dataDirectories)
                {
                    Fs2RetailFiles.EnsureOrdinaryDirectoryPath(destination, directory.RelativePath);
                    CreateDestinationDirectory(Path.Combine(destination, directory.RelativePath), createdDirectories);
                }
                foreach (var file in files)
                {
                    Rename(file, stage, destination);
                    published.Add(file);
                }

                // Keep a valid destination throughout source cleanup. Removing the
                // original metadata first also leaves child mods in FS2 discoverable.
                foreach (var file in rootFiles.Reverse())
                {
                    Rename(file, source, backup);
                    originals.Add(file);
                }
                if (dataDirectories.Count > 0)
                {
                    Directory.Move(sourceData, backupData);
                    dataBackedUp = true;
                    CheckOriginals(backup, dataFiles, dataDirectories);
                }
            }
            catch (Exception moveError)
            {
                var recoveryErrors = new List<Exception>();
                if (dataBackedUp)
                {
                    try { Directory.Move(backupData, sourceData); }
                    catch (Exception error) { recoveryErrors.Add(error); }
                }
                Restore(originals, backup, source, recoveryErrors);
                if (recoveryErrors.Count == 0)
                    Restore(published, destination, stage, recoveryErrors);

                if (recoveryErrors.Count > 0)
                {
                    keepRecoveryFiles = true;
                    recoveryErrors.Insert(0, moveError);
                    throw new IOException(
                        "FreeSpace 2 could not be moved and rollback was incomplete. Recovery files may remain at '" +
                        sourceTransaction + "' and '" + destinationTransaction + "'.",
                        new AggregateException(recoveryErrors));
                }
                foreach (var directory in createdDirectories.AsEnumerable().Reverse())
                    CleanDirectory(directory, recursive: false);
                throw;
            }
            finally
            {
                if (!keepRecoveryFiles)
                {
                    CleanDirectory(sourceTransaction);
                    CleanDirectory(destinationTransaction);
                }
            }
            progress(1);
        }

        private static RetailFile[] FindRootFiles(string source)
        {
            var found = new List<RetailFile>();
            Fs2RetailFiles.EnsureOrdinaryDirectoryPath(source, string.Empty);
            var wanted = Fs2RetailFiles.KnownInstalledPaths()
                .Where(path => string.IsNullOrEmpty(Path.GetDirectoryName(path))).ToHashSet(fileNames);
            var matched = new HashSet<string>(fileNames);
            foreach (var file in new DirectoryInfo(source).EnumerateFiles())
            {
                if (!wanted.Contains(file.Name)) continue;
                if (!matched.Add(file.Name))
                    throw new IOException($"Several FreeSpace 2 files in '{source}' match '{file.Name}'.");
                if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException($"Cannot move a linked FreeSpace 2 file at '{file.FullName}'.");
                found.Add(new RetailFile(file.Name, file.Length, file.LastWriteTimeUtc));
            }

            if (!found.Any(file => file.IsMetadata) || !found.Any(file =>
                    fileNames.Equals(file.RelativePath, "root_fs2.vp") || fileNames.Equals(file.RelativePath, "root_fs2.vpc")))
                throw new IOException("The FreeSpace 2 installation is missing its metadata or root data pack.");

            return found.OrderBy(file => file.IsMetadata).ToArray();
        }

        private static void CollectData(string source, List<RetailFile> files, List<RetailDirectory> directories,
            CancellationToken token)
        {
            Fs2RetailFiles.EnsureOrdinaryDirectoryPath(source, DataDirectory);
            var data = new DirectoryInfo(Path.Combine(source, DataDirectory));
            if (!data.Exists) return;

            var pending = new Stack<DirectoryInfo>();
            pending.Push(data);
            while (pending.TryPop(out var directory))
            {
                token.ThrowIfCancellationRequested();
                var path = Path.GetRelativePath(source, directory.FullName);
                Fs2RetailFiles.EnsureOrdinaryDirectoryPath(source, path);
                directories.Add(new RetailDirectory(path, directory.LastWriteTimeUtc));
                foreach (var entry in directory.EnumerateFileSystemInfos())
                {
                    if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                        throw new IOException($"Cannot move a linked FreeSpace 2 path at '{entry.FullName}'.");
                    if (entry is DirectoryInfo child)
                        pending.Push(child);
                    else if (entry is FileInfo file)
                        files.Add(new RetailFile(Path.Combine(path, file.Name), file.Length, file.LastWriteTimeUtc));
                }
            }
        }

        private static async Task StageAsync(string source, string stage, IReadOnlyList<RetailFile> files,
            CancellationToken token, Action<float> progress)
        {
            const int bufferSize = 128 * 1024;
            long total = Math.Max(files.Sum(file => file.Length), 1);
            long copied = 0;
            int reported = -1;
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                foreach (var file in files)
                {
                    token.ThrowIfCancellationRequested();
                    var target = Path.Combine(stage, file.RelativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    await using (var input = new FileStream(Path.Combine(source, file.RelativePath),
                        FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true))
                    await using (var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write,
                        FileShare.None, bufferSize, useAsync: true))
                    {
                        int count;
                        while ((count = await input.ReadAsync(buffer.AsMemory(0, bufferSize), token).ConfigureAwait(false)) > 0)
                        {
                            await output.WriteAsync(buffer.AsMemory(0, count), token).ConfigureAwait(false);
                            copied += count;
                            int percent = (int)Math.Min(90, 90.0 * copied / total);
                            if (percent != reported)
                            {
                                reported = percent;
                                progress(percent / 100f);
                            }
                        }
                    }
                    File.SetLastWriteTimeUtc(target, file.LastWriteTimeUtc);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static void CheckOriginals(string source, IEnumerable<RetailFile> files, IEnumerable<RetailDirectory> directories)
        {
            foreach (var directory in directories)
            {
                Fs2RetailFiles.EnsureOrdinaryDirectoryPath(source, directory.RelativePath);
                var original = new DirectoryInfo(Path.Combine(source, directory.RelativePath));
                if (!original.Exists || original.LastWriteTimeUtc != directory.LastWriteTimeUtc)
                    throw new IOException($"'{original.FullName}' changed while it was being copied. Try the move again.");
            }
            foreach (var file in files)
            {
                var original = new FileInfo(Path.Combine(source, file.RelativePath));
                if (!original.Exists || (original.Attributes & FileAttributes.ReparsePoint) != 0 ||
                    original.Length != file.Length || original.LastWriteTimeUtc != file.LastWriteTimeUtc)
                    throw new IOException($"'{original.FullName}' changed while it was being copied. Try the move again.");
            }
        }

        private static void CreateDestinationDirectory(string path, ICollection<string> createdDirectories)
        {
            if (Directory.Exists(path)) return;
            Directory.CreateDirectory(path);
            createdDirectories.Add(path);
        }

        private static void Rename(RetailFile file, string source, string destination)
        {
            var subfolder = Path.GetDirectoryName(file.RelativePath) ?? string.Empty;
            Fs2RetailFiles.EnsureOrdinaryDirectoryPath(source, subfolder);
            Fs2RetailFiles.EnsureOrdinaryDirectoryPath(destination, subfolder);
            var target = Path.Combine(destination, file.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Move(Path.Combine(source, file.RelativePath), target);
        }

        private static void Restore(IEnumerable<RetailFile> files, string source, string destination, List<Exception> errors)
        {
            foreach (var file in files.Reverse())
            {
                // Keep metadata unpublished if its associated source files could not be restored.
                if (file.IsMetadata && errors.Count > 0) continue;
                try { Rename(file, source, destination); }
                catch (Exception error)
                {
                    errors.Add(error);
                    if (file.IsMetadata) break;
                }
            }
        }

        private static void CleanDirectory(string path, bool recursive = true)
        {
            try
            {
                if (Directory.Exists(path) && (recursive || !Directory.EnumerateFileSystemEntries(path).Any()))
                    Directory.Delete(path, recursive);
            }
            catch (Exception error)
            {
                Log.Add(Log.LogSeverity.Warning, nameof(Fs2RetailMove),
                    $"Unable to clean up the FreeSpace 2 move folder '{path}': {error.Message}");
            }
        }
    }
}
