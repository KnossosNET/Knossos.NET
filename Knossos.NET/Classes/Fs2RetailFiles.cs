using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Knossos.NET.Classes
{
    internal static class Fs2RetailFiles
    {
        internal static readonly string[] Required =
        {
            "root_fs2.vp", "smarty_fs2.vp", "sparky_fs2.vp",
            "sparky_hi_fs2.vp", "stu_fs2.vp", "tango1_fs2.vp",
            "tango2_fs2.vp", "tango3_fs2.vp", "warble_fs2.vp"
        };

        internal static readonly string[] Optional =
        {
            "hud_1.hcf", "hud_2.hcf", "hud_3.hcf", "movies_fs2.vp", "multi-mission-pack.vp", "multi-voice-pack.vp", "bastion.ogg", "colossus.ogg",
            "endpart1.ogg", "endprt2a.ogg", "endprt2b.ogg", "intro.ogg", "mono1.ogg", "mono2.ogg", "mono3.ogg", "mono4.ogg", "bastion.mve",
            "colossus.mve", "endpart1.mve", "endprt2a.mve", "endprt2b.mve", "intro.mve", "mono1.mve", "mono2.mve", "mono3.mve", "mono4.mve"
        };

        internal const string Tile = "kn_tile.png";
        internal const string Banner = "kn_banner.png";
        internal const string Metadata = "mod.json";
        internal static readonly string[] Screenshots = Enumerable.Range(0, 12).Select(index => $"kn_screen_{index}.jpg").ToArray();
        internal static StringComparer FileNameComparer { get; } = StringComparer.OrdinalIgnoreCase;

        internal static bool HasRootPack(string folder) => Directory.Exists(folder) &&
            Directory.EnumerateFiles(folder).Any(path =>
                string.Equals(Path.GetFileName(path), "root_fs2.vp", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(path), "root_fs2.vpc", StringComparison.OrdinalIgnoreCase));

        internal static IEnumerable<string> KnownInstalledPaths()
        {
            var packsAndMedia = Required.Concat(Optional);
            return packsAndMedia.Concat(packsAndMedia.Where(name => Path.GetExtension(name) == ".vp")
                    .Select(name => Path.ChangeExtension(name, ".vpc")))
                .Concat(Screenshots)
                .Concat(new[] { Tile, Banner, Metadata, "mod_settings.json" })
                .Select(name => Path.Combine(InstalledSubfolder(name), name));
        }

        private static string InstalledSubfolder(string name) => Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".hcf" => Path.Combine("data", "players"),
            ".ogg" or ".mve" => Path.Combine("data", "movies"),
            _ => string.Empty
        };

        internal static void CheckDestination(string destination, IEnumerable<string> paths)
        {
            foreach (var group in paths.GroupBy(path => Path.GetDirectoryName(path) ?? string.Empty))
            {
                EnsureOrdinaryDirectoryPath(destination, group.Key);
                var directory = Path.Combine(destination, group.Key);
                if (!Directory.Exists(directory)) continue;

                var names = group.Select(Path.GetFileName).ToHashSet(FileNameComparer);
                if (group.Key.Length == 0)
                    names.UnionWith(new[] { "mod.ini", "tool.json", "mod_settings.json", "root_fs2.vp", "root_fs2.vpc" });
                var existing = Directory.EnumerateFileSystemEntries(directory)
                    .FirstOrDefault(path => names.Contains(Path.GetFileName(path)));
                if (existing != null)
                    throw new IOException($"The destination already contains '{existing}'.");
            }
        }

        internal static void EnsureOrdinaryDirectoryPath(string root, string subfolder)
        {
            EnsureOrdinaryDirectory(root);
            foreach (var part in subfolder.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
            {
                root = Path.Combine(root, part);
                EnsureOrdinaryDirectory(root);
            }
        }

        private static void EnsureOrdinaryDirectory(string path)
        {
            if (File.Exists(path) ||
                (Directory.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0))
                throw new IOException($"Cannot use a linked directory or file at '{path}' for FreeSpace 2.");
        }
    }
}
