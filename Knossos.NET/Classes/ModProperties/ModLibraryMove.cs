using Knossos.NET.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Knossos.NET.Classes
{
    internal static class ModLibraryMove
    {
        internal const string TemporaryDirectoryPrefix = ".knossos-move-";

        internal static string GetDestination(Mod mod, string library, IReadOnlyList<string> libraries)
        {
            var source = Path.TrimEndingDirectorySeparator(Path.GetFullPath(mod.fullPath));
            var sourceLibrary = Knossos.GetLibraryForPath(source);
            if (sourceLibrary == null || Knossos.LibraryPathComparer.Equals(sourceLibrary, source) ||
                !Knossos.IsPathInLibrary(sourceLibrary, source))
                throw new IOException($"'{source}' is not a mod folder within a configured library.");
            CheckLibrary(library, libraries);
            if (Knossos.LibraryPathComparer.Equals(sourceLibrary, library))
                return source;

            var retail = Fs2RetailMove.IsRetail(mod);
            var destination = Path.Combine(library, retail ? "FS2" : Path.GetRelativePath(sourceLibrary, source));
            if (!Knossos.IsPathInLibrary(library, destination))
                throw new IOException($"'{destination}' is outside the destination library.");
            if (!retail && (File.Exists(destination) || Directory.Exists(destination)))
                throw new IOException($"'{destination}' already exists.");
            if (!retail && Knossos.GetInstalledModList(null).Any(other => !ReferenceEquals(other, mod) &&
                KnUtils.IsSubPath(source, Path.GetRelativePath(source, other.fullPath))))
                throw new IOException($"'{source}' contains other installed mods.");

            var earlierLibraries = libraries.TakeWhile(path => !Knossos.LibraryPathComparer.Equals(path, library)).Append(library);
            var shadowingCopy = Knossos.GetDuplicateModPaths(mod).FirstOrDefault(path =>
                Directory.Exists(path) &&
                earlierLibraries.Contains(Knossos.GetLibraryForPath(path) ?? string.Empty, Knossos.LibraryPathComparer));
            if (shadowingCopy != null)
                throw new IOException($"An existing copy at '{shadowingCopy}' would conflict with the moved version.");
            return destination;
        }

        private static void CheckLibrary(string library, IReadOnlyList<string> libraries)
        {
            if (!libraries.Contains(library, Knossos.LibraryPathComparer) || !Directory.Exists(library))
                throw new IOException($"The destination library '{library}' is unavailable.");
        }

        internal static async Task MoveAsync(Mod mod, string library, string destination, CancellationToken token, Action<float> progress)
        {
            var source = Path.TrimEndingDirectorySeparator(Path.GetFullPath(mod.fullPath));
            if (Knossos.LibraryPathComparer.Equals(source, destination))
                return;
            CheckLibrary(library, Knossos.GetLibraryPaths());
            if (Fs2RetailMove.IsRetail(mod))
            {
                await Fs2RetailMove.MoveAsync(source, destination, token, progress).ConfigureAwait(false);
                return;
            }

            var destinationParent = Path.GetDirectoryName(destination)!;
            Directory.CreateDirectory(destinationParent);
            token.ThrowIfCancellationRequested();
            try
            {
                // A rename on the same filesystem preserves links, attributes and timestamps.
                Directory.Move(source, destination);
                return;
            }
            catch (IOException) when (Directory.Exists(source) && !Directory.Exists(destination) && !File.Exists(destination))
            {
                // Across volumes Directory.Move may fail. Publish a complete staged copy instead.
            }

            var staging = Path.Combine(destinationParent, TemporaryDirectoryPrefix + Guid.NewGuid().ToString("N"));
            var backup = Path.Combine(Path.GetDirectoryName(source)!, TemporaryDirectoryPrefix + Guid.NewGuid().ToString("N"));
            try
            {
                Copy(source, staging, token, progress);
                token.ThrowIfCancellationRequested();
                CheckLibrary(library, Knossos.GetLibraryPaths());
                Directory.Move(staging, destination);
                try
                {
                    Directory.Move(source, backup);
                }
                catch
                {
                    Directory.Move(destination, staging);
                    throw;
                }

                CleanTemporaryDirectory(backup);
            }
            finally
            {
                CleanTemporaryDirectory(staging);
            }
        }

        private static void Copy(string source, string staging, CancellationToken token, Action<float> progress)
        {
            var files = new List<FileInfo>();
            var directories = new Stack<DirectoryInfo>();
            var visitedDirectories = new List<DirectoryInfo>();
            directories.Push(new DirectoryInfo(source));
            while (directories.TryPop(out var directory))
            {
                token.ThrowIfCancellationRequested();
                _ = directory.LastWriteTimeUtc;
                visitedDirectories.Add(directory);
                Directory.CreateDirectory(Path.Combine(staging, Path.GetRelativePath(source, directory.FullName)));
                foreach (var entry in directory.EnumerateFileSystemInfos())
                {
                    if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                        throw new IOException($"Cross-volume moves cannot copy the linked path '{entry.FullName}'.");
                    if (entry is DirectoryInfo child)
                        directories.Push(child);
                    else if (entry is FileInfo file)
                        files.Add(file);
                }
            }

            // Native file copies preserve modes needed by scripts and other developer files.
            var bytes = Math.Max(1, files.Sum(file => file.Length));
            long written = 0;
            var lastPercent = -1;
            foreach (var file in files)
            {
                token.ThrowIfCancellationRequested();
                var target = Path.Combine(staging, Path.GetRelativePath(source, file.FullName));
                file.CopyTo(target);
                written += file.Length;
                var percent = (int)Math.Min(99, 100.0 * written / bytes);
                if (percent != lastPercent)
                {
                    progress(percent / 100f);
                    lastPercent = percent;
                }
            }

            foreach (var file in files)
            {
                var current = new FileInfo(file.FullName);
                if (!current.Exists || current.Length != file.Length || current.LastWriteTimeUtc != file.LastWriteTimeUtc)
                    throw new IOException($"'{file.FullName}' changed during the move. Try again after closing any applications using it.");
            }
            foreach (var directory in visitedDirectories)
            {
                if (Directory.GetLastWriteTimeUtc(directory.FullName) != directory.LastWriteTimeUtc)
                    throw new IOException($"'{directory.FullName}' changed during the move. Try again after closing any applications using it.");
            }
        }

        private static void CleanTemporaryDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, nameof(ModLibraryMove), $"Move cleanup failed for '{path}': {ex.Message}");
            }
        }
    }
}
