using Knossos.NET.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;

namespace Knossos.NET
{
    /// <summary>
    /// Utility class
    /// Only static methods
    /// </summary>
    public static class KnUtils
    {
        private struct LegacySettings
        {
            public string? base_path { get; set; }
        }

        private static readonly bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static readonly bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        private static readonly bool isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        private static readonly bool cpuAVX = Avx.IsSupported;
        private static readonly bool cpuAVX2 = Avx2.IsSupported;
        private static string fsoPrefPath = string.Empty;

        public static bool IsWindows => isWindows;
        public static bool IsLinux => isLinux;
        public static bool IsMacOS => isMacOS;
        public static string CpuArch => cpuArch;
        public static bool CpuAVX => cpuAVX;
        public static bool CpuAVX2 => cpuAVX2;

        /// <summary>
        /// Possible Values:
        /// Arm	  //A 32-bit ARM processor architecture.
        /// Armv6 //A 32-bit ARMv6 processor architecture.
        /// Arm64 //A 64-bit ARM processor architecture.
        /// X64   //An Intel-based 64-bit processor architecture.
        /// X86   //An Intel-based 32-bit processor architecture.
        /// https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.architecture?view=net-6.0
        /// </summary>
        private static readonly string cpuArch = RuntimeInformation.OSArchitecture.ToString();

        /// <summary>
        /// The full path to KNET data folder
        /// </summary>
        /// <returns>fullpath as a string</returns>
        public static string GetKnossosDataFolderPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KnossosNET");
        }

        /// <summary>
        /// Gets the full path to FSO data folder
        /// Uses "fsoprefpath" provided that a compatible FSO executable is installed
        /// If an FSO executable is not found this method will try to guess the path to the data folder
        /// In Windows this is correct, but it may not on other OS.
        /// </summary>
        /// <returns>fullpath as string</returns>
        public static string GetFSODataFolderPath()
        {
            if (!string.IsNullOrEmpty(fsoPrefPath))
            {
                return fsoPrefPath;
            }
            else
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HardLightProductions", "FreeSpaceOpen");
            }

        }

        /// <summary>
        /// Saves fsoPrefPath so it can be used by GetFSODataFolderPath()
        /// </summary>
        /// <param name="path"></param>
        public static void SetFSODataFolderPath(string path)
        {
            //FSO responds with this if there is no ini
            if (path != @".\")
                fsoPrefPath = path;
        }

        /// <summary>
        /// Converts a DateTime into a timestamp string
        /// </summary>
        /// <param name="value"></param>
        /// <returns>timestamp string or string empty if fails</returns>
        public static String GetTimestamp(DateTime value)
        {
            try
            {
                return value.ToString("yyyyMMddHHmmssffff");
            }catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "KnUtils.GetTimestamp", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Try to get library fullpath from old Knossos data folder
        /// </summary>
        /// <returns>Knossos Library path or null if not found</returns>
        public static String? GetBasePathFromKnossosLegacy()
        {
            try
            {
                string jsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "knossos", "settings.json");
                if (File.Exists(jsonPath))
                {
                    using FileStream jsonFile = File.OpenRead(jsonPath);
                    var jsonObject = JsonSerializer.Deserialize<LegacySettings>(jsonFile)!;
                    jsonFile.Close();
                    return jsonObject.base_path;
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "KnUtils.GetBasePathFromKnossosLegacy", ex);
            }
            return null;
        }

        /// <summary>
        /// Executes a Chmod command in MacOSX and Linux
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="permissions"></param>
        /// <param name="recursive"></param>
        /// <returns>true if successfull, false otherwise</returns>
        public static bool Chmod(string filePath, string permissions = "+x", bool recursive = false)
        {
            if (IsWindows)
                return false;
            string cmd;
            if (recursive)
                cmd = $"chmod -R {permissions} '{filePath}'";
            else
                cmd = $"chmod {permissions} '{filePath}'";

            try
            {
                using (Process proc = Process.Start("/bin/bash", $"-c \"{cmd}\""))
                {
                    proc.WaitForExit();
                    return proc.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Formats a filesize value into a string with the right B/KB/MB/GB/TB suffix
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns>value in a string with a suffix or string.empty if fails</returns>
        public static string FormatBytes(long bytes)
        {
            try
            {
                string[] suffix = { "B", "KB", "MB", "GB", "TB" };
                int i;
                double dblSByte = bytes;
                for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
                {
                    dblSByte = bytes / 1024.0;
                }

                return String.Format("{0:0.##} {1}", dblSByte, suffix[i]);
            }catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "KnUtils.FormatBytes()", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Return the current OS name in a string format
        /// </summary>
        /// <returns>"Windows", "Linux", "MacOS" or "Unknown"</returns>
        public static string GetOSNameString()
        {
            if (IsWindows)
                return "Windows";
            if (IsLinux)
                return "Linux";
            if (IsMacOS)
                return "MacOS";
            return "Unknown";
        }

        /// <summary>
        /// Open a URL in a external browser
        /// </summary>
        /// <param name="url"></param>
        public static void OpenBrowserURL(string url)
        {
            try
            {
                using (var process = new Process())
                {
                    if (IsWindows)
                    {
                        process.StartInfo.FileName = "cmd";
                        process.StartInfo.Arguments = $"/c start {url}";
                    }
                    else if (IsLinux)
                    {
                        process.StartInfo.FileName = "xdg-open";
                        process.StartInfo.Arguments = url;
                    }
                    else if (IsMacOS)
                    {
                        process.StartInfo.FileName = "open";
                        process.StartInfo.Arguments = url;
                    }
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "KnUtils.OpenBrowser()", ex);
            }
        }

        /// <summary>
        /// Opens a folder using the operative system file browser
        /// </summary>
        /// <param name="path"></param>
        public static void OpenFolder(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = path,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "KnUtils.OpenFolder()", ex);
            }
        }

        /// <summary>
        /// Async directory copy helper method
        /// Support optional recursive copy and progressCallback that informs the name of the current file that is being copied
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="destinationDir"></param>
        /// <param name="recursive"></param>
        /// <param name="cancelSource"></param>
        /// <param name="progressCallback"></param>
        /// <returns></returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="TaskCanceledException"></exception>
        public static async Task CopyDirectoryAsync(string sourceDir, string destinationDir, bool recursive, CancellationTokenSource cancelSource, Action<string>? progressCallback = null)
        {
            await Task.Run(async () =>
            {
                var dir = new DirectoryInfo(sourceDir);

                if (!dir.Exists)
                    throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

                var dirs = dir.GetDirectories();

                Directory.CreateDirectory(destinationDir);

                foreach (var file in dir.GetFiles())
                {
                    if (cancelSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }
                    var targetFilePath = Path.Combine(destinationDir, file.Name);
                    if (progressCallback != null)
                    {
                        progressCallback(file.Name);
                    }
                    file.CopyTo(targetFilePath);
                }

                if (recursive)
                {
                    foreach (var subDir in dirs)
                    {
                        if (cancelSource.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }

                        var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                        await CopyDirectoryAsync(subDir.FullName, newDestinationDir, true, cancelSource, progressCallback);
                    }
                }
            });
        }

        /// <summary>
        /// Basic string encryption
        /// Encodes the string intro a base64 string,
        /// Usefull if you dont want to display or write a string in a readeable format
        /// </summary>
        /// <param name="unencryptedString"></param>
        /// <returns>base64 string or same string as input if fails</returns>
        public static string StringEncryption(string unencryptedString)
        {
            try
            {
                var stringBytes = Encoding.UTF8.GetBytes(unencryptedString);
                return Convert.ToBase64String(stringBytes);
            }catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "KnUtils.DIYStringEncryption()", ex);
                return unencryptedString;
            }
        }

        /// <summary>
        /// Basic string Decryption
        /// Decodes a base64 string intro a normal, readable string
        /// </summary>
        /// <param name="encryptedString"></param>
        /// <returns>decoded string or or same as input if fails</returns>
        public static string StringDecryption(string encryptedString)
        {
            try
            {
                var base64Bytes = Convert.FromBase64String(encryptedString);
                return Encoding.UTF8.GetString(base64Bytes);
            }catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "KnUtils.DIYStringDecryption()", ex);
                return encryptedString;
            }
        }

        /// <summary>
        /// Basic check to see that the input string is a valid email format
        /// Does not checks if the emails actually works
        /// </summary>
        /// <param name="email"></param>
        /// <returns>true if valid, false otherwise</returns>
        public static bool IsValidEmail(string email)
        {
            var trimmedEmail = email.Trim();

            if (trimmedEmail.EndsWith("."))
            {
                return false;
            }
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == trimmedEmail;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a complete file hash string in a format compatible with Nebula
        /// (sha256, lowercase)
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="method"></param>
        /// <returns>hash string or null if failed</returns>
        public static async Task<string?> GetFileHash(string fullPath)
        {
            try
            {
                using (FileStream? file = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                {
                    using (SHA256 checksum = SHA256.Create())
                    {
                        return BitConverter.ToString(await checksum.ComputeHashAsync(file)).Replace("-", String.Empty).ToLower();
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "KnUtils.GetFileHash()", ex);
            }
            return null;
        }

        /// <summary>
        /// Extension method to add ForEach to ObservableCollection
        /// Performs the action on each element
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="action"></param>
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var cur in enumerable)
            {
                action(cur);
            }
        }

        /// <summary>
        /// Adds arguments to a cmdline string, only if they arent already present
        /// </summary>
        /// <param name="cmdline"></param>
        /// <param name="args"></param>
        /// <returns>cmdline string</returns>
        public static string CmdLineBuilder(string cmdline, string[]? args)
        {
            try
            {
                if (args != null && args.Any())
                {
                    var addedArgs = new List<string>();
                    foreach (var arg in cmdline.ToLower().Split('-'))
                    {
                        addedArgs.Add(arg.Split(' ')[0].Trim());
                    }
                    foreach (var arg in args)
                    {
                        var argName = arg.Trim().Split(' ')[0];
                        if (!addedArgs.Contains(argName.ToLower()))
                        {
                            if (arg.Trim().Length > 0)
                            {
                                cmdline += " -" + arg.Trim();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "KnUtils.CmdLineBuilder()", ex);
            }
            return cmdline;
        }
    }
}
