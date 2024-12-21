using Avalonia.Platform;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Knossos.NET
{
    /// <summary>
    /// 7z console wrapper for Knet
    /// Supports Windows, Linux and Mac
    /// Uses 64 bit executable Windows x86(so it is not really supported), x64 and Arm64
    /// Uses 64 bit executable on Mac x64 and arm64
    /// Uses 64 bit executable on Linux x64
    /// Uses arm64 executable on Linux arm64
    /// </summary>
    public class SevenZipConsoleWrapper : IDisposable
    {
        private bool disposed = false;
        public string versionString = string.Empty;
        private static string? pathToConsoleExecutable = null;
        private Action<int>? progressCallback;
        private Process? process;
        private bool completedSuccessfully = false;
        private CancellationTokenSource? cancelSource;

        public SevenZipConsoleWrapper(Action<int>? progressCallback = null, CancellationTokenSource? cancelSource = null) 
        {
            if(pathToConsoleExecutable == null)
            {
                pathToConsoleExecutable = UnpackExec();
                if (File.Exists(pathToConsoleExecutable))
                {
                    _ = Run();
                }
                else
                {
                    pathToConsoleExecutable = null;
                    Log.Add(Log.LogSeverity.Error, "SevenZipConsoleWrapper.Constructor", "File does not exist: " + pathToConsoleExecutable);
                }
            }
            this.progressCallback = progressCallback;
            this.cancelSource = cancelSource;
        }

        /// <summary>
        /// Decompress a .zip, .7z or .tar.gz file to a folder using the 7zip cmdline tool
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="destFolder"></param>
        /// <param name="extractFullPath"></param>
        /// <returns>true if the descompression was successfull, false otherwise</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public async Task<bool> DecompressFile(string sourceFile, string destFolder, bool extractFullPath = true)
        {
            if (disposed)
                throw new ObjectDisposedException("This object was already disposed.");

            bool isTarGz = false;

            if (sourceFile.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
                isTarGz = true;

            string cmdline = "\"" + sourceFile + "\" -aoa -bsp1 -y";

            if (extractFullPath)
                cmdline = "x " + cmdline;
            else
                cmdline = "e " + cmdline;

            var result = await Run(cmdline, destFolder);

            if (isTarGz && result)
            {
                sourceFile = Path.Combine(destFolder, Path.GetFileName(sourceFile).Replace(".tar.gz", ".tar"));
                if (extractFullPath)
                    cmdline = "x ";
                else
                    cmdline = "e ";
                cmdline += "\"" + sourceFile + "\" -aoa -bsp1 -y";
                result = await Run(cmdline, destFolder);
                try
                {
                    File.Delete(sourceFile);
                }
                catch { }
            }

            return result;
        }

        /// <summary>
        /// Compress a folder into a .7z file with max LZMA2 compression
        /// destFile must be pass with the ".7z" extension.
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <param name="destFile"></param>
        /// <returns>true if successfull, false otherwise</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public async Task<bool> CompressFolder(string sourceFolder, string destFile, bool doNotStoreTimestamp = false)
        {
            if (disposed)
                throw new ObjectDisposedException("This object was already disposed.");
            string cmdline = "a -t7z -m0=lzma2 -md=64M -mx=9 -ms=on -bsp1 -y ";
            if (doNotStoreTimestamp)
                cmdline += "-mtm- ";
            return await Run(cmdline + "\"" + destFile + "\"", sourceFolder);
        }

        /// <summary>
        /// Compresses a folder into a .tar.gz file, copies over symblinks as links
        /// Important: Do not pass any extension in the destFile, the extension ".tar.gz" is added here
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <param name="destFile"></param>
        /// <returns>true if successfull, false otherwise</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public async Task<bool> CompressFolderTarGz(string sourceFolder, string destFile, bool doNotStoreTimestamp = false)
        {
            if (disposed)
                throw new ObjectDisposedException("This object was already disposed.");
            string cmdline = "a -snh -snl -y ";
            if (doNotStoreTimestamp)
                cmdline += "-mtm- ";
            var r = await Run(cmdline + destFile + ".tar", sourceFolder);
            if (r)
            {
                cmdline = "a -mx=8 -bsp1 -y ";
                if (doNotStoreTimestamp)
                    cmdline += "-mtm- ";
                return await Run(cmdline + "\"" + destFile + ".tar.gz\"" + " " + destFile + ".tar", sourceFolder);
            }
            return r;
        }

        /// <summary>
        /// Compress a file into a .7z file with max LZMA2 compression
        /// destFile must be pass with the ".7z" extension.
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <param name="destFile"></param>
        /// <returns>true if successfull, false otherwise</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public async Task<bool> CompressFile(string filepath, string workingFolder, string destFile, bool doNotStoreTimestamp = false)
        {
            if (disposed)
                throw new ObjectDisposedException("This object was already disposed.");
            string cmdline = "a -t7z -m0=lzma2 -md=64M -mx=9 -ms=on -bsp1 -y ";
            if (doNotStoreTimestamp)
                cmdline += "-mtm- ";
            return await Run(cmdline + destFile + " " + filepath, workingFolder);
        }

        /// <summary>
        /// Test a compressed file integrity
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <param name="destFile"></param>
        /// <returns>true if successfull, false otherwise</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public async Task<bool> VerifyFile(string file)
        {
            if (disposed)
                throw new ObjectDisposedException("This object was already disposed.");
            string cmdline = "t ";
            return await Run(cmdline + "\"" + file + "\"");
        }

        public void KillProcess()
        {
            try
            {
                if (process != null && process.ExitCode != 0)
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "SevenZipConsoleWrapper.KillProcess", ex);
            }
        }

        private string UnpackExec()
        {
            string execPath = KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar;
            try
            {
                if (KnUtils.IsWindows)
                {
                    execPath += "7za.exe";
                    if (!File.Exists(execPath) || new FileInfo(execPath).Length == 0)
                    {
                        using (var fileStream = File.Create(execPath))
                        {
                            AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/utils/win/7za.exe")).CopyTo(fileStream);
                            fileStream.Close();
                        }
                        using (var fileStream = File.Create(KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "7z.License.txt"))
                        {
                            AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/utils/win/7z.License.txt")).CopyTo(fileStream);
                            fileStream.Close();
                        }
                    }
                }
                else
                {
                    if (KnUtils.IsLinux)
                    {
                        if (KnUtils.CpuArch == "X64")
                        {
                            execPath += "7zzs";
                            if (!File.Exists(execPath) || new FileInfo(execPath).Length == 0)
                            {
                                using (var fileStream = File.Create(execPath))
                                {
                                    AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/utils/linux-x64/7zzs")).CopyTo(fileStream);
                                    fileStream.Close();
                                    KnUtils.Chmod(execPath, "+x");
                                }
                                using (var fileStream = File.Create(KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "7z.License.txt"))
                                {
                                    AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/utils/linux-x64/7z.License.txt")).CopyTo(fileStream);
                                    fileStream.Close();
                                }
                            }
                        }
                        if (KnUtils.CpuArch == "Arm64")
                        {
                            execPath += "7zzs";
                            if (!File.Exists(execPath) || new FileInfo(execPath).Length == 0)
                            {
                                using (var fileStream = File.Create(execPath))
                                {
                                    AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/utils/linux-arm64/7zzs")).CopyTo(fileStream);
                                    fileStream.Close();
                                    KnUtils.Chmod(execPath, "+x");
                                }
                                using (var fileStream = File.Create(KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "7z.License.txt"))
                                {
                                    AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/utils/linux-arm64/7z.License.txt")).CopyTo(fileStream);
                                    fileStream.Close();
                                }
                            }
                        }
                        if (KnUtils.CpuArch == "RiscV64")
                        {
                            execPath += "7zzs";
                            if (!File.Exists(execPath) || new FileInfo(execPath).Length == 0)
                            {
                                using (var fileStream = File.Create(execPath))
                                {
                                    AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/utils/linux-riscv64/7zzs")).CopyTo(fileStream);
                                    fileStream.Close();
                                    KnUtils.Chmod(execPath, "+x");
                                }
                                using (var fileStream = File.Create(KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "7z.License.txt"))
                                {
                                    AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/utils/linux-riscv64/7z.License.txt")).CopyTo(fileStream);
                                    fileStream.Close();
                                }
                            }
                        }
                    }
                    else
                    {
                        if (KnUtils.IsMacOS)
                        {
                            execPath += "7zz";
                            if (!File.Exists(execPath) || new FileInfo(execPath).Length == 0)
                            {
                                using (var fileStream = File.Create(execPath))
                                {
                                    AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/utils/osx/7zz")).CopyTo(fileStream);
                                    fileStream.Close();
                                    KnUtils.Chmod(execPath, "+x");
                                }
                                using (var fileStream = File.Create(KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "7z.License.txt"))
                                {
                                    AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/utils/osx/7z.License.txt")).CopyTo(fileStream);
                                    fileStream.Close();
                                }
                            }
                        }
                    }
                }
            }catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "SevenZipConsoleWrapper.UnpackExec", ex);
            }
            return execPath;
        }

        private async Task<bool> Run(string cmdline = "", string workingFolder="")
        {
            try
            {
                completedSuccessfully = false;

                if (pathToConsoleExecutable == null)
                    throw new Exception("Path con 7z console executable was null, the extraction probably failed.");
                
                using (process = new Process())
                {
                    process.StartInfo.FileName = pathToConsoleExecutable;
                    process.StartInfo.Arguments = cmdline;
                    process.StartInfo.WorkingDirectory = workingFolder;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.ErrorDataReceived += CmdError;
                    process.OutputDataReceived += CmdOutput;
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    await process.WaitForExitAsync();
                }
                
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "SevenZipConsoleWrapper.Run()", ex);
            }
            return completedSuccessfully;
        }

        private void CmdOutput(object sender, DataReceivedEventArgs e)
        {
            if (cancelSource != null && cancelSource.IsCancellationRequested)
                KillProcess();

            if (e.Data != null)
            {
                if (versionString == string.Empty && e.Data.Contains("7-Zip"))
                {
                    versionString = e.Data;
                    Log.Add(Log.LogSeverity.Information, "SevenZipConsoleWrapper.CmdOutput", versionString);
                    return;
                }
                try
                {
                    if(e.Data.Contains("%"))
                    {
                        var percentage = string.Empty;
                        foreach (Match match in Regex.Matches(e.Data, @"(\d+)%"))
                        {
                            percentage = match.Groups[1].Value;
                        }
                        progressCallback?.Invoke(int.Parse(percentage));
                    }  
                }catch { }
                if(e.Data.Contains("Everything is Ok"))
                {
                    progressCallback?.Invoke(100);
                    completedSuccessfully = true;
                }
            }
        }

        private void CmdError(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Log.Add(Log.LogSeverity.Error, "SevenZipConsoleWrapper.CmdOutput", e.Data);
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing && process != null)
            {
                try
                {
                    if (process.ExitCode != 0)
                    {
                        process.Kill();
                    }
                }
                catch { }
                process.Dispose();
            }

            disposed = true;
        }
    }
}
