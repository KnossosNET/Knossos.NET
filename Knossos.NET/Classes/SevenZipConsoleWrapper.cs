using Avalonia.Platform;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Knossos.NET
{
    public class SevenZipConsoleWrapper
    {
        public string versionString = string.Empty;
        private string pathToConsoleExecutable;
        private Action<string>? progressCallback;
        private Process? process;
        private bool completedSuccessfully = false;

        public SevenZipConsoleWrapper(Action<string>? progressCallback = null) 
        {
            this.pathToConsoleExecutable = UnpackExec();
            this.progressCallback = progressCallback;

            if(File.Exists(pathToConsoleExecutable))
            {
                _ = Run();
            }
            else
            {
                Log.Add(Log.LogSeverity.Error, "SevenZipConsoleWrapper.Constructor", "File does not exist: " + pathToConsoleExecutable);
            }
        }

        public async Task<bool> CompressFolder(string sourceFolder, string destFile)
        {
            string cmdline = "a -t7z -m0=lzma2 -md=64M -mx=9 -ms=on -bsp1 -y ";
            return await Run(cmdline + destFile, sourceFolder);
        }

        public async Task<bool> CompressFile(string filepath, string workingFolder, string destFile)
        {
            string cmdline = "a -t7z -m0=lzma2 -md=64M -mx=9 -ms=on -bsp1 -y ";
            return await Run(cmdline + destFile + " " + filepath, workingFolder);
        }

        public async Task<bool> VerifyFile(string file)
        {
            string cmdline = "t ";
            return await Run(cmdline + file);
        }

        public void KillProcess()
        {
            try
            {
                process?.Kill();
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "SevenZipConsoleWrapper.KillProcess", ex);
            }
        }

        private string UnpackExec()
        {
            string execPath = SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar;
            try
            {
                if (SysInfo.IsWindows)
                {
                    execPath += "7za.exe";
                    if (!File.Exists(execPath))
                    {
                        using (var fileStream = File.Create(execPath))
                        {
                            AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/utils/win/7za.exe")).CopyTo(fileStream);
                            fileStream.Close();
                        }
                        using (var fileStream = File.Create(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "7z.License.txt"))
                        {
                            AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/utils/win/7z.License.txt")).CopyTo(fileStream);
                            fileStream.Close();
                        }
                    }
                }
                else
                {
                    if (SysInfo.IsLinux)
                    {
                        if (SysInfo.CpuArch == "X64")
                        {
                            execPath += "7zzs";
                            if (!File.Exists(execPath))
                            {
                                using (var fileStream = File.Create(execPath))
                                {
                                    AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/utils/linux-x64/7zzs")).CopyTo(fileStream);
                                    fileStream.Close();
                                    SysInfo.Chmod(execPath, "+x");
                                }
                                using (var fileStream = File.Create(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "7z.License.txt"))
                                {
                                    AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/utils/linux-x64/7z.License.txt")).CopyTo(fileStream);
                                    fileStream.Close();
                                }
                            }
                        }
                        if (SysInfo.CpuArch == "Arm64")
                        {
                            execPath += "7zzs";
                            if (!File.Exists(execPath))
                            {
                                using (var fileStream = File.Create(execPath))
                                {
                                    AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/utils/linux-arm64/7zzs")).CopyTo(fileStream);
                                    fileStream.Close();
                                    SysInfo.Chmod(execPath, "+x");
                                }
                                using (var fileStream = File.Create(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "7z.License.txt"))
                                {
                                    AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/utils/linux-arm64/7z.License.txt")).CopyTo(fileStream);
                                    fileStream.Close();
                                }
                            }
                        }
                    }
                    else
                    {
                        if (SysInfo.IsMacOS)
                        {
                            execPath += "7zz";
                            if (!File.Exists(execPath))
                            {
                                using (var fileStream = File.Create(execPath))
                                {
                                    AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/utils/osx/7zz")).CopyTo(fileStream);
                                    fileStream.Close();
                                    SysInfo.Chmod(execPath, "+x");
                                }
                                using (var fileStream = File.Create(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "7z.License.txt"))
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
                using (process = new Process())
                {
                    process = new Process();
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
                Console.WriteLine(ex.ToString());
            }
            return completedSuccessfully;
        }

        private void CmdOutput(object sender, DataReceivedEventArgs e)
        {
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
                        progressCallback?.Invoke(percentage);
                    }  
                }catch { }
                if(e.Data.Contains("Everything is Ok"))
                {
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
    }
}
