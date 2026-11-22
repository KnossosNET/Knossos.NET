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
}
