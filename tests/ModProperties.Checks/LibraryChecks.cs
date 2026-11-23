using Knossos.NET.Models;
using Knossos.NET.ViewModels;
using System.Reflection;

static partial class Program
{
    static void LibraryChecks(string output)
    {
        var app = typeof(Knossos.NET.Knossos);
        var previousSettings = Knossos.NET.Knossos.globalSettings;
        var first = Directory.CreateDirectory(Path.Combine(output, "library-first")).FullName;
        var second = Directory.CreateDirectory(Path.Combine(output, "library-second")).FullName;
        var settings = new GlobalSettings { basePath = first };
        var normalize = app.GetMethod("NormalizeLibrarySettings", BindingFlags.NonPublic | BindingFlags.Static)!;
        var scan = app.GetMethod("FolderSearchRecursive", BindingFlags.NonPublic | BindingFlags.Static)!;
        const string id = "test-multiple-libraries";

        Mod CreateVersion(string library, string version)
        {
            var mod = Make(id, version);
            mod.fullPath = Directory.CreateDirectory(Path.Combine(library, id, id + "-" + version)).FullName;
            mod.SaveJson();
            File.WriteAllText(Path.Combine(mod.fullPath, "payload.txt"), library);
            return mod;
        }

        try
        {
            Knossos.NET.Knossos.globalSettings = settings;
            normalize.Invoke(null, new object[] { settings });
            Check(settings.libraryPaths?.SequenceEqual(new[] { first }) == true && settings.basePath == first,
                "legacy basePath becomes the first library and stays in settings");

            settings.libraryPaths = new() { first, Path.Combine(first, "nested"), Path.Combine(first, "."), second };
            normalize.Invoke(null, new object[] { settings });
            Check(Knossos.NET.Knossos.GetLibraryPaths().SequenceEqual(new[] { first, second }),
                "nested and repeated libraries are rejected while order is preserved");

            var original = CreateVersion(first, "1.0.0");
            var duplicate = CreateVersion(second, "1.0.0");
            var otherVersion = CreateVersion(second, "2.0.0");
            foreach (var root in Knossos.NET.Knossos.GetLibraryPaths())
                Pump((Task)scan.Invoke(null, new object[] { root, true, 0 })!);

            var current = Knossos.NET.Knossos.GetInstalledMod(id, original.version)!;
            Check(Knossos.NET.Knossos.GetInstalledModList(id).Count == 2 &&
                Knossos.NET.Knossos.GetLibraryForPath(current.fullPath) == first,
                "scanning merges libraries and uses the first copy of a duplicate version");
            Check(Knossos.NET.Knossos.GetDuplicateModPaths(current).Select(Path.TrimEndingDirectorySeparator)
                    .SequenceEqual(new[] { duplicate.fullPath }),
                "the duplicate warning lists the ignored copy");

            var release = Make(id, "3.0.0", Pkg("core", "required"));
            release.packages[0].files = Array.Empty<ModFile>();
            var catalog = new Catalog { UseActualInstalled = true };
            catalog.Available.Add(release);
            using var install = new ModPropertiesViewModel(release,
                menuPageEntry: ModPropertiesViewModel.MenuPageEntry.Versions, catalog: catalog);
            Pump(install.Versions.CurrentLoad);
            Check(install.Versions.ShowInstallationLibrary && install.Versions.SelectedInstallationLibrary == second,
                "a new release defaults to its preceding installed version's library");

            _ = new TaskViewModel();
            install.Versions.SelectedInstallationLibrary = first;
            Pump(install.Versions.ApplyCommand.ExecuteAsync(null));
            Check(Knossos.NET.Knossos.GetLibraryForPath(Knossos.NET.Knossos.GetInstalledMod(id, release.version)?.fullPath) == first,
                "the selected library overrides the default and receives the installed version");

            var installed = Knossos.NET.Knossos.GetInstalledMod(id, otherVersion.version)!;
            using var properties = new ModPropertiesViewModel(installed);
            properties.MoveCommand.Execute(null);
            Check(properties.IsMoveModal && properties.SelectedMoveLibrary == first,
                "move dialog defaults to a different library");
            Pump(properties.ConfirmMoveCommand.ExecuteAsync(null));
            var movedFolder = Path.Combine(first, id, id + "-" + installed.version);
            Check(Path.TrimEndingDirectorySeparator(installed.fullPath) == movedFolder &&
                File.ReadAllText(Path.Combine(movedFolder, "payload.txt")) == second &&
                !Directory.Exists(otherVersion.fullPath),
                "the queued move retains the payload and updates the installed version's path");

            RetailMoveChecks(first, second, scan);
            BuildLibraryChecks(output, first, second);

            settings.libraryPaths = new();
            normalize.Invoke(null, new object[] { settings });
            Check(Knossos.NET.Knossos.GetLibraryPaths().Count == 0 && settings.basePath == null,
                "an explicit empty library list does not restore the previous basePath");
        }
        finally
        {
            Knossos.NET.Knossos.globalSettings = previousSettings;
        }
    }

    static void RetailMoveChecks(string first, string second, MethodInfo scan)
    {
        var retail = Make("FS2", "1.20.0");
        retail.parent = "FS2";
        retail.fullPath = first;
        retail.SaveJson();

        var child = Make("retail-child-fixture", "1.0.0");
        child.parent = "FS2";
        child.type = ModType.mod;
        var childFolder = Directory.CreateDirectory(Path.Combine(first, child.id)).FullName;
        child.fullPath = childFolder;
        child.SaveJson();
        File.WriteAllText(Path.Combine(childFolder, "child.vp"), "mod content");
        File.WriteAllText(Path.Combine(first, "SDL2.dll"), "unrelated file");

        string[] retailPaths = { "root_fs2.vp", "kn_tile.png", "kn_banner.png", "kn_screen_0.jpg",
            "mod_settings.json", Path.Combine("data", "movies", "intro.mve"),
            Path.Combine("data", "tables", "retail.tbl") };
        foreach (var path in retailPaths)
        {
            var file = Path.Combine(first, path);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, path == "mod_settings.json" ? "{}" : path);
        }
        Directory.CreateDirectory(Path.Combine(first, "data", "empty"));

        var destination = Path.Combine(second, "FS2");
        Directory.CreateDirectory(Path.Combine(destination, "data", "tables"));
        var destinationContent = Path.Combine(destination, "data", "tables", "existing.tbl");
        File.WriteAllText(destinationContent, "already at destination");
        var destinationMod = Directory.CreateDirectory(Path.Combine(destination, "other-mod")).FullName;
        File.WriteAllText(Path.Combine(destinationMod, "other.vp"), "destination mod content");

        Pump((Task)scan.Invoke(null, new object[] { first, true, 0 })!);
        var installed = Knossos.NET.Knossos.GetInstalledMod(retail.id, retail.version)!;
        Check(Path.TrimEndingDirectorySeparator(installed.fullPath) == first,
            "the scanner finds legacy Retail FS2 at the library root");

        var conflictingPath = Path.Combine(destination, "data", "tables", "retail.tbl");
        File.WriteAllText(conflictingPath, "conflicting content");
        using (var conflictingMove = new ModPropertiesViewModel(installed))
        {
            conflictingMove.MoveCommand.Execute(null);
            Pump(conflictingMove.ConfirmMoveCommand.ExecuteAsync(null));
            Check(conflictingMove.Versions.Status.Contains("already contains") &&
                Path.TrimEndingDirectorySeparator(installed.fullPath) == first &&
                File.Exists(Path.Combine(first, "mod.json")) && !File.Exists(Path.Combine(destination, "mod.json")) &&
                File.ReadAllText(conflictingPath) == "conflicting content" &&
                retailPaths.All(path => File.Exists(Path.Combine(first, path))),
                "a conflicting data file aborts the retail move with both libraries intact");
        }
        File.Delete(conflictingPath);

        using var properties = new ModPropertiesViewModel(installed);
        properties.MoveCommand.Execute(null);
        Check(properties.IsMoveModal && properties.SelectedMoveLibrary == second,
            "legacy Retail FS2 can select the other library as its destination");
        Pump(properties.ConfirmMoveCommand.ExecuteAsync(null));
        Check(Path.TrimEndingDirectorySeparator(installed.fullPath) == destination,
            "the queued retail move updates the installed path: " + properties.Versions.Status);
        Check(File.Exists(Path.Combine(destination, "mod.json")) && !File.Exists(Path.Combine(first, "mod.json")) &&
            retailPaths.All(path => File.Exists(Path.Combine(destination, path)) && !File.Exists(Path.Combine(first, path))) &&
            File.ReadAllText(conflictingPath) == Path.Combine("data", "tables", "retail.tbl") &&
            Directory.Exists(Path.Combine(destination, "data", "empty")) && !Directory.Exists(Path.Combine(first, "data")),
            "retail packs, images, metadata and the entire data tree move to the new library");
        Check(File.Exists(Path.Combine(childFolder, "mod.json")) && File.Exists(Path.Combine(childFolder, "child.vp")) &&
            File.Exists(Path.Combine(first, "SDL2.dll")) && File.Exists(Path.Combine(destinationMod, "other.vp")) &&
            File.ReadAllText(destinationContent) == "already at destination" && !Directory.Exists(Path.Combine(destination, child.id)),
            "the retail move preserves installed mods and unrelated files in both libraries");
    }
}
