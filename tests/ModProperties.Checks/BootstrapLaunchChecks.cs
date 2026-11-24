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
}
