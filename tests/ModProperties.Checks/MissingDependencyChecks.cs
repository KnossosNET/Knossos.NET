using Knossos.NET;
using Knossos.NET.Models;
using Knossos.NET.ViewModels;

static partial class Program
{
    static void MissingDependencyChecks(string output)
    {
        var settings = Knossos.NET.Knossos.globalSettings;
        var previousBuilds = FsoBuildsViewModel.Instance;
        var library = Directory.CreateDirectory(Path.Combine(output, "missing-dependency-library")).FullName;
        Knossos.NET.Knossos.globalSettings = new() { basePath = library, modCompression = CompressionSettings.Disabled };
        try
        {
            var catalog = new Catalog { CanDevelop = false, UseActualInstalled = true };
            _ = new TaskViewModel();
            var builds = new FsoBuildsViewModel();
            const string dependencyId = "missing-mainhalls-fixture";

            var requiredEngine = new ModDependency { id = "FSO", version = "~23.2.1" };
            var requiredMainhalls = new ModDependency { id = dependencyId, version = "~1.4.10" };
            var campaign = Make("campaign-missing-dependency-fixture", "3.3.3",
                Pkg("core", "required", requiredEngine), Pkg("mainhalls", "recommended", requiredMainhalls));
            var mainhalls = Make(dependencyId, "1.4.10",
                Pkg("audio", "required", new ModDependency { id = "FSO", version = ">=19.0.0" }),
                Pkg("galatea", "recommended"));
            mainhalls.type = ModType.mod;
            mainhalls.parent = "FS2";
            var newerMainhalls = Copy(mainhalls);
            newerMainhalls.version = "1.7.3";

            var engine = Make("FSO", "23.2.1", Pkg("platform", "required"));
            engine.type = ModType.engine;
            engine.stability = "stable";
            var platform = KnUtils.IsWindows ? "windows" : KnUtils.IsLinux ? "linux" : "macosx";
            var arch = KnUtils.CpuArch == "X64" ? "X86_64" : KnUtils.CpuArch == "Arm64" ? "ARM64" : "X86";
            engine.packages[0].environment = platform + " && " + arch;

            foreach (var mod in new[] { campaign, mainhalls, newerMainhalls, engine })
            {
                foreach (var package in mod.packages)
                    package.files = Array.Empty<ModFile>();
                catalog.Available.Add(Copy(mod));
            }

            foreach (var mod in new[] { campaign, newerMainhalls })
            {
                mod.installed = true;
                mod.fullPath = Directory.CreateDirectory(Path.Combine(library, mod.id + "-" + mod.version)).FullName;
                mod.SaveJson();
                Knossos.NET.Knossos.AddMod(mod);
            }

            using var properties = new ModPropertiesViewModel(campaign,
                menuPageEntry: ModPropertiesViewModel.MenuPageEntry.Versions, catalog: catalog);
            var versions = properties.Versions;
            Pump(versions.CurrentLoad);
            Check(versions.CanApply && versions.Root!.Dependencies.Any(edge =>
                edge.Node.Mod.id == dependencyId && edge.Node.Mod.version == "1.4.10" && edge.Node.Installed == null),
                "Apply Changes detects the missing pinned dependency on the initial plan");
            Check(builds.StableItems.Count == 0, "the planned FSO build has no item in the Engine tab yet");
            Pump(versions.ApplyCommand.ExecuteAsync(null));

            var installed = Knossos.NET.Knossos.GetInstalledMod(dependencyId, "1.4.10");
            Check(installed?.packages.Any(package => package.name == "audio") == true &&
                Knossos.NET.Knossos.GetInstalledMod(dependencyId, "1.7.3") == newerMainhalls,
                "the first Apply installs the requested version without package toggles and retains the newer version");
            Check(builds.StableItems.Any(item => item.CompareIdAndVersionToMod(engine) && item.IsInstalled),
                "the successfully installed FSO build appears in Engine");
        }
        finally
        {
            FsoBuildsViewModel.Instance = previousBuilds;
            Knossos.NET.Knossos.globalSettings = settings;
        }
    }
}
