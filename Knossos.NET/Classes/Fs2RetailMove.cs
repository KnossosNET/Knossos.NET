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
        private static readonly StringComparer fileNames = Fs2RetailFiles.FileNameComparer;

        private sealed record RetailFile(string RelativePath, long Length, DateTime LastWriteTimeUtc)
        {
            internal bool IsMetadata => fileNames.Equals(RelativePath, Fs2RetailFiles.Metadata);
        }

        internal static bool IsRetail(Mod mod) => mod.id == "FS2" && mod.parent == "FS2" && mod.type == ModType.tc;

        internal static async Task MoveAsync(string source, string destination, CancellationToken token, Action<float> progress)
        {
            source = Path.TrimEndingDirectorySeparator(Path.GetFullPath(source));
            destination = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destination));
            if (Knossos.LibraryPathComparer.Equals(source, destination))
                throw new IOException("FreeSpace 2 is already in this library.");

            var files = FindRetailFiles(source);
            var destinationPaths = files.Select(file => file.RelativePath);
            Fs2RetailFiles.CheckDestination(destination, destinationPaths);
            token.ThrowIfCancellationRequested();

            var transactionName = ".knossos-retail-move-" + Guid.NewGuid().ToString("N");
            var sourceTransaction = Path.Combine(Path.GetDirectoryName(source)!, transactionName);
            var destinationTransaction = Path.Combine(Path.GetDirectoryName(destination)!, transactionName);
            // Store inactive copies beneath the library scanner's discovery depth.
            var backup = Path.Combine(sourceTransaction, "files", "original");
            var stage = Path.Combine(destinationTransaction, "files", "staged");
            var published = new List<RetailFile>();
            var originals = new List<RetailFile>();
            bool keepRecoveryFiles = false;
            try
            {
                await StageAsync(source, stage, files, token, progress).ConfigureAwait(false);
                Fs2RetailFiles.CheckDestination(destination, destinationPaths);
                CheckOriginals(source, files);
                token.ThrowIfCancellationRequested();

                // Publish the known retail files without overwriting any existing files.
                // The installation becomes visible to Knossos when its metadata arrives.
                foreach (var file in files)
                {
                    Rename(file, stage, destination);
                    published.Add(file);
                }

                // Keep a valid destination throughout source cleanup. Removing the
                // original metadata first also leaves child mods in FS2 discoverable.
                foreach (var file in files.Reverse())
                {
                    Rename(file, source, backup);
                    originals.Add(file);
                }
            }
            catch (Exception moveError)
            {
                var recoveryErrors = Restore(originals, backup, source);
                if (recoveryErrors.Count == 0)
                    recoveryErrors = Restore(published, destination, stage);

                if (recoveryErrors.Count > 0)
                {
                    keepRecoveryFiles = true;
                    recoveryErrors.Insert(0, moveError);
                    throw new IOException(
                        "FreeSpace 2 could not be moved and rollback was incomplete. Recovery files may remain at '" +
                        sourceTransaction + "' and '" + destinationTransaction + "'.",
                        new AggregateException(recoveryErrors));
                }
                throw;
            }
            finally
            {
                if (!keepRecoveryFiles)
                {
                    CleanTransaction(sourceTransaction);
                    CleanTransaction(destinationTransaction);
                }
            }
            progress(1);
        }

        private static RetailFile[] FindRetailFiles(string source)
        {
            var found = new List<RetailFile>();

            foreach (var group in Fs2RetailFiles.KnownInstalledPaths().GroupBy(path => Path.GetDirectoryName(path) ?? string.Empty))
            {
                var directory = Path.Combine(source, group.Key);
                Fs2RetailFiles.EnsureOrdinaryDirectoryPath(source, group.Key);
                if (!Directory.Exists(directory)) continue;

                var wanted = group.Select(Path.GetFileName).ToHashSet(fileNames);
                var matched = new HashSet<string>(fileNames);
                foreach (var entry in new DirectoryInfo(directory).EnumerateFiles())
                {
                    if (!wanted.Contains(entry.Name)) continue;
                    if (!matched.Add(entry.Name))
                        throw new IOException($"Several FreeSpace 2 files in '{directory}' match '{entry.Name}'.");
                    if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                        throw new IOException($"Cannot move a linked FreeSpace 2 file at '{entry.FullName}'.");
                    found.Add(new RetailFile(Path.Combine(group.Key, entry.Name), entry.Length, entry.LastWriteTimeUtc));
                }
            }

            if (!found.Any(file => file.IsMetadata) || !found.Any(file =>
                    fileNames.Equals(file.RelativePath, "root_fs2.vp") || fileNames.Equals(file.RelativePath, "root_fs2.vpc")))
                throw new IOException("The FreeSpace 2 installation is missing its metadata or root data pack.");

            return found.OrderBy(file => file.IsMetadata).ToArray();
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

        private static void CheckOriginals(string source, IEnumerable<RetailFile> files)
        {
            foreach (var file in files)
            {
                var original = new FileInfo(Path.Combine(source, file.RelativePath));
                if (!original.Exists || (original.Attributes & FileAttributes.ReparsePoint) != 0 ||
                    original.Length != file.Length || original.LastWriteTimeUtc != file.LastWriteTimeUtc)
                    throw new IOException($"'{original.FullName}' changed while it was being copied. Try the move again.");
            }
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

        private static List<Exception> Restore(IEnumerable<RetailFile> files, string source, string destination)
        {
            var errors = new List<Exception>();
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
            return errors;
        }

        private static void CleanTransaction(string path)
        {
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
            }
            catch (Exception error)
            {
                Log.Add(Log.LogSeverity.Warning, nameof(Fs2RetailMove),
                    $"Unable to remove the FreeSpace 2 staging folder '{path}': {error.Message}");
            }
        }
    }
}
