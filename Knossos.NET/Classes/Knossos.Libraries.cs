using Avalonia.Threading;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.ViewModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Knossos.NET
{
    public static partial class Knossos
    {
        public static StringComparer LibraryPathComparer { get; } =
            OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;

        public static event Action? LibraryPathsChanged;
        private static readonly ConcurrentDictionary<(string Id, string Version), string[]> duplicateModPaths = new();

        public static IReadOnlyList<string> GetLibraryPaths()
        {
            var settings = globalSettings;
            if (settings.libraryPaths != null)
                return settings.libraryPaths.ToArray();
            return settings.basePath is { } legacyPath ? new[] { legacyPath } : Array.Empty<string>();
        }

        public static string? GetLibraryForPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var fullPath = NormalizeLibraryPath(path);
            return GetLibraryPaths().FirstOrDefault(root => ContainsPath(root, fullPath));
        }

        internal static string? GetDefaultModLibrary(Mod mod)
        {
            var installed = GetInstalledModList(mod.id);
            var previous = installed.FirstOrDefault(candidate => candidate.version == mod.version);
            if (previous == null)
            {
                installed.Sort(Mod.CompareVersionNewerToOlder);
                previous = installed.FirstOrDefault(candidate => Mod.CompareVersion(candidate, mod) < 0)
                    ?? installed.FirstOrDefault();
            }
            return GetLibraryForPath(previous?.fullPath) ?? GetKnossosLibraryPath();
        }

        internal static bool IsPathInLibrary(string root, string path)
        {
            var fullPath = NormalizeLibraryPath(path);
            if (!ContainsPath(root, fullPath))
                return false;

            // The registered root may itself be a link. Paths inside it must use the
            // same directory layout the scanner can discover.
            var resolvedRoot = ResolveLibraryPath(root);
            var expected = Path.GetFullPath(Path.GetRelativePath(root, fullPath), resolvedRoot);
            var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            return comparer.Equals(ResolveLibraryPath(fullPath), expected);
        }

        public static IReadOnlyList<string> GetDuplicateModPaths(Mod mod) =>
            duplicateModPaths.TryGetValue((mod.id, mod.version), out var paths) ? paths : Array.Empty<string>();

        private static bool RegisterScannedMod(Mod mod)
        {
            var existing = GetInstalledMod(mod.id, mod.version);
            if (existing == null)
            {
                installedMods.Add(mod);
                return true;
            }

            if (!LibraryPathComparer.Equals(NormalizeLibraryPath(existing.fullPath), NormalizeLibraryPath(mod.fullPath)))
            {
                duplicateModPaths.AddOrUpdate((mod.id, mod.version), _ => new[] { mod.fullPath },
                    (_, paths) => paths.Append(mod.fullPath).ToArray());
                Log.Add(Log.LogSeverity.Warning, nameof(RegisterScannedMod),
                    $"Duplicate {mod.id} {mod.version} at '{mod.fullPath}' ignored. Using '{existing.fullPath}'.");
            }
            return false;
        }

        internal static void NormalizeLibrarySettings(GlobalSettings settings)
        {
            if (settings.libraryPaths == null && string.IsNullOrWhiteSpace(settings.basePath))
                return;

            var roots = new List<string>();
            foreach (var path in settings.libraryPaths ?? new List<string> { settings.basePath! })
            {
                if (!TryNormalizeLibraryPath(path, roots, out var root, out var error))
                {
                    Log.Add(Log.LogSeverity.Warning, nameof(NormalizeLibrarySettings), error!);
                    continue;
                }
                roots.Add(root!);
            }

            settings.libraryPaths = roots;
            settings.basePath = roots.FirstOrDefault();
        }

        public static bool TryAddLibrary(string path, out string? error)
        {
            if (!CanChangeLibraries())
            {
                error = "Wait for the library scan and running tasks to finish, then try again.";
                return false;
            }

            var roots = GetLibraryPaths().ToList();
            if (!TryNormalizeLibraryPath(path, roots, out var root, out error))
                return false;

            try
            {
                Directory.CreateDirectory(root!);
                // Delete the uniquely named probe even if writing it fails.
                using var probe = new FileStream(Path.Combine(root!, $".knossos-write-{Guid.NewGuid():N}"),
                    FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
                probe.WriteByte(0);
            }
            catch (Exception ex)
            {
                error = $"Knossos cannot write to '{root}': {ex.Message}";
                return false;
            }

            roots.Add(root!);
            return TrySaveLibraries(roots, out error);
        }

        public static bool RemoveLibrary(string path)
        {
            if (!CanChangeLibraries())
                return false;

            var roots = GetLibraryPaths().ToList();
            if (roots.RemoveAll(root => LibraryPathComparer.Equals(root, path)) == 0)
                return false;
            return TrySaveLibraries(roots, out _);
        }

        private static bool CanChangeLibraries() =>
            initIsComplete && libraryLoadInProgress == 0 && !ModInstallOperation.HasPendingVersions &&
            TaskViewModel.Instance?.IsSafeState() != false;

        private static bool TrySaveLibraries(List<string> roots, out string? error)
        {
            var previousPaths = globalSettings.libraryPaths;
            var previousBasePath = globalSettings.basePath;
            globalSettings.libraryPaths = roots;
            if (!globalSettings.TrySave(writeIni: false))
            {
                globalSettings.libraryPaths = previousPaths;
                globalSettings.basePath = previousBasePath;
                error = "Knossos could not save the library settings.";
                return false;
            }

            error = null;
            ResetBasePath();
            Dispatcher.UIThread.Post(() => LibraryPathsChanged?.Invoke());
            return true;
        }

        private static bool TryNormalizeLibraryPath(string? path, IReadOnlyList<string> roots,
            out string? root, out string? error)
        {
            root = null;
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("The library path is empty.");
                root = NormalizeLibraryPath(path);
                var physicalRoot = ResolveLibraryPath(root);
                foreach (var existing in roots)
                {
                    var physicalExisting = ResolveLibraryPath(existing);
                    if (ContainsPath(existing, root) || ContainsPath(root, existing) ||
                        ContainsPath(physicalExisting, physicalRoot) || ContainsPath(physicalRoot, physicalExisting))
                    {
                        error = $"'{root}' overlaps the library at '{existing}'. Select a separate folder.";
                        return false;
                    }
                }
                error = null;
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
            {
                error = $"Cannot use '{path}' as a library: {ex.Message}";
                return false;
            }
        }

        private static string NormalizeLibraryPath(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

        private static bool ContainsPath(string root, string path, StringComparer? comparer = null)
        {
            comparer ??= LibraryPathComparer;
            if (comparer.Equals(root, path))
                return true;
            var prefixLength = root.Length + (Path.EndsInDirectorySeparator(root) ? 0 : 1);
            return path.Length > prefixLength && comparer.Equals(root, path[..root.Length]) &&
                   (Path.EndsInDirectorySeparator(root) || path[root.Length] == Path.DirectorySeparatorChar);
        }

        private static string ResolveLibraryPath(string path)
        {
            var directory = new DirectoryInfo(path);
            var parts = new Stack<DirectoryInfo>();
            for (var part = directory; part != null; part = part.Parent)
                parts.Push(part);

            var resolved = parts.Pop().FullName;
            while (parts.TryPop(out var component))
            {
                var next = new DirectoryInfo(Path.Combine(resolved, component.Name));
                resolved = next.Exists && (next.Attributes & FileAttributes.ReparsePoint) != 0
                    ? next.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? next.FullName
                    : next.FullName;
            }
            return NormalizeLibraryPath(resolved);
        }
    }
}
