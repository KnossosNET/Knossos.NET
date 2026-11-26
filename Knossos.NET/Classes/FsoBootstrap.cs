using Knossos.NET.Models;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Knossos.NET.Classes
{
    internal static class FsoBootstrap
    {
        internal const string CommandLineOption = "-bootstrap";
        private const string FileName = "bootstrap.json";
        private const int LatestSupportedVersion = 1;
        private static readonly SemaphoreSlim LaunchGate = new(1, 1);
        private sealed record ManagedBootstrap(string Path, string Hash, string? PreviousHash)
        {
            public bool Owns(string hash) => Hash == hash || PreviousHash == hash;
        }

        public static async Task<FsoResult> RunAsync(
            FsoBuild build, FsoExecType executableType, string cmdline, string modId, FsoModLaunch launch)
        {
            await LaunchGate.WaitAsync().ConfigureAwait(false);
            try
            {
                var workingFolder = launch.WorkingFolder;
                // Store ownership in the launcher data folder so it survives a restart
                // or a change in which preferences directory the engine reports.
                var registration = Path.Combine(KnUtils.GetKnossosDataFolderPath(), "managed-fso-bootstrap.json");
                var managed = ReadRegistration(registration);
                if (!launch.UsesMultipleLibraries)
                {
                    RemoveManagedBootstrap(managed, registration);
                    return await build.RunFSO(executableType, cmdline, workingFolder).ConfigureAwait(false);
                }

                var wineArch = build.GetWineArchitecture(executableType);
                var portableMode = KnUtils.SplitCmdLineFlags(cmdline)?.Any(flag =>
                    string.Equals(flag.Trim(), "portable_mode", StringComparison.OrdinalIgnoreCase)) == true;
                var (maximumVersion, preferencesPath) = await build.GetBootstrapSupportAsync(executableType, workingFolder, portableMode)
                    .ConfigureAwait(false);
                if (maximumVersion == 0)
                {
                    return new FsoResult(false, $"This mod uses content from multiple libraries, and FSO {build.version} does not report support for multiple libraries. " +
                        "Select a compatible engine build or move the mod and its dependencies into the same library.");
                }

                var path = await ResolveBootstrapPathAsync(preferencesPath, workingFolder, portableMode, wineArch)
                    .ConfigureAwait(false);
                if (managed != null && !Knossos.LibraryPathComparer.Equals(managed.Path, path))
                {
                    RemoveManagedBootstrap(managed, registration);
                    managed = null;
                }

                var existing = File.Exists(path) ? File.ReadAllText(path) : null;
                var existingHash = existing == null ? null : GetHash(existing);
                if (existingHash != null && managed?.Owns(existingHash) != true)
                    return ExistingBootstrapError(path);

                var roots = launch.Roots.Select(root => root.Path);
                var winePaths = wineArch.HasValue
                    ? await Wine.ConvertPathsAsync(workingFolder, wineArch.Value, toWindows: true,
                        roots.Prepend(path).Select(Path.GetFullPath).ToArray()).ConfigureAwait(false)
                    : null;
                var bootstrapArgumentPath = winePaths?[0] ?? path;
                var rootPaths = winePaths != null ? winePaths[1..] : roots.Select(FormatEnginePath).ToArray();
                var version = Math.Min(maximumVersion, LatestSupportedVersion);
                var contents = SerializeManifest(version, modId, launch, rootPaths);
                var current = new ManagedBootstrap(path, GetHash(contents), existingHash);
                if (contents != existing)
                {
                    // Register both hashes before replacing the file. If interrupted,
                    // the next launch can still recognize either version as its own.
                    WriteFileAtomically(registration, JsonSerializer.Serialize(current));
                    WriteFileAtomically(path, contents);
                }
                Log.Add(Log.LogSeverity.Information, "FsoBootstrap", "Used bootstrap file: " + path);
                Log.Add(Log.LogSeverity.Information, "FsoBootstrap", $"Using bootstrap version: {version}");
                Log.Add(Log.LogSeverity.Information, "FsoBootstrap", $"Using mod ID: {modId}");
                Log.Add(Log.LogSeverity.Information, "FsoBootstrap", $"Using roots (in order, count: {rootPaths.Length})");
                for (var index = 0; index < rootPaths.Length; index++)
                    Log.Add(Log.LogSeverity.Information, "FsoBootstrap", $"Root {index + 1}: {rootPaths[index]}");

                var result = await build.RunFSO(executableType, cmdline, workingFolder, bootstrapPath: bootstrapArgumentPath)
                    .ConfigureAwait(false);
                if (!result.IsSuccess)
                    RemoveManagedBootstrap(current, registration);
                return result;
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "FsoBootstrap", ex);
                return new FsoResult(false, "Unable to prepare the FSO bootstrap file. " + ex.Message);
            }
            finally
            {
                LaunchGate.Release();
            }
        }

        private static string SerializeManifest(int version, string modId, FsoModLaunch launch, string[] rootPaths)
        {
            return JsonSerializer.Serialize(new
            {
                version,
                mod_id = modId,
                workfolder = (string?)null,
                roots = launch.Roots.Select((root, index) => new
                {
                    path = rootPaths[index],
                    metadata = new
                    {
                        id = root.Mod.id,
                        type = (root.Mod.type ?? ModType.mod).ToString(),
                        version = root.Mod.version,
                        title = root.Mod.title
                    }
                })
            });
        }

        private static FsoResult ExistingBootstrapError(string path) =>
            new(false, "There is a bootstrap.json at " + path +
                " that Knossos did not create. Move that file out of FSO's preferences folder before launching this mod.");

        private static async Task<string> ResolveBootstrapPathAsync(
            string? reportedPath, string workingFolder, bool portableMode, FsoExecArch? wineArch)
        {
            if (string.IsNullOrWhiteSpace(reportedPath))
            {
                if (portableMode)
                    return GetBootstrapPath(workingFolder, workingFolder);
                if (wineArch.HasValue)
                    throw new IOException("The Windows FRED build did not report its Wine preferences directory.");
                return GetBootstrapPath(GetConfiguredPreferences(), workingFolder);
            }

            if (wineArch.HasValue)
            {
                var converted = await Wine.ConvertPathsAsync(workingFolder, wineArch.Value, toWindows: false, reportedPath)
                    .ConfigureAwait(false);
                reportedPath = converted[0];
            }
            return GetBootstrapPath(reportedPath, workingFolder);
        }

        private static string GetConfiguredPreferences()
        {
            if (!KnUtils.IsAndroid)
            {
                const string preferenceSetting = "FSO_PREFERENCES_PATH";
                foreach (var envVar in Knossos.globalSettings.envVars.Split(',').Reverse())
                {
                    var parts = envVar.Split('=');
                    if (parts.Length == 2 && parts[0] == preferenceSetting && !string.IsNullOrWhiteSpace(parts[1]))
                        return parts[1];
                }
                var overridden = FsoBuild.GetPreferencesPathOverride() ?? Environment.GetEnvironmentVariable(preferenceSetting);
                if (!string.IsNullOrWhiteSpace(overridden))
                    return overridden;
            }
            return KnUtils.GetFSODataFolderPath();
        }

        private static string GetBootstrapPath(string preferencesFolder, string workingFolder) =>
            Path.GetFullPath(Path.Combine(preferencesFolder, FileName), Path.GetFullPath(workingFolder));

        private static string FormatEnginePath(string path) =>
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(path)).Replace(Path.DirectorySeparatorChar, '/');

        private static string GetHash(string contents) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contents)));

        private static ManagedBootstrap? ReadRegistration(string registration) => File.Exists(registration)
            ? JsonSerializer.Deserialize<ManagedBootstrap>(File.ReadAllText(registration))
            : null;

        private static void RemoveManagedBootstrap(ManagedBootstrap? managed, string registration)
        {
            if (managed == null)
                return;

            if (File.Exists(managed.Path) && managed.Owns(GetHash(File.ReadAllText(managed.Path))))
                File.Delete(managed.Path);

            File.Delete(registration);
        }

        private static void WriteFileAtomically(string path, string contents)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, contents);
                File.Move(temporary, path, overwrite: true);
            }
            finally
            {
                File.Delete(temporary);
            }
        }
    }
}
