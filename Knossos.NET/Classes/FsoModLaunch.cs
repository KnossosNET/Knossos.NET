using Knossos.NET.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Knossos.NET.Classes
{
    internal sealed record FsoModRoot(string Path, Mod Mod);

    internal sealed record FsoModLaunch(string WorkingFolder, List<FsoModRoot> Roots, bool UsesMultipleLibraries)
    {
        public string GetLegacyModFlag(Mod mod)
        {
            return string.Join(",", Roots.Select(root => root.Path).Select(path => !mod.devMode && path == mod.fullPath
                ? mod.folderName
                : Path.GetRelativePath(WorkingFolder, path).TrimEnd('/', '\\')));
        }

        public static FsoModLaunch? Create(Mod mod, List<Mod> dependencies, bool standaloneServer)
        {
            if (string.IsNullOrEmpty(mod.fullPath))
                return null;

            var library = Knossos.GetLibraryForPath(mod.fullPath);
            var parent = FindParent(mod, library, dependencies);
            var workingFolder = GetWorkingFolder(mod, parent);
            if (workingFolder == null)
                return null;

            var loadedMods = new List<Mod>(dependencies) { mod };
            if (parent != null)
                loadedMods.Add(parent);

            var roots = new List<FsoModRoot>();
            foreach (var id in mod.GetModFlagList())
            {
                if (id == mod.id)
                {
                    if (mod.devMode)
                    {
                        foreach (var pkg in mod.packages.Where(pkg => pkg.isEnabled).OrderBy(pkg => pkg.name))
                            roots.Add(new(Path.Combine(mod.fullPath, pkg.folder ?? ""), mod));

                        // The dedicated server reads multi.cfg from the mod root's data directory.
                        if (standaloneServer)
                            roots.Add(new(mod.fullPath, mod));
                    }
                    else if (id != "FS2")
                    {
                        roots.Add(new(mod.fullPath, mod));
                    }
                    continue;
                }

                var dependency = dependencies.FirstOrDefault(candidate => candidate.id == id) ?? FindOptionalMod(id);
                if (dependency == null)
                    continue;

                loadedMods.Add(dependency);
                if (dependency.devMode)
                {
                    foreach (var pkg in dependency.packages)
                        roots.Add(new(Path.Combine(dependency.fullPath, pkg.folder ?? ""), dependency));
                }
                else
                {
                    roots.Add(new(dependency.fullPath, dependency));
                }
            }

            var multipleLibraries = loadedMods.Any(candidate =>
                !Knossos.LibraryPathComparer.Equals(library, Knossos.GetLibraryForPath(candidate.fullPath)));
            if (multipleLibraries && mod.id != "FS2" && mod.parent != "FS2" &&
                !Knossos.LibraryPathComparer.Equals(library, Knossos.GetLibraryForPath(workingFolder)))
            {
                workingFolder = GetContainingFolder(mod.fullPath) ?? workingFolder;
            }

            return new FsoModLaunch(workingFolder, roots, multipleLibraries);
        }

        private static Mod? FindParent(Mod mod, string? library, List<Mod> dependencies)
        {
            if (mod.type != ModType.mod || mod.parent == null || mod.parent == mod.id)
                return null;

            var parents = Knossos.GetInstalledModList(mod.parent).Where(candidate => candidate.type == ModType.tc).ToList();
            if (mod.parent == "FS2")
            {
                var retail = parents.FirstOrDefault(candidate => Fs2RetailFiles.HasRootPack(candidate.fullPath));
                if (retail != null)
                    return retail;
            }
            return dependencies.FirstOrDefault(candidate => candidate.id == mod.parent && candidate.type == ModType.tc)
                ?? parents.LastOrDefault(candidate => Knossos.LibraryPathComparer.Equals(library, Knossos.GetLibraryForPath(candidate.fullPath)))
                ?? parents.FirstOrDefault();
        }

        private static string? GetWorkingFolder(Mod mod, Mod? parent)
        {
            if (mod.type == ModType.tc || mod.parent == mod.id)
                return mod.parent == null ? GetContainingFolder(mod.fullPath) : mod.fullPath;

            if (parent == null)
                return null;

            return parent.parent == null ? GetContainingFolder(mod.fullPath) : parent.fullPath;
        }

        private static string? GetContainingFolder(string path) => Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(path));

        private static Mod? FindOptionalMod(string id)
        {
            // A mod flag can request an installed mod without declaring it as a dependency.
            try
            {
                var mod = Knossos.GetInstalledModList(id).MaxBy(candidate => new SemanticVersion(candidate.version));
                Log.Add(Log.LogSeverity.Information, "Knossos.PlayMod()", mod == null
                    ? "Optional dependency is not installed: " + id
                    : "Loading optional dependency: " + mod);
                return mod;
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Knossos.PlayMod(optional dependency)", ex);
                return null;
            }
        }
    }
}
