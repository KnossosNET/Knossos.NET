using Knossos.NET.Classes;
using Knossos.NET.Models;
using System.Reflection;
using System.Text.Json;

static partial class Program
{
    const string CaptureBootstrapLaunchOption = "--capture-fso-launch";
    const string CapturedArgumentsFile = "fso-arguments.json";

    static bool CaptureBootstrapLaunch(string[] args)
    {
        if (args.Length == 0 || args[0] != CaptureBootstrapLaunchOption) return false;
        // Skip the probe option and the executable forwarded by the prefix command.
        File.WriteAllText(CapturedArgumentsFile, JsonSerializer.Serialize(args.Skip(2)));
        return true;
    }

    static void BootstrapLaunchChecks(string output)
    {
        var androidArgs = typeof(AndroidHelper).GetMethod("GetLaunchArguments", BindingFlags.NonPublic | BindingFlags.Static)!;
        const string androidPath = "/data/user/0/com.knossosnet.knossosnet/files/FSO prefs/bootstrap.json";
        var arguments = (List<string>)androidArgs.Invoke(null, new object?[] { " -window -aa", androidPath })!;
        Check(arguments.SequenceEqual(new[] { "-window", "-aa", "-bootstrap", androidPath }),
            "the Android launch passes -bootstrap and the full path as two arguments");
        arguments = (List<string>)androidArgs.Invoke(null, new object?[] { " -window -mod MVPS-3.8.2", null })!;
        Check(arguments.SequenceEqual(new[] { "-window", "-mod", "MVPS-3.8.2" }),
            "an Android launch within one library keeps the ordinary -mod arguments");

        var workingFolder = Directory.CreateDirectory(Path.Combine(output, "launch workfolder")).FullName;
        var bootstrapPath = Path.Combine(output, "FSO preferences", "bootstrap.json");
        var settings = Knossos.NET.Knossos.globalSettings;
        var prefix = settings.prefixCMD;
        try
        {
            settings.prefixCMD = $"dotnet \"{typeof(Program).Assembly.Location}\" {CaptureBootstrapLaunchOption}";
            var build = new FsoBuild(Environment.ProcessPath!);
            var launch = build.RunFSO(FsoExecType.Release, "-window -aa", workingFolder,
                waitForExit: true, bootstrapPath: bootstrapPath);
            Pump(launch);
            var report = Path.Combine(workingFolder, CapturedArgumentsFile);
            Check(launch.Result.IsSuccess && File.Exists(report) &&
                JsonSerializer.Deserialize<string[]>(File.ReadAllText(report))!.SequenceEqual(
                    new[] { "-window", "-aa", "-bootstrap", bootstrapPath }),
                "the desktop process receives the explicit bootstrap path and retains its working folder");
        }
        finally
        {
            settings.prefixCMD = prefix;
        }
    }

    static void BootstrapManifestChecks(string firstLibrary, string secondLibrary)
    {
        var dependency = new Mod
        {
            id = "shared-content", type = ModType.mod, version = "1.4.0", title = "Shared Content",
            fullPath = Path.Combine(secondLibrary, "shared-content-1.4.0")
        };
        var primary = new Mod
        {
            id = "primary-tc", type = ModType.tc, version = "2.5.0", title = "Conversión Principal",
            parent = "primary-tc", devMode = true, fullPath = Path.Combine(firstLibrary, "primary-tc-2.5.0"),
            modFlag = new() { dependency.id, "primary-tc" },
            packages = new() { new ModPackage { name = "Audio", folder = "audio" }, new ModPackage { name = "Core", folder = "core" } }
        };

        var assembly = typeof(FsoBuild).Assembly;
        var launchType = assembly.GetType("Knossos.NET.Classes.FsoModLaunch")!;
        var launch = launchType.GetMethod("Create")!.Invoke(null, new object[] { primary, new List<Mod> { dependency }, false })!;
        Check((bool)launchType.GetProperty("UsesMultipleLibraries")!.GetValue(launch)!,
            "the dependency and primary package roots span two libraries");
        var legacyFlag = (string)launchType.GetMethod("GetLegacyModFlag")!.Invoke(launch, new object[] { primary })!;
        Check(legacyFlag == string.Join(",", Path.GetRelativePath(primary.fullPath, dependency.fullPath), "audio", "core"),
            "tracking root metadata preserves the original -mod ordering and paths");

        var serialize = assembly.GetType("Knossos.NET.Classes.FsoBootstrap")!
            .GetMethod("SerializeManifest", BindingFlags.NonPublic | BindingFlags.Static)!;
        var enginePaths = new[] { @"Z:\\shared-content", @"Z:\\primary-tc\\audio", @"Z:\\primary-tc\\core" };
        using var json = JsonDocument.Parse((string)serialize.Invoke(null, new object[] { 1, primary.id, launch, enginePaths })!);
        var manifest = json.RootElement;
        Check(manifest.GetProperty("version").GetInt32() == 1 && manifest.GetProperty("mod_id").GetString() == primary.id &&
            manifest.GetProperty("workfolder").ValueKind == JsonValueKind.Null,
            "the structured manifest keeps version 1, the primary ID and the null workfolder");

        var roots = manifest.GetProperty("roots");
        var expectedMods = new[] { dependency, primary, primary };
        Check(roots.GetArrayLength() == expectedMods.Length, "the manifest contains one object for every mount path");
        for (var index = 0; index < expectedMods.Length; index++)
        {
            var mod = expectedMods[index];
            var metadata = roots[index].GetProperty("metadata");
            Check(roots[index].GetProperty("path").GetString() == enginePaths[index] &&
                metadata.GetProperty("id").GetString() == mod.id && metadata.GetProperty("type").GetString() == mod.type.ToString() &&
                metadata.GetProperty("version").GetString() == mod.version && metadata.GetProperty("title").GetString() == mod.title,
                $"root {index + 1} pairs its engine path with the owning mod's complete metadata");
        }
    }
}
