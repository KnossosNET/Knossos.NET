using System;
using Knossos.NET.Models;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace Knossos.NET.Classes
{
    public struct WineResult
    {
        public bool IsSuccess;
        public string ErrorMessage;

        public WineResult(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public WineResult(bool isSuccess)
        {
            IsSuccess = isSuccess;
            ErrorMessage = string.Empty;
        }
    }

    /// <summary>
    /// Small helper class to run Windows executables on Linux with Wine
    /// </summary>
    public static class Wine
    {
        public static async Task<WineResult> RunTool(string exePath, string exeCmdLine, string? workingFolder, string arch)
        {
            try
            {
                var wineArch = GetWineArch(arch.ToLower());
                if (wineArch == null)
                {
                    return new WineResult(false, "Unsupported WINEARCH: " + arch);
                }

                var prefixResult = await SetWinePrefixFred2(wineArch); //use the same settings for fred2
                if (!prefixResult.IsSuccess)
                {
                    return prefixResult;
                }

                var winePrefix = GetWinePrefixPath();
                Log.Add(Log.LogSeverity.Information, "Wine.RunTool()", "Executing wine with the following cmdline: WINEPREFIX=" + winePrefix +
                    " WINEARCH=" + wineArch + " wine " + exePath + " " + exeCmdLine);

                using (var wine = new Process())
                {
                    wine.StartInfo.FileName = "wine";
                    wine.StartInfo.Arguments = $"\"{exePath}\" {exeCmdLine}";
                    if (workingFolder != null)
                    {
                        wine.StartInfo.WorkingDirectory = workingFolder;
                    }
                    wine.StartInfo.EnvironmentVariables["WINEPREFIX"] = winePrefix;
                    wine.StartInfo.EnvironmentVariables["WINEARCH"] = wineArch;
                    wine.StartInfo.UseShellExecute = false;
                    wine.Start();
                    await wine.WaitForExitAsync();
                }

                return new WineResult(true);
            }
            catch (Win32Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Wine.RunTool()", "Wine not found or another error ocurred: " + ex.Message);
                return new WineResult(false, "Wine not found or another error ocurred: " + ex.Message);
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Wine.RunTool()", ex.ToString());
                return new WineResult(false, ex.ToString());
            }
        }

        public static async Task<WineResult> RunFred2(string exePath, string exeCmdLine, string? workingFolder, FsoExecArch fsoArch,
            bool waitForExit = true)
        {
            try
            {
                var startInfo = await PrepareFred2Async(exePath, exeCmdLine, workingFolder, fsoArch).ConfigureAwait(false);
                using var wine = Process.Start(startInfo)!;
                if (waitForExit)
                    await wine.WaitForExitAsync();

                return new WineResult(true);
            }
            catch (Win32Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Wine.RunFred2()", "Wine not found or another error ocurred: " + ex.Message);
                return new WineResult(false, "Wine not found or another error ocurred: " + ex.Message);
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Wine.RunFred2()", ex.ToString());
                return new WineResult(false, ex.ToString());
            }
        }

        internal static async Task<ProcessStartInfo> PrepareFred2Async(string executable, string args, string? workingFolder,
            FsoExecArch architecture)
        {
            var wineArch = GetWineArch(architecture) ?? throw new InvalidOperationException("Unsupported WINEARCH: " + architecture);
            var prefix = await SetWinePrefixFred2(wineArch).ConfigureAwait(false);
            if (!prefix.IsSuccess)
                throw new InvalidOperationException(prefix.ErrorMessage);

            var startInfo = CreateStartInfo(workingFolder, wineArch);
            startInfo.Arguments = $"\"{executable}\" {args}";
            Log.Add(Log.LogSeverity.Information, "Wine.PrepareFred2Async",
                "Executing wine with the following command line: " + startInfo.Arguments);
            return startInfo;
        }

        internal static async Task<string[]> ConvertPathsAsync(string workingFolder, FsoExecArch architecture,
            bool toWindows, params string[] paths)
        {
            var wineArch = GetWineArch(architecture) ?? throw new InvalidOperationException("Unsupported WINEARCH: " + architecture);
            var startInfo = CreateStartInfo(workingFolder, wineArch);
            startInfo.ArgumentList.Add("winepath.exe");
            startInfo.ArgumentList.Add("-0");
            startInfo.ArgumentList.Add(toWindows ? "-w" : "-u");
            startInfo.ArgumentList.Add("--");
            foreach (var path in paths)
                startInfo.ArgumentList.Add(path);

            var (output, error, exitCode) = await FsoBuild.ReadProcessOutputAsync(startInfo, CancellationToken.None).ConfigureAwait(false);
            var converted = output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
            if (exitCode != 0 || converted.Length != paths.Length || converted.Any(string.IsNullOrWhiteSpace))
                throw new IOException("Wine could not convert the FSO paths. " + error);

            return converted;
        }

        private static ProcessStartInfo CreateStartInfo(string? workingFolder, string wineArch)
        {
            var startInfo = new ProcessStartInfo("wine") { UseShellExecute = false };
            if (workingFolder != null)
                startInfo.WorkingDirectory = workingFolder;
            startInfo.EnvironmentVariables["WINEPREFIX"] = GetWinePrefixPath();
            startInfo.EnvironmentVariables["WINEARCH"] = wineArch;
            return startInfo;
        }

        /// <summary>
        /// Sets the WinePrefix folder with the correct configuration for Fred2
        /// </summary>
        private static async Task<WineResult> SetWinePrefixFred2(string wineArch)
        {
            try
            {
                var winePrefix = GetWinePrefixPath();
                Log.Add(Log.LogSeverity.Information,"Wine.SetWinePrefixFred2()","Executing wine with the following cmdline: WINEPREFIX=" + winePrefix +
                    " WINEARCH=" + wineArch + " WINEDLLOVERRIDES=mscoree=d wine wineboot");
                using (var wine = new Process())
                {
                    wine.StartInfo.FileName = "wine";
                    wine.StartInfo.Arguments = "wineboot";
                    wine.StartInfo.EnvironmentVariables["WINEPREFIX"] = winePrefix;
                    wine.StartInfo.EnvironmentVariables["WINEARCH"] = wineArch;
                    wine.StartInfo.EnvironmentVariables["WINEDLLOVERRIDES"] = "mscoree=d";
                    wine.StartInfo.UseShellExecute = false;
                    wine.Start();
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    try
                    {
                        await wine.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        if (!wine.HasExited)
                            wine.Kill(true);
                        return new WineResult(false, "Wine did not finish initializing its prefix.");
                    }
                    //Create a symlink to the Knossos library, this is not critical
                    try
                    {
                        var libraryPath = Knossos.GetKnossosLibraryPath();
                        if (libraryPath != null)
                        {
                            var linkLocation = Path.Combine(winePrefix, "drive_c", "users", Environment.UserName, "Favorites", "KnossosLibrary");
                            if (Directory.Exists(linkLocation))
                            {
                                //Note: This only deletes the symlink not the library folder itself! I tested it.
                                Directory.Delete(linkLocation);
                            }
                            Directory.CreateSymbolicLink(linkLocation, libraryPath);
                        }
                    }
                    catch (Exception ex) 
                    {
                        Log.Add(Log.LogSeverity.Warning, "Wine.SetWinePrefixFred2(symlink)", ex.ToString());
                    }
                    return new WineResult(true);
                }

            }
            catch (Win32Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Wine.SetWinePrefixFred2()", "Wine not found or another error ocurred: " + ex.Message);
                return new WineResult(false, "Wine not found or another error ocurred: " + ex.Message);
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Wine.SetWinePrefixFred2()", ex.ToString());
                return new WineResult(false, ex.ToString());
            }
        }

        /// <summary>
        /// Determine WINEARCH from FsoExecArch
        /// </summary>
        /// <param name="fsoArch"></param>
        /// <returns>WineArch compatible string or null</returns>
        private static string? GetWineArch(FsoExecArch fsoArch)
        {
            switch(fsoArch)
            {
                case FsoExecArch.x86:
                case FsoExecArch.x86_avx:
                case FsoExecArch.x86_avx2:
                case FsoExecArch.arm32:
                case FsoExecArch.riscv32:
                    return "win32";
                case FsoExecArch.x64:
                case FsoExecArch.x64_avx:
                case FsoExecArch.x64_avx2:
                case FsoExecArch.arm64:
                case FsoExecArch.riscv64:
                    return "win64";
                default: 
                    return null;
            }
        }

        /// <summary>
        /// Determine WINEARCH from Arch string (tools)
        /// </summary>
        /// <param name="archString"></param>
        /// <returns>WineArch compatible string or null</returns>
        private static string? GetWineArch(string archString)
        {
            switch (archString)
            {
                case "x86":
                    return "win32";
                case "x64":
                case "arm64":
                case "riscv64":
                    return "win64";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Get the WinePrefix directory, a "wine" directory inside Knet data folder
        /// </summary>
        /// <returns>WinePrefix string</returns>
        internal static string GetWinePrefixPath()
        {
            return Path.Combine(KnUtils.GetKnossosDataFolderPath(), "wine");
        }
    }
}
