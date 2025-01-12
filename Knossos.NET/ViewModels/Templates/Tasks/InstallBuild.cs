using Avalonia.Threading;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading;
using Knossos.NET.Classes;

namespace Knossos.NET.ViewModels
{
    public partial class TaskItemViewModel : ViewModelBase
    {
        public async Task<FsoBuild?> InstallBuild(FsoBuild build, FsoBuildItemViewModel sender, CancellationTokenSource? cancelSource = null, Mod? modJson = null, List<ModPackage>? modifyPkgs = null, bool cleanupOldVersions = false)
        {
            string? modPath = null;
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    if (cancelSource != null)
                    {
                        cancellationTokenSource = cancelSource;
                    }
                    else
                    {
                        cancellationTokenSource = new CancellationTokenSource();
                    }
                    CancelButtonVisible = true;

                    Name = "Downloading " + build.ToString();
                    if (modifyPkgs != null)
                        Name = "Modifying " + build.ToString();

                    ShowProgressText = false;
                    await Dispatcher.UIThread.InvokeAsync(() => TaskRoot.Add(this));
                    Info = "In Queue";

                    //Wait in Queue
                    while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                    {
                        await Task.Delay(1000);
                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }
                    }

                    Info = "Starting";

                    //parse repo to get the data we need
                    if (modJson == null)
                    {
                        modJson = await Nebula.GetModData(build.id, build.version);
                    }

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

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
                            -Main progress max value is calculated as follows: ( Number of files to download * 2 ) + 1
                             (Download, Decompression, Download banner/tile images)
                        */
                        List<ModFile> files = new List<ModFile>();
                        string modFolder = modJson.id + "-" + modJson.version;
                        modPath = Knossos.GetKnossosLibraryPath() + Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar + modFolder;
                        if (modifyPkgs != null)
                        {
                            //Modify Build
                            foreach (var pkg in modifyPkgs)
                            {
                                var installedPkg = modJson.packages.FirstOrDefault(p => p.name == pkg.name && p.folder == pkg.folder);

                                if (installedPkg != null && !pkg.isSelected)
                                {
                                    //If it is installed but not selected, delete it
                                    try
                                    {
                                        var deleteTask = new TaskItemViewModel();
                                        deleteTask.ShowMsg("Deleting pkg: " + pkg.name, null);
                                        await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, deleteTask));
                                        if (pkg.filelist != null)
                                        {
                                            foreach (var f in pkg.filelist)
                                            {
                                                if (File.Exists(modPath + Path.DirectorySeparatorChar + f.filename))
                                                {
                                                    File.Delete(modPath + Path.DirectorySeparatorChar + f.filename);
                                                }
                                            }
                                            deleteTask.IsCompleted = true;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.InstallBuild()", ex);
                                    }
                                    modJson.packages.Remove(installedPkg);
                                    continue;
                                }

                                if (installedPkg == null && pkg.isSelected)
                                {
                                    //if it is not installed and selected, install it
                                    files.AddRange(pkg.files!);
                                    foreach (ModExecutable exec in pkg.executables!)
                                    {
                                        exec.properties = FsoBuild.FillProperties(pkg.environment!);
                                    }
                                    modJson.packages.Add(pkg);
                                }
                            }
                        }
                        else
                        {
                            //New Install
                            for (int i = modJson.packages.Count - 1; i >= 0; i--)
                            {
                                if (FsoBuild.IsEnviromentStringValidInstall(modJson.packages[i].environment))
                                {
                                    files.AddRange(modJson.packages[i].files!);
                                    foreach (ModExecutable exec in modJson.packages[i].executables!)
                                    {
                                        exec.properties = FsoBuild.FillProperties(modJson.packages[i].environment!);
                                        var arch = FsoBuild.GetExecArch(exec.properties);
                                        exec.score = FsoBuild.DetermineScoreFromArch(arch, KnUtils.CpuArch == "X86" || KnUtils.CpuArch == "X64" ? true : false);
                                    }
                                    var bestExec = modJson.packages[i].executables!.MaxBy(exec => exec.score);
                                    if (bestExec != null)
                                    {
                                        modJson.packages[i].buildScore = bestExec.score;
                                    }
                                }
                                else
                                {
                                    modJson.packages.RemoveAt(i);
                                }
                            }

                            var bestpkg = modJson.packages!.MaxBy(pkg => pkg.buildScore);
                            if (bestpkg != null && bestpkg.files != null)
                            {
                                files.Clear();
                                modJson.packages.Clear();
                                files.AddRange(bestpkg.files);
                                modJson.packages.Add(bestpkg);
                            }
                        }

                        Directory.CreateDirectory(modPath);

                        foreach (var file in files)
                        {
                            if (file.dest != null && file.dest.Trim() != string.Empty)
                            {
                                var path = file.dest;
                                Directory.CreateDirectory(modPath + Path.DirectorySeparatorChar + path);
                            }
                        }

                        ProgressBarMin = 0;
                        //sender.ProgressBarCurrent = ProgressCurrent = 0;
                        ProgressCurrent = 0;
                        //sender.ProgressBarMax = ProgressBarMax = (files.Count * 2) + 1;
                        ProgressBarMax = (files.Count * 2) + 1;
                        Info = "Tasks: 0/" + ProgressBarMax;

                        try
                        {
                            File.Create(modPath + Path.DirectorySeparatorChar + "knossos_net_download.token").Close();
                        }
                        catch { }

                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }

                        /*
                            -Use parallel to process this new list, the max parallelism is the max number of concurrent downloads
                            -Always check canceltask before executing something
                            -Download File -> Verify Checksum -> Extract file
                            -Increase main progress when: 
                             File starts to download, File finishes downloading, Decompression starts, Decompression ends, Image download completed
                        */
                        await Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = Knossos.globalSettings.maxConcurrentSubtasks }, async (file, token) =>
                        {
                            if (cancellationTokenSource.IsCancellationRequested)
                            {
                                throw new TaskCanceledException();
                            }

                            //Download
                            var fileTask = new TaskItemViewModel();
                            await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, fileTask));
                            if (file.dest == null)
                            {
                                file.dest = string.Empty;
                            }

                            Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;
                            var fileFullPath = modPath + Path.DirectorySeparatorChar + file.filename;
                            var result = await fileTask.DownloadFile(file.urls!, fileFullPath, "Downloading " + file.filename, false, null, cancellationTokenSource);

                            if (cancellationTokenSource.IsCancellationRequested)
                            {
                                throw new TaskCanceledException();
                            }

                            if (result.HasValue && result.Value)
                            {
                                //sender.ProgressBarCurrent = ++ProgressCurrent;
                                ++ProgressCurrent;
                                Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;
                            }
                            else
                            {
                                throw new Exception("Error while downloading file: " + fileFullPath);
                            }

                            //Checksum
                            if (file.checksum != null && file.checksum.Count() > 0)
                            {
                                if (file.checksum[0].ToLower() == "sha256")
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
                                                //Nightlies Errata #1
                                                //All Nightlies older than 20230805 got their checksum changed when they got moved and data is not updated in the DB
                                                if (build.id == "FSO" && build.stability == FsoStability.Nightly && build.date != null && string.Compare("2023-08-05", build.date) >= 0)
                                                {
                                                    fileTask.Info += " Nightlies Errata #1";
                                                    Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.InstallBuild()", "The downloaded file hash was incorrect, expected: " + file.checksum[1] + " Calculated Hash: " + hashValue + " (Nightly Errata #1)");
                                                }
                                                else
                                                {
                                                    fileTask.Info += " Checksum Mismatch!";
                                                    throw new Exception("The downloaded file hash was incorrect, expected: " + file.checksum[1] + " Calculated Hash: " + hashValue);
                                                }
                                            }
                                            else
                                            {
                                                fileTask.Info += " Checksum OK!";
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.InstallBuild()", "Unsupported checksum crypto, skipping checksum check :" + file.checksum[0]);
                                }
                            }

                            if (cancellationTokenSource.IsCancellationRequested)
                            {
                                throw new TaskCanceledException();
                            }

                            //Decompress
                            var decompressTask = new TaskItemViewModel();
                            await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, decompressTask));
                            var decompResult = await decompressTask.DecompressNebulaFile(fileFullPath, file.filename, modPath + Path.DirectorySeparatorChar + file.dest, cancellationTokenSource);
                            if (!decompResult)
                            {
                                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.InstallBuild()", "Error while decompressing the file " + fileFullPath);
                                CancelTaskCommand();
                            }
                            //sender.ProgressBarCurrent = ++ProgressCurrent;
                            ++ProgressCurrent;
                            File.Delete(fileFullPath);
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

                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }

                        //Download Tile Image
                        if (!string.IsNullOrEmpty(modJson.tile) && modifyPkgs == null)
                        {
                            Directory.CreateDirectory(modPath + Path.DirectorySeparatorChar + "kn_images");
                            var uri = new Uri(modJson.tile);
                            using (var fs = await KnUtils.GetRemoteResourceStream(modJson.tile))
                            {
                                var tileTask = new TaskItemViewModel();
                                await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, tileTask));
                                tileTask.ShowMsg("Getting tile image", null);
                                if (fs != null)
                                {
                                    using (var destImg = new FileStream(modPath + Path.DirectorySeparatorChar + "kn_images" + Path.DirectorySeparatorChar + Path.GetFileName(uri.LocalPath), FileMode.Create, FileAccess.Write))
                                    {
                                        await fs.CopyToAsync(destImg);
                                        fs.Close();
                                        destImg.Close();
                                    }
                                }
                            }
                            modJson.tile = "kn_images" + Path.DirectorySeparatorChar + Path.GetFileName(uri.LocalPath);
                        }


                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }


                        //Download Banner Image
                        if (!string.IsNullOrEmpty(modJson.banner) && modifyPkgs == null)
                        {
                            Directory.CreateDirectory(modPath + Path.DirectorySeparatorChar + "kn_images");
                            var uri = new Uri(modJson.banner);
                            using (var fs = await KnUtils.GetRemoteResourceStream(modJson.banner))
                            {
                                var bannerTask = new TaskItemViewModel();
                                await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, bannerTask));
                                bannerTask.ShowMsg("Getting banner image", null);
                                if (fs != null)
                                {
                                    using (var destImg = new FileStream(modPath + Path.DirectorySeparatorChar + "kn_images" + Path.DirectorySeparatorChar + Path.GetFileName(uri.LocalPath), FileMode.Create, FileAccess.Write))
                                    {
                                        await fs.CopyToAsync(destImg);
                                        fs.Close();
                                        destImg.Close();
                                    }
                                }
                            }
                            modJson.banner = "kn_images" + Path.DirectorySeparatorChar + Path.GetFileName(uri.LocalPath);
                        }

                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }

                        //Download Screenshots
                        if (modJson.screenshots != null && modJson.screenshots.Any() && modifyPkgs == null)
                        {
                            Directory.CreateDirectory(modPath + Path.DirectorySeparatorChar + "kn_images");
                            var scList = new List<string>();
                            foreach (var sc in modJson.screenshots)
                            {
                                if (cancellationTokenSource.IsCancellationRequested)
                                {
                                    throw new TaskCanceledException();
                                }
                                var uri = new Uri(sc);
                                using (var fs = await KnUtils.GetRemoteResourceStream(sc))
                                {
                                    var scTask = new TaskItemViewModel();
                                    await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, scTask));
                                    scTask.ShowMsg("Getting screenshot #" + scList.Count() + " image", null);
                                    if (fs != null)
                                    {
                                        using (var destImg = new FileStream(modPath + Path.DirectorySeparatorChar + "kn_images" + Path.DirectorySeparatorChar + Path.GetFileName(uri.LocalPath), FileMode.Create, FileAccess.Write))
                                        {
                                            await fs.CopyToAsync(destImg);
                                            fs.Close();
                                            destImg.Close();
                                        }
                                    }
                                }
                                scList.Add("kn_images" + Path.DirectorySeparatorChar + Path.GetFileName(uri.LocalPath));
                            }
                            modJson.screenshots = scList.ToArray();
                        }

                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }

                        /**/
                        //sender.ProgressBarCurrent = ++ProgressCurrent;
                        ++ProgressCurrent;
                        Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;
                        modJson.fullPath = modPath + Path.DirectorySeparatorChar;
                        modJson.folderName = modFolder;
                        modJson.installed = true;
                        modJson.inNebula = true;
                        modJson.SaveJson();
                        try
                        {
                            File.Delete(modJson.fullPath + Path.DirectorySeparatorChar + "knossos_net_download.token");
                        }
                        catch (Exception ex)
                        {
                            Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.InstallBuild()", ex);
                        }
                        FsoBuild newBuild = new FsoBuild(modJson);
                        if (modifyPkgs == null)
                        {
                            //New Build
                            Knossos.AddBuild(newBuild);
                            DeveloperModsViewModel.Instance?.UpdateListedFsoBuildVersionsInEditor();
                        }
                        if (modJson.devMode)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() => MainWindowViewModel.Instance!.AddDevMod(modJson), DispatcherPriority.Background);
                            //Update version editor if needed
                            DeveloperModsViewModel.Instance?.UpdateVersionManager(modJson.id);
                        }
                        IsCompleted = true;
                        CancelButtonVisible = false;

                        //Re-run Dependencies checks 
                        MainWindowViewModel.Instance?.RunModStatusChecks();

                        // Clean old versions
                        if (cleanupOldVersions)
                        {
                            try
                            {
                                var versions = Knossos.GetInstalledBuildsList(build.id, build.stability);
                                if (versions != null)
                                {
                                    versions.Remove(build);
                                    if (versions.Any())
                                    {
                                        foreach (var version in versions.ToList())
                                        {
                                            //Check if it is inferior to the one we just installed
                                            if (SemanticVersion.Compare(build.version, version.version) >= 1)
                                            {
                                                bool inUse = false;
                                                string inUseMods = "";
                                                foreach (var m in Knossos.GetInstalledModList(null))
                                                {
                                                    if (m != null && m.id != build.id)
                                                    {
                                                        var deps = m.GetModDependencyList();
                                                        if (deps != null)
                                                        {
                                                            foreach (var dep in deps)
                                                            {
                                                                var depMod = dep.SelectBuild();
                                                                if (depMod == version)
                                                                {
                                                                    inUse = true;
                                                                    inUseMods += m + ", ";
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                if (inUse)
                                                {
                                                    Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.InstallBuild()", "Cleanup: " + version + " is in use by these mods: " + inUseMods + ". Skipping.");
                                                }
                                                else
                                                {
                                                    //Safe to delete
                                                    FsoBuildItemViewModel? uiItem = null;
                                                    switch (version.stability)
                                                    {
                                                        case FsoStability.Stable: uiItem = FsoBuildsViewModel.Instance!.StableItems.FirstOrDefault(x => x.build != null && x.build.version == version.version); break;
                                                        case FsoStability.RC: uiItem = FsoBuildsViewModel.Instance!.RcItems.FirstOrDefault(x => x.build != null && x.build.version == version.version); break;
                                                        case FsoStability.Nightly: uiItem = FsoBuildsViewModel.Instance!.NightlyItems.FirstOrDefault(x => x.build != null && x.build.version == version.version); break;
                                                        case FsoStability.Custom: uiItem = FsoBuildsViewModel.Instance!.CustomItems.FirstOrDefault(x => x.build != null && x.build.version == version.version); break;
                                                    }

                                                    if (uiItem != null)
                                                    {
                                                        Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.InstallBuild()", "Cleanup: " + version + " is not in use, deleting...");
                                                        var msgtask = new TaskItemViewModel();
                                                        msgtask.ShowMsg("Cleanup: Deleting " + version.version, null);
                                                        await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, msgtask));
                                                        FsoBuildsViewModel.Instance!.DeleteBuild(version, uiItem, false);
                                                        //If the build is custom and the dev editor is open and loaded this build id, reset it
                                                        if (version.stability == FsoStability.Custom)
                                                            await Dispatcher.UIThread.InvokeAsync(() => DeveloperModsViewModel.Instance!.ResetModEditor(version.id));
                                                    }
                                                    else
                                                    {
                                                        Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.InstallBuild()", "Cleanup: Unable to find" + version + " in the UI items, so we can't auto delete.");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.InstallBuild()", "Cleanup: " + version + " is newer than " + build + ". Skipping.");
                                            }
                                        }
                                        MainWindowViewModel.Instance?.RunModStatusChecks();
                                    }
                                    else
                                    {
                                        //Nothing to cleanup
                                        var msgtask = new TaskItemViewModel();
                                        msgtask.ShowMsg("Cleanup: Nothing to cleanup.", null);
                                        await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, msgtask));
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                var msgtask = new TaskItemViewModel();
                                msgtask.ShowMsg("Cleanup: An error has ocurred, check logs.", null);
                                await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, msgtask));
                                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.InstallMod()", ex);
                            }
                        }

                        /*
                            Always Dequeue, always check for check size and verify that the first is this TaskItemViewModel object
                        */
                        if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                        {
                            TaskViewModel.Instance!.taskQueue.Dequeue();
                        }
                        /*
                            If flag data wasnt loaded, load it now
                        */
                        if (!Knossos.flagDataLoaded)
                        {
                            MainWindowViewModel.Instance?.GlobalSettingsLoadData();
                        }
                        return newBuild;
                    }
                    else
                    {
                        cancellationTokenSource?.Cancel(); //if some error has ocurred cancel everything
                        Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.InstallBuild()", "Unable to find mod in Nebula repo, requested id:" + build.id + " version: " + build.version);
                        return null;
                    }
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (TaskCanceledException)
            {
                /*
                    Task cancel requested by user
                */
                IsCompleted = false;
                CancelButtonVisible = false;
                IsCancelled = true;
                Info = "Cancel Requested";
                while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                {
                    await Task.Delay(500);
                }
                if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                {
                    TaskViewModel.Instance!.taskQueue.Dequeue();
                }
                await Task.Delay(2000); //give time for child tasks to cancel first
                Info = "Task Cancelled";
                try
                {
                    if (modPath != null)
                    {
                        Directory.Delete(modPath, true);
                    }
                }
                catch { }
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                return null;
            }
            catch (Exception ex)
            {
                /*
                    Task cancel forced due to a error
                */
                IsCompleted = false;
                IsCancelled = true;
                CancelButtonVisible = false;
                cancellationTokenSource?.Cancel();
                Info = "Cancel Requested";
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    //Messagebox is not thread safe!
                    await MessageBox.Show(MainWindow.instance!, "An error was ocurred during the download of the mod: " + build.ToString() + ". Error: " + ex.Message, "Error", MessageBox.MessageBoxButtons.OK);
                });
                while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                {
                    await Task.Delay(500);
                }
                if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                {
                    TaskViewModel.Instance!.taskQueue.Dequeue();
                }

                await Task.Delay(2000); //give time for child tasks to cancel first
                Info = "Task Failed";
                try
                {
                    if (modPath != null)
                    {
                        Directory.Delete(modPath, true);
                    }
                }
                catch { }
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.InstallBuild()", ex);
                return null;
            }
        }
    }
}
