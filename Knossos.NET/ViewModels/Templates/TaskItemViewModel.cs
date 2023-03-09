using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using SharpCompress.Archives;
using SharpCompress.Readers;
using SharpCompress.Common;
using Avalonia.Controls;

namespace Knossos.NET.ViewModels
{
    public partial class TaskItemViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool taskIsSet = false;
        [ObservableProperty]
        private bool cancelButtonVisible = false;
        [ObservableProperty]
        private bool cancelTask = false;
        [ObservableProperty]
        private bool tooltipVisible = false;
        [ObservableProperty]
        private string? tooltip = null;
        [ObservableProperty]
        private string info = string.Empty;
        [ObservableProperty]
        private float progressBarMin = 0;
        [ObservableProperty]
        private float progressBarMax = 100;
        [ObservableProperty]
        private float progressCurrent = 0;
        [ObservableProperty]
        private string name = string.Empty;
        [ObservableProperty]
        private bool isCompleted = false;
        [ObservableProperty]
        private bool isFileDownloadTask = false;
        [ObservableProperty]
        private bool isMsgTask = false;
        [ObservableProperty]
        private bool isBuildInstallTask = false;
        [ObservableProperty]
        private bool showProgressText = true;
        [ObservableProperty]
        private bool isDecompressionTask = false;

        [ObservableProperty]
        public ObservableCollection<TaskItemViewModel> taskList = new ObservableCollection<TaskItemViewModel>();
        [ObservableProperty]
        public ObservableCollection<TaskItemViewModel> taskRoot = new ObservableCollection<TaskItemViewModel>();

        public TaskItemViewModel() 
        { 
        }

        public void ShowMsg(string msg, string? tooltip)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    IsMsgTask = true;
                    Name = msg;
                    if (tooltip != null)
                    {
                        Tooltip = tooltip.Trim();
                        TooltipVisible = true;
                    }
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.DownloadFile()", ex);
            }
        }

        public async Task<bool> DecompressNebulaFile(string filepath, string? filename, string dest)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    IsDecompressionTask = true;
                    CancelButtonVisible = false;
                    Name = "Decompressing " + filename;
                    ShowProgressText = false;
                    ProgressBarMin = 0;
                    ProgressCurrent = 1;

                    await Task.Run(() => {
                        using (var archive = ArchiveFactory.Open(filepath))
                        {
                            var reader = archive.ExtractAllEntries();
                            while (reader.MoveToNextEntry())
                            {
                                ProgressBarMax = archive.Entries.Count();
                                Info = "Files: " + ProgressCurrent + "/" + archive.Entries.Count();
                                ProgressCurrent++;
                                if (!reader.Entry.IsDirectory)
                                {
                                    reader.WriteEntryToDirectory(dest, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                                }
                                if (CancelTask)
                                {
                                    continue;
                                }
                            }
                        }
                    });
                    if (!CancelTask)
                    {
                        IsCompleted = true;
                        return true;
                    }
                    else
                    {
                        CancelTaskCommand();
                        return false;
                    }
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }catch(Exception ex)
            {
                CancelTaskCommand();
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.DecompressTask()", ex);
                return false;
            }
        }

        public async Task<FsoBuild?> InstallBuild(FsoBuild build, FsoBuildItemViewModel sender)
        {
            string? modPath = null;
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    IsBuildInstallTask = true;
                    CancelButtonVisible = true;
                    Name = "Nebula: Downloading "+build.ToString();
                    ShowProgressText = false;
                    TaskRoot.Add(this);

                    var modJson = await Nebula.GetModData(build.id,build.version);
                    if (modJson != null)
                    {
                        /*
                            -Parse all files we need to download
                            -Delete all incompatible packages with system/cpu
                            -Generate the executable properties for valid packages
                            -Add all "ModFile" to a new list for easy access
                            -Create all folders
                            -Create the download token on the root of the mod.
                            -Set all the data needed here, number of tasks, etc for the progress bar and info
                            -Main progress max value is calculated as follows: ( Number of files to download * 2 ) + 2
                             (Download, Decompression, Download banner/tile images)
                        */
                        List<ModFile> files = new List<ModFile>();
                        string modFolder = modJson.id + "-" + modJson.version;
                        modPath = Knossos.GetKnossosLibraryPath() + @"\bin\" + modFolder;
                        for (int i = modJson.packages.Count - 1; i >= 0; i--)
                        {
                            if (IsEnviromentStringValid(modJson.packages[i].environment))
                            {
                                files.AddRange(modJson.packages[i].files!);
                                foreach(ModExecutable exec in modJson.packages[i].executables!)
                                {
                                    exec.properties = FsoBuild.FillProperties(modJson.packages[i].environment!);
                                }
                            }
                            else
                            {
                                modJson.packages.RemoveAt(i);
                            }
                        }

                        Directory.CreateDirectory(modPath);

                        foreach(var file in files)
                        {
                            if(file.dest != null && file.dest.Trim() != string.Empty)
                            {
                                var path = file.dest.Replace('/', '\\');
                                Directory.CreateDirectory(modPath+"\\"+path);
                            }
                        }

                        ProgressBarMin = 0;
                        sender.ProgressBarCurrent = ProgressCurrent = 0;
                        sender.ProgressBarMax = ProgressBarMax = (files.Count * 2) + 2;
                        Info = "Tasks: 0/" + ProgressBarMax;

                        try
                        {
                            File.Create(modPath + @"\knossos_net_download.token").Close();
                        }
                        catch { }

                        /*
                            -Use parallel to process this new list, the max parallelism is the max number of concurrent downloads
                            -Always check canceltask before executing something
                            -Download File -> Verify Checksum -> Extract file
                            -Increase main progress when: 
                             File starts to download, File finishes downloading, Decompression starts, Decompression ends, Image download completed
                        */
                        await Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = Nebula.GetMaxConcurrentDownloads() }, async (file, token) =>
                        {
                            if (!CancelTask)
                            {
                                //Download
                                var fileTask = new TaskItemViewModel();
                                TaskList.Insert(0,fileTask);
                                if (file.dest == null)
                                {
                                    file.dest = string.Empty;
                                }
                                Info = "Tasks: "+ ProgressCurrent + "/" + ProgressBarMax;
                                var fileFullPath = modPath + "\\" + file.filename;
                                var result = await fileTask.DownloadFile(file.urls!, fileFullPath, "Downloading " + file.filename, false, null);
                                if (result.HasValue && result.Value)
                                {
                                    sender.ProgressBarCurrent = ++ProgressCurrent;
                                    Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;
                                }
                                else
                                {
                                    throw new Exception("Error while downloading file: "  + fileFullPath);
                                }

                                //Checksum
                                if(!CancelTask)
                                {
                                    if(file.checksum != null && file.checksum.Count()> 0)
                                    {
                                        if (file.checksum[0].ToLower()=="sha256")
                                        {
                                            using (FileStream? filehash = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read))
                                            {
                                                using (SHA256 checksum = SHA256.Create())
                                                {
                                                    filehash.Position = 0;
                                                    var hashValue = BitConverter.ToString(await checksum.ComputeHashAsync(filehash)).Replace("-", String.Empty);
                                                    filehash.Close();
                                                    if (hashValue.ToLower() != file.checksum[1].ToLower())
                                                    {
                                                        throw new Exception("The downloaded file hash was incorrect, expected: "+ file.checksum[1] + " Calculated Hash: "+ hashValue);
                                                    }
                                                }
                                                fileTask.Info += " Checksum OK!";
                                            }
                                        }
                                        else
                                        {
                                            Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.InstallBuild()", "Unsupported checksum crypto, skipping checksum check :" + file.checksum[0]);
                                        }
                                    }
                                }

                                //Decompress
                                var decompressTask = new TaskItemViewModel();
                                TaskList.Insert(0, decompressTask);
                                var decompResult = await decompressTask.DecompressNebulaFile(fileFullPath,file.filename, modPath + "\\"+file.dest.Replace('/', '\\'));
                                if(!decompResult)
                                {
                                    Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.InstallBuild()", "Error while decompressing the file " + fileFullPath);
                                    CancelTaskCommand();
                                }
                                sender.ProgressBarCurrent = ++ProgressCurrent;
                                File.Delete(fileFullPath);
                            }
                        });
                        files.Clear();

                        /*
                            -Delete the download token.
                            -Download tile and banner images, update those file names on json, increase progress
                            -Add folder name and full path to the modJson before creating the fsobuild
                            -Set modJson installed to true before saving json
                            -Serialize json to folder
                            -Create the FsoBuild object and add it to the main list
                            -Return the same FsoObject so it can be updated on the FsoBuildView
                        */
                        if(!string.IsNullOrEmpty(modJson.tile))
                        {
                            var tileTask = new TaskItemViewModel();
                            TaskList.Insert(0, tileTask);
                            await tileTask.DownloadFile(modJson.tile,modPath+ @"\kn_tile.png", "Downloading tile image", false, null);
                            modJson.tile = "kn_tile.png";
                        }
                        sender.ProgressBarCurrent = ++ProgressCurrent;
                        Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;
                        if (!string.IsNullOrEmpty(modJson.banner))
                        {
                            var bannerTask = new TaskItemViewModel();
                            TaskList.Insert(0, bannerTask);
                            await bannerTask.DownloadFile(modJson.banner, modPath + @"\kn_banner.png", "Downloading banner image", false, null);
                            modJson.banner = "kn_banner.png";
                        }
                        sender.ProgressBarCurrent = ++ProgressCurrent;
                        Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;
                        modJson.fullPath = modPath + @"\";
                        modJson.folderName = modFolder;
                        modJson.installed = true;
                        modJson.SaveJson();
                        try
                        {
                            File.Delete(modJson.fullPath + @"\knossos_net_download.token");
                        }
                        catch(Exception ex)
                        {
                            Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.InstallBuild()", ex);
                        }
                        FsoBuild newBuild = new FsoBuild(modJson);
                        Knossos.AddBuild(newBuild);
                        IsCompleted = true;
                        CancelButtonVisible = false;
                        return newBuild;
                    }
                    else
                    {
                        CancelTaskCommand();
                        Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.InstallBuild()", "Unable to find mod in Nebula repo, requested id:" + build.id+ " version: " + build.version);
                        return null;
                    }
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (Exception ex)
            {
                IsCompleted = false;
                CancelTaskCommand();
                try
                {
                    if (modPath != null)
                    {
                        Directory.Delete(modPath, true);
                    }
                }
                catch { }
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.InstallBuild()", ex);
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    //Messagebox is not thread safe!
                    await MessageBox.Show(MainWindow.instance!, "An error was ocurred during the download of the mod: " + build.ToString() + ". Error: " + ex.Message, "Error", MessageBox.MessageBoxButtons.OK);
                });
                return null;
            }
        }

        public async Task<bool?> DownloadFile(string url, string dest, string msg, bool showStopButton, string? tooltip)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    IsFileDownloadTask = true;
                    CancelButtonVisible = showStopButton;
                    Name = msg;
                    if(tooltip != null)
                    {
                        Tooltip = tooltip.Trim();
                        TooltipVisible = true;
                    }

                    var downloadProgress = (long? filesize, long bytesDownloaded, string speed, double? progressPercentage) =>
                    {
                        if (progressPercentage.HasValue)
                        {
                            ProgressCurrent = (float)progressPercentage.Value;
                            Info = string.Format("{1}MB / {2}MB {0}", speed,bytesDownloaded/1024/1024, filesize/1024/1024);
                        }

                        return CancelTask;
                    };
                    int maxRetries = 10;
                    int count = 0;
                    bool result = false;
                    do
                    {
                        count++;
                        if (count > 1)
                        {
                            Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.DownloadFile()", "Retrying download of file: " + url);
                        }
                        result = await Download(url, dest, downloadProgress);
                    } while (result != true && !CancelTask && count < maxRetries);

                    CancelButtonVisible = false;
                    if(result)
                    {
                        IsCompleted = true;
                        return true;
                    }
                    else
                    {
                        CancelTask = true;
                        return false;
                    }
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.DownloadFile()", ex);
                return false;
            }
        }

        public async Task<bool?> DownloadFile(List<string> mirrors, string dest, string msg, bool showStopButton, string? tooltip)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    IsFileDownloadTask = true;
                    CancelButtonVisible = showStopButton;
                    Name = msg;
                    if (tooltip != null)
                    {
                        Tooltip = tooltip.Trim();
                        TooltipVisible = true;
                    }

                    var downloadProgress = (long? filesize, long bytesDownloaded, string speed, double? progressPercentage) =>
                    {
                        if (progressPercentage.HasValue)
                        {
                            ProgressCurrent = (float)progressPercentage.Value;
                            Info = string.Format("{1}MB / {2}MB {0}", speed, bytesDownloaded / 1024 / 1024, filesize / 1024 / 1024);
                        }

                        return CancelTask;
                    };

                    bool result = false;

                    result = await Download(mirrors, dest, downloadProgress);

                    CancelButtonVisible = false;
                    if (result)
                    {
                        IsCompleted = true;
                        return true;
                    }
                    else
                    {
                        CancelTask = true;
                        return false;
                    }
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.DownloadFile()", ex);
                return false;
            }
        }

        public void CancelTaskCommand()
        {
            if (!IsCompleted)
            {
                CancelTask = true;
                CancelButtonVisible = false;
                foreach(var t in TaskList)
                {
                    t.CancelTaskCommand();
                }
            }
        }


        private bool IsEnviromentStringValid(string? enviroment)
        {
            if(enviroment == null || enviroment.Trim() == string.Empty)
            { 
                return false; 
            }

            if(enviroment.ToLower().Contains("windows") && !SysInfo.IsWindows || enviroment.ToLower().Contains("linux") && !SysInfo.IsLinux || enviroment.ToLower().Contains("macosx") && !SysInfo.IsMacOS)
            {
                return false;
            }

            if (enviroment.ToLower().Contains("avx2") && !SysInfo.CpuAVX2 || enviroment.ToLower().Contains("avx") && !SysInfo.CpuAVX)
            {
                return false;
            }

            if (enviroment.ToLower().Contains("x86_64") && SysInfo.CpuArch == "X64")
            {
                return true;
            }
            if (enviroment.ToLower().Contains("arm64") && SysInfo.CpuArch == "Arm64")
            {
                return true;
            }
            if (enviroment.ToLower().Contains("arm32") && (SysInfo.CpuArch == "Armv6" || SysInfo.CpuArch == "Arm"))
            {
                return true;
            }
            if(SysInfo.CpuArch == "X86" && !enviroment.ToLower().Contains("x86_64") && !enviroment.ToLower().Contains("arm64") && !enviroment.ToLower().Contains("arm32"))
            {
                return true;
            }
            if (SysInfo.CpuArch == "X64" && !enviroment.ToLower().Contains("x86_64") && !enviroment.ToLower().Contains("arm64") && !enviroment.ToLower().Contains("arm32"))
            {
                return true;
            }

            return false;
        }

        private async Task<bool> Download(List<string> downloadMirrors, string destinationFilePath, Func<long?, long, string, double?, bool> progressChanged)
        {
            Random rnd = new Random();
            int maxRetries = 10;
            int count = 0;
            bool result = false;
            do
            {
                string url = downloadMirrors[rnd.Next(downloadMirrors.Count)];
                count++;
                if (count > 1)
                {
                    Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.Download(List<mirrors>)", "Retrying download of file: " + url);
                }
                result = await Download(url, destinationFilePath, progressChanged);
            } while (result != true && !CancelTask && count < maxRetries);
            
            return result;
        }

        private async Task<bool> Download(string downloadUrl, string destinationFilePath, Func<long?, long, string, double?, bool> progressChanged)
        {
            try
            {
                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                using HttpClient httpClient = new HttpClient() { Timeout = TimeSpan.FromDays(1) };
                using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength;
                using var contentStream = await response.Content.ReadAsStreamAsync();
                var totalBytesRead = 0L;
                var totalBytesPerSecond = 0L;
                var readCount = 0L;
                var buffer = new byte[262144];
                var isMoreToRead = true;
                var speed = string.Empty;

                static double? calculatePercentage(long? totalDownloadSize, long totalBytesRead) => totalDownloadSize.HasValue ? Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2) : null;

                using var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 262144, true);
                stopwatch.Start();
                
                do
                {
                    var bytesRead = await contentStream.ReadAsync(buffer);
                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;

                        if (progressChanged(totalBytes, totalBytesRead, string.Empty, calculatePercentage(totalBytes, totalBytesRead)))
                        {
                            stopwatch.Reset();
                            throw new OperationCanceledException();
                        }

                        continue;
                    }

                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));

                    totalBytesRead += bytesRead;
                    totalBytesPerSecond += bytesRead;
                    readCount++;

                    if (stopwatch.Elapsed.TotalSeconds >= 1)
                    {
                        speed = string.Format("@ {0} MB/s", (totalBytesPerSecond / 1024.0 / 1024.0 / stopwatch.Elapsed.TotalSeconds).ToString("0.00"));
                        totalBytesPerSecond = 0L;
                        stopwatch.Restart();
                    }


                    if (readCount % 100 == 0)
                    {
                        if (progressChanged(totalBytes, totalBytesRead, speed, calculatePercentage(totalBytes, totalBytesRead)))
                        {
                            stopwatch.Reset();
                            throw new OperationCanceledException();
                        }
                    }
                }
                while (isMoreToRead);
                stopwatch.Reset();
                return true;
            }
            catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.Download", ex);
                return false;
            }
        }
    }
}
