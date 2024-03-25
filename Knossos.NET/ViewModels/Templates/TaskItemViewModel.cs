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
using System.Threading;
using Knossos.NET.Classes;
using VP.NET;
using System.Text;
using Avalonia.Media;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Knossos.NET.ViewModels
{
    /*
        //New Task Method Example
        public async Task<bool> NewTaskExample(CancellationTokenSource? cancelSource = null)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true; //a task can only do one thing, when a method is called this defines what this task will do, this ensures no other operation is done by mistake
                    ProgressBarMax = 1; // 0 = no progress bar, anything else to display progress bar, you can set this at any time
                    ProgressCurrent = 0; //if you need a progress bar you can set this at any time
                    ShowProgressText = false; //If the progress bar must display ProgressCurrent / ProgressBarMax on top of the bar
                    CancelButtonVisible = true; //you want the user to be able to manually cancel this task?
                    IsTextTask = false; // Set to True if you want simple "show my text" task. This will not display the "task completed" text.
                    IsFileDownloadTask = true; //This enables pause/resume buttons for file download on this task
                    Name = ""; //Display name

                    //We need a cancel token, if it is not provided one must be created
                    if (cancelSource != null)
                        cancellationTokenSource = cancelSource;
                    else
                        cancellationTokenSource = new CancellationTokenSource();

                    if (cancellationTokenSource.IsCancellationRequested) //this task was cancelled? you may want to check this multiple times
                        throw new TaskCanceledException();

                    //Wait in Queue. Important!!! Only for main tasks that need to wait in queue that are created in TaskViewModel.cs
                    //And only if it needs to actually wait in the queue, some you dont need to do this for, ex: show a text msg.
                    //Do not do this for internal/subtasks as it will loop here forever
                    Info = "In Queue";
                    while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                    {
                        await Task.Delay(1000);
                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }
                    }

                    Info = ""; //This display Text on the TaskView, if the task has a progress bar this text is display to the right of it, otherwise to the right of "name".
                    TextColor =  Brushes.White; //Info text color

                    //If to need your task to create subtasks
                    //Add this task to the task root (you can do it at any point, only do it once)
                    Dispatcher.UIThread.InvokeAsync(() => TaskRoot.Add(this)); //important! do it from the ui thread

                    //Create and add all subtask to the task list
                    var newTask = new TaskItemViewModel();
                    newTask.NewTaskExample(cancellationTokenSource); //pass the cancel token
                    Dispatcher.UIThread.InvokeAsync(() => TaskList.Add(newTask)); //important! do it from the ui thread

                    IsCompleted = true; //once we completed the task this must be set to true
                    CancelButtonVisible = false; //once we completed the task this must be set to true
                    ProgressCurrent = ProgressBarMax; //a recomended step at the end in case your task had a progress bar

                    //Dequeue. Important: Only if this is not a Subtask!!! If this task did not wait in a queue dont do this it will loop here forever
                    if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                    {
                        TaskViewModel.Instance!.taskQueue.Dequeue();
                    }

                    return true; //on task completion return true
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (TaskCanceledException)
            {
                //Task cancel requested by user
                IsCompleted = false;
                IsCancelled = true;
                CancelButtonVisible = false;
                Info = "Task Cancelled";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                else
                {
                    //Call cancel task on the parent object
                    cancellationTokenSource?.Cancel();
                }
                //If you need to do a task specific stuff on cancel resquest do it here
                //Dequeue. Important: Only if this is not a Subtask!!! If this task did not wait in a queue dont do this it will loop here forever
                while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                {
                    await Task.Delay(500);
                }
                if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                {
                    TaskViewModel.Instance!.taskQueue.Dequeue();
                }
                return false;
            }
            catch (Exception ex)
            {
                //An exception has happened during task run
                IsCompleted = false;
                CancelButtonVisible = false;
                IsCancelled = true;
                Info = "Task Failed";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                else
                {
                    //Call cancel task on the parent object
                    cancellationTokenSource?.Cancel();
                }
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.NewTaskExample()", ex);
                //If you need to do a task specific stuff do it here
                //Dequeue. Important: Only if this is not a Subtask!!! If this task did not wait in a queue dont do this it will loop here forever
                while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                {
                    await Task.Delay(500);
                }
                if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                {
                    TaskViewModel.Instance!.taskQueue.Dequeue();
                }
                return false;
            }
        }
    */

    public partial class TaskItemViewModel : ViewModelBase
    {
        [ObservableProperty]
        internal bool taskIsSet = false;
        [ObservableProperty]
        internal bool cancelButtonVisible = false;
        [ObservableProperty]
        internal bool tooltipVisible = false;
        [ObservableProperty]
        internal string? tooltip = null;
        [ObservableProperty]
        internal string info = string.Empty;
        [ObservableProperty]
        internal float progressBarMin = 0;
        [ObservableProperty]
        internal float progressBarMax = 0;
        [ObservableProperty]
        internal float progressCurrent = 0;
        [ObservableProperty]
        internal string name = string.Empty;
        [ObservableProperty]
        internal bool isCompleted = false;
        [ObservableProperty]
        internal bool isCancelled = false;
        [ObservableProperty]
        internal bool isFileDownloadTask = false;
        [ObservableProperty]
        internal bool showProgressText = true;
        [ObservableProperty]
        internal string currentMirror = string.Empty;
        [ObservableProperty]
        internal string pauseButtonText = "Pause";
        [ObservableProperty]
        internal IBrush textColor = Brushes.White;
        [ObservableProperty]
        internal bool isTextTask = false;

        /* If this task contains subtasks, the subtasks must be added here, from the UIthread */
        [ObservableProperty]
        public ObservableCollection<TaskItemViewModel> taskList = new ObservableCollection<TaskItemViewModel>();
        /* If this task contains subtasks (this) object must be added to this single item list, from the UIthread */
        [ObservableProperty]
        public ObservableCollection<TaskItemViewModel> taskRoot = new ObservableCollection<TaskItemViewModel>();

        private CancellationTokenSource? cancellationTokenSource = null;
        public string? installID = null;
        public string? installVersion = null;
        private bool restartDownload = false;
        private bool pauseDownload = false;

        public TaskItemViewModel() 
        { 
        }

        public async Task<bool> InstallTool(Tool tool, Tool? updateFrom, Action<bool> finishedCallback, CancellationTokenSource? cancelSource = null)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    ProgressBarMax = 2;
                    ProgressCurrent = 0; 
                    ShowProgressText = false; 
                    CancelButtonVisible = true;
                    IsTextTask = false;
                    IsFileDownloadTask = true;
                    Name = "Install Tool: " + tool.name;
                    if(updateFrom!= null)
                    {
                        Name = "Update Tool: " + tool.name;
                    }

                    if (cancelSource != null)
                        cancellationTokenSource = cancelSource;
                    else
                        cancellationTokenSource = new CancellationTokenSource();

                    if (cancellationTokenSource.IsCancellationRequested) 
                        throw new TaskCanceledException();

                    Info = "In Queue";
                    while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                    {
                        await Task.Delay(1000);
                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }
                    }

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    var libPath = Knossos.GetKnossosLibraryPath();

                    if (string.IsNullOrEmpty(libPath))
                        throw new TaskCanceledException("Knossos library path is empty!");

                    var toolPath = Path.Combine(libPath, "tools", tool.name);
                    if(updateFrom != null)
                    {
                        toolPath += "_tool_update";
                    }

                    Directory.CreateDirectory(toolPath);

                    try
                    {
                        File.Create(toolPath + Path.DirectorySeparatorChar + "knossos_net_download.token").Close();
                    }
                    catch { }

                    await Dispatcher.UIThread.InvokeAsync(() => TaskRoot.Add(this));

                    //Download
                    var fileTask = new TaskItemViewModel();
                    await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, fileTask));

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;

                    var url = tool.GetDownloadURL();

                    if (url == null)
                        throw new TaskCanceledException("Tool download URL was null.");

                    var fileName = Path.GetFileName(url);
                    var fileFullPath = toolPath + Path.DirectorySeparatorChar + fileName; 
                    var result = await fileTask.DownloadFile(url, fileFullPath, "Downloading "+ fileName, false, null, cancellationTokenSource);

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    ProgressCurrent++;

                    Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;

                    //Decompress
                    var decompressTask = new TaskItemViewModel();
                    await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, decompressTask));
                    var decompResult = await decompressTask.DecompressNebulaFile(fileFullPath, fileName, toolPath, cancellationTokenSource);
                    if (!decompResult)
                    {
                        Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.InstallTool()", "Error while decompressing the file " + fileFullPath);
                        CancelTaskCommand();
                    }

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    File.Delete(fileFullPath);

                    Knossos.AddTool(tool);

                    try
                    {
                        File.Delete(toolPath + Path.DirectorySeparatorChar + "knossos_net_download.token");
                    }
                    catch { }

                    if (updateFrom != null)
                    {
                        updateFrom.Delete();
                        await Task.Delay(300);
                        var newPath = toolPath.Replace("_tool_update", "");
                        Directory.Move(toolPath, newPath);
                        tool.isFavorite = updateFrom.isFavorite;
                        tool.SaveJson(newPath);
                    }
                    else
                    {
                        tool.SaveJson(toolPath);
                    }

                    if (KnUtils.IsMacOS)
                    {
                        // Binaries on macOS must be signed as of BigSur (11.0) in order to run
                        // on Apple Silicon. So make sure that at least the main executable is
                        // signed ad-hoc after install.
                        //
                        // NOTE: This will *not* replace an existing signature.
                        // NOTE: This will *not* sign libraries or frameworks! The assumption
                        //       is that more complicated tools will already be signed.

                        var executablePath = tool.GetBestPackage()?.executablePath;

                        if ( !string.IsNullOrEmpty(executablePath) )
                        {
                            var execPath = Path.Combine(toolPath, executablePath);

                            try
                            {
                                using var process = new Process();
                                process.StartInfo.FileName = "codesign";
                                process.StartInfo.Arguments = $"-s - \"{execPath}\"";
                                process.StartInfo.CreateNoWindow = true;
                                process.Start();
                            }
                            catch (Exception ex)
                            {
                                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.InstallTool()", ex);
                            }
                        }
                    }

                    ProgressCurrent++;

                    Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;

                    IsCompleted = true;
                    CancelButtonVisible = false;
                    ProgressCurrent = ProgressBarMax;

                    if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                    {
                        TaskViewModel.Instance!.taskQueue.Dequeue();
                    }

                    finishedCallback.Invoke(true);
                    return true;
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (TaskCanceledException)
            {
                //Task cancel requested by user
                IsCompleted = false;
                IsCancelled = true;
                CancelButtonVisible = false;
                Info = "Task Cancelled";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                else
                {
                    //Call cancel task on the parent object
                    cancellationTokenSource?.Cancel();
                }
                while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                {
                    await Task.Delay(500);
                }
                if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                {
                    TaskViewModel.Instance!.taskQueue.Dequeue();
                }
                finishedCallback.Invoke(true);
                return false;
            }
            catch (Exception ex)
            {
                //An exception has happened during task run
                IsCompleted = false;
                CancelButtonVisible = false;
                IsCancelled = true;
                Info = "Task Failed";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                else
                {
                    //Call cancel task on the parent object
                    cancellationTokenSource?.Cancel();
                }
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.InstallTool()", ex);
                while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                {
                    await Task.Delay(500);
                }
                if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                {
                    TaskViewModel.Instance!.taskQueue.Dequeue();
                }
                finishedCallback.Invoke(true);
                return false;
            }
        }

        private async Task<string> PreFlightCheck(Mod mod, CancellationTokenSource? cancelSource = null)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    ProgressBarMax = 0;
                    ProgressCurrent = 0;
                    ShowProgressText = false;
                    CancelButtonVisible = false;
                    IsTextTask = false;
                    IsFileDownloadTask = false;
                    Name = "Pre-flight Check";

                    if (cancelSource != null)
                        cancellationTokenSource = cancelSource;
                    else
                        cancellationTokenSource = new CancellationTokenSource();

                    if (cancellationTokenSource.IsCancellationRequested)
                        throw new TaskCanceledException();

                    var cleanMod = new Mod();
                    cleanMod.id = mod.id;
                    cleanMod.title = mod.title;
                    cleanMod.type = mod.type;
                    cleanMod.parent = mod.parent;
                    cleanMod.cmdline = mod.cmdline;
                    cleanMod.description = mod.description;
                    cleanMod.version = mod.version;
                    cleanMod.firstRelease = mod.firstRelease;
                    cleanMod.lastUpdate = mod.lastUpdate;
                    cleanMod.isPrivate = mod.isPrivate;
                    cleanMod.videos = mod.videos;
                    cleanMod.packages = mod.packages;
                    cleanMod.stability = mod.stability;
                    cleanMod.releaseThread = mod.releaseThread;
                    cleanMod.tile = string.Empty;
                    cleanMod.banner = string.Empty;
                    cleanMod.screenshots = new string[0];
                    cleanMod.attachments = new string[0];

                    var result = await Nebula.PreflightCheck(cleanMod);
                    if (result == null || ( result != "ok" && result.ToLower() != "duplicated version"))
                    {
                        if (result != null)
                        {
                            Info = "Preflight check failed. Reason: " + result;
                        }
                        else
                        {
                            Info = "Preflight check failed for unknown reasons.";
                            throw new TaskCanceledException();
                        }
                    }
                    Info = result;
                    IsCompleted = true;
                    CancelButtonVisible = false;
                    ProgressCurrent = ProgressBarMax;
                    return result;
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (TaskCanceledException)
            {
                //Task cancel requested by user
                IsCompleted = false;
                IsCancelled = true;
                CancelButtonVisible = false;
                Info = "Task Cancelled";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                else
                {
                    //Call cancel task on the parent object
                    cancellationTokenSource?.Cancel();
                }
                return "fail";
            }
            catch (Exception ex)
            {
                //An exception has happened during task run
                IsCompleted = false;
                CancelButtonVisible = false;
                IsCancelled = true;
                Info = "Task Failed";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                else
                {
                    //Call cancel task on the parent object
                    cancellationTokenSource?.Cancel();
                }
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.PreFlightCheck()", ex);
                return "fail";
            }
        }

        private async Task<string> ReleaseMod(Mod mod, bool metaUpdate,CancellationTokenSource? cancelSource = null)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    ProgressBarMax = 0;
                    ProgressCurrent = 0;
                    ShowProgressText = false;
                    CancelButtonVisible = false;
                    IsTextTask = false;
                    IsFileDownloadTask = false;

                    if (metaUpdate)
                        Name = "Metadata Update";
                    else
                        Name = "Release Mod";

                    if (cancelSource != null)
                        cancellationTokenSource = cancelSource;
                    else
                        cancellationTokenSource = new CancellationTokenSource();

                    if (cancellationTokenSource.IsCancellationRequested)
                        throw new TaskCanceledException();

                    var cleanMod = new Mod();
                    cleanMod.id = mod.id;
                    cleanMod.title = mod.title;
                    cleanMod.firstRelease = mod.firstRelease;
                    cleanMod.tile = mod.tile;
                    cleanMod.version = mod.version;

                    //Update title and tile image
                    await Nebula.UpdateMod(cleanMod);

                    cleanMod.type = mod.type;
                    cleanMod.parent = mod.parent;
                    cleanMod.cmdline = mod.cmdline == null ? "" : mod.cmdline;
                    cleanMod.description = mod.description == null ? "" : mod.description;
                    cleanMod.lastUpdate = mod.lastUpdate;
                    cleanMod.isPrivate = mod.isPrivate;
                    cleanMod.videos = mod.videos == null ? new string[0] : mod.videos;
                    cleanMod.packages = mod.packages;
                    cleanMod.stability = mod.stability;
                    cleanMod.releaseThread = mod.releaseThread;
                    cleanMod.modFlag = mod.modFlag;
                    cleanMod.banner = mod.banner == null ? "" : mod.banner;
                    cleanMod.screenshots = mod.screenshots == null ? new string[0] : mod.screenshots;
                    cleanMod.attachments = new string[0];
                    cleanMod.members = new List<ModMember>();
                    cleanMod.notes = mod.notes == null ? "" : mod.notes;

                    if(cleanMod.packages != null && cleanMod.packages.Any())
                    {
                        foreach(var pkg in cleanMod.packages)
                        {
                            if(pkg.dependencies == null)
                                pkg.dependencies = new ModDependency[0];
                        }
                    }

                    string? result;

                    if (!metaUpdate)
                    {
                        result = await Nebula.ReleaseMod(cleanMod);
                    }
                    else
                    {
                        result = await Nebula.UpdateMetaData(cleanMod);
                    }

                    if (result == null || result != "ok")
                    {
                        if (result != null)
                        {
                            Info = "Release Mod failed. Reason: " + result;
                        }
                        else
                        {
                            Info = "Release Mod failed for unknown reasons.";
                            throw new TaskCanceledException();
                        }
                    }
                    Info = result;
                    IsCompleted = true;
                    CancelButtonVisible = false;
                    ProgressCurrent = ProgressBarMax;
                    return result;
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (TaskCanceledException)
            {
                //Task cancel requested by user
                IsCompleted = false;
                IsCancelled = true;
                CancelButtonVisible = false;
                Info = "Task Cancelled";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                else
                {
                    //Call cancel task on the parent object
                    cancellationTokenSource?.Cancel();
                }
                return "fail";
            }
            catch (Exception ex)
            {
                //An exception has happened during task run
                IsCompleted = false;
                CancelButtonVisible = false;
                IsCancelled = true;
                Info = "Task Failed";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                else
                {
                    //Call cancel task on the parent object
                    cancellationTokenSource?.Cancel();
                }
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.ReleaseMod()", ex);
                return "fail";
            }
        }

        private async Task<bool> UploadModImages(Mod mod, CancellationTokenSource? cancelSource = null)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    ProgressBarMax = 2;
                    ProgressCurrent = 0;
                    ShowProgressText = false;
                    CancelButtonVisible = false;
                    IsTextTask = false;
                    IsFileDownloadTask = false;
                    Name = "Uploading Mod Images";

                    if (cancelSource != null)
                        cancellationTokenSource = cancelSource;
                    else
                        cancellationTokenSource = new CancellationTokenSource();

                    if (cancellationTokenSource.IsCancellationRequested)
                        throw new TaskCanceledException();

                    //Screenshots
                    if (mod.screenshots != null && mod.screenshots.Any())
                    {
                        ProgressBarMax += mod.screenshots.Length;
                        var list = new List<string>();
                        var i = 1;
                        foreach (var sc in mod.screenshots)
                        {
                            Info = "Screenshot Image " + i + " / " + mod.screenshots.Length;
                            var cks = await Nebula.UploadImage(mod.fullPath + Path.DirectorySeparatorChar + sc);
                            if (cks != null)
                            {
                                list.Add(cks);
                            }
                            ProgressCurrent++;
                        }
                        mod.screenshots = list.ToArray();
                    }
                    //Tile
                    if (!string.IsNullOrEmpty(mod.tile))
                    {
                        Info = "Tile Image";
                        mod.tile = await Nebula.UploadImage(mod.fullPath + Path.DirectorySeparatorChar + mod.tile);
                    }
                    ProgressCurrent++;
                    //Banner
                    if (!string.IsNullOrEmpty(mod.banner))
                    {
                        Info = "Banner Image";
                        mod.banner = await Nebula.UploadImage(mod.fullPath + Path.DirectorySeparatorChar + mod.banner);
                    }
                    ProgressCurrent++;

                    Info = "OK";
                    IsCompleted = true;
                    CancelButtonVisible = false;
                    ProgressCurrent = ProgressBarMax;
                    return true;
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (TaskCanceledException)
            {
                //Task cancel requested by user
                IsCompleted = false;
                IsCancelled = true;
                CancelButtonVisible = false;
                Info = "Task Cancelled";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                else
                {
                    //Call cancel task on the parent object
                    cancellationTokenSource?.Cancel();
                }
                return false;
            }
            catch (Exception ex)
            {
                //An exception has happened during task run
                IsCompleted = false;
                CancelButtonVisible = false;
                IsCancelled = true;
                Info = "Task Failed";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                else
                {
                    //Call cancel task on the parent object
                    cancellationTokenSource?.Cancel();
                }
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.UploadModImages()", ex);
                return false;
            }
        }

        private async Task<bool> CreateModNebula(Mod mod, CancellationTokenSource? cancelSource = null)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    ProgressBarMax = 0; 
                    ProgressCurrent = 0;
                    ShowProgressText = false;
                    CancelButtonVisible = false;
                    IsTextTask = false;
                    IsFileDownloadTask = false; 
                    Name = "Creating mod on Nebula Database";

                    if (cancelSource != null)
                        cancellationTokenSource = cancelSource;
                    else
                        cancellationTokenSource = new CancellationTokenSource();

                    if (cancellationTokenSource.IsCancellationRequested) 
                        throw new TaskCanceledException();

                    var cleanMod = new Mod();
                    cleanMod.id = mod.id;
                    cleanMod.type = mod.type;
                    cleanMod.title = mod.title;
                    cleanMod.parent = mod.parent;
                    cleanMod.isPrivate = true;
                    var result = await Nebula.CreateMod(cleanMod);
                    if (result == null || result != "ok")
                    {
                        if (result != null)
                        {
                            Info = "Create mod fail. Reason: " + result;
                        }
                        else
                        {
                            Info = "Create mod failed for unknown reasons.";
                        }
                        throw new TaskCanceledException();
                    }
                    Info = "Done";
                    IsCompleted = true;
                    CancelButtonVisible = false; 
                    ProgressCurrent = ProgressBarMax; 
                    return true; 
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (TaskCanceledException)
            {
                //Task cancel requested by user
                IsCompleted = false;
                IsCancelled = true;
                CancelButtonVisible = false;
                Info = "Task Cancelled";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                else
                {
                    //Call cancel task on the parent object
                    cancellationTokenSource?.Cancel();
                }
                return false;
            }
            catch (Exception ex)
            {
                //An exception has happened during task run
                IsCompleted = false;
                CancelButtonVisible = false;
                IsCancelled = true;
                Info = "Task Failed";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                else
                {
                    //Call cancel task on the parent object
                    cancellationTokenSource?.Cancel();
                }
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.CreateModNebula()", ex);
                return false;
            }
        }

        private async Task<bool> UploadModPkg(ModPackage pkg, string modFullPath, CancellationTokenSource? cancelSource = null)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    ProgressBarMax = 100;
                    ProgressCurrent = 0;
                    ShowProgressText = false;
                    CancelButtonVisible = false;
                    IsTextTask = false;
                    IsFileDownloadTask = false;
                    Name = "Uploading: " + pkg.name;

                    if (cancelSource != null)
                        cancellationTokenSource = cancelSource;
                    else
                        cancellationTokenSource = new CancellationTokenSource();

                    if (cancellationTokenSource.IsCancellationRequested)
                        throw new TaskCanceledException();

                    var zipPath = modFullPath + Path.DirectorySeparatorChar + "kn_upload" + Path.DirectorySeparatorChar + pkg.folder + ".7z";
                    if (pkg.environment != null && pkg.environment.ToLower().Contains("macos"))
                    {
                        zipPath = modFullPath + Path.DirectorySeparatorChar + "kn_upload" + Path.DirectorySeparatorChar + pkg.folder + ".tar.gz";
                    }
                    if (!File.Exists(zipPath))
                    {
                        throw new TaskCanceledException();
                    }

                    Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.UploadPkg()", "Uploading: " + zipPath);

                    var multi = new Nebula.MultipartUploader(zipPath, cancellationTokenSource, multiuploaderCallback);
                    if (!await multi.Upload())
                    {
                        throw new TaskCanceledException();
                    }
                    await Task.Delay(300);
                    //Info = "OK";
                    IsCompleted = true;
                    CancelButtonVisible = false;
                    ProgressCurrent = ProgressBarMax;
                    return true;
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (TaskCanceledException)
            {
                //Task cancel requested by user
                IsCompleted = false;
                IsCancelled = true;
                CancelButtonVisible = false;
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                else
                {
                    //Call cancel task on the parent object
                    cancellationTokenSource?.Cancel();
                }
                return false;
            }
            catch (Exception ex)
            {
                //An exception has happened during task run
                IsCompleted = false;
                CancelButtonVisible = false;
                IsCancelled = true;
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                else
                {
                    //Call cancel task on the parent object
                    cancellationTokenSource?.Cancel();
                }
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.UploadModPkg()", ex);
                return false;
            }
        }

        private async Task<bool> PrepareModPkg(ModPackage pkg, string modFullPath, CancellationTokenSource? cancelSource = null)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    ProgressBarMax = 0;
                    ProgressCurrent = 0;
                    ShowProgressText = false;
                    CancelButtonVisible = false;
                    IsTextTask = false;
                    IsFileDownloadTask = false;
                    Name = "Prepare Pkg: " + pkg.name;

                    if (cancelSource != null)
                        cancellationTokenSource = cancelSource;
                    else
                        cancellationTokenSource = new CancellationTokenSource();

                    if (cancellationTokenSource.IsCancellationRequested)
                        throw new TaskCanceledException();

                    //Create VP if needed
                    //Create filelist
                    //Filename, archive(7z), orig_name, checksum
                    //Compress with 7z
                    //Clear files.urls
                    //Fill file.filename, file.checksum, file.dest, file.filesize
                    //Note: MacOSX builds must be compressed as tar.gz keeping symblinks as links

                    if (!Directory.Exists(modFullPath + Path.DirectorySeparatorChar + pkg.folder))
                    {
                        Info = "Fail - No Dir";
                        IsCompleted = true;
                        CancelButtonVisible = false;
                        ProgressCurrent = ProgressBarMax;
                        Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.PrepareModPkg()", "Package folder: " + modFullPath + Path.DirectorySeparatorChar + pkg.folder + " does not exist.");
                        throw new TaskCanceledException();
                    }

                    var allfiles = Directory.GetFiles(modFullPath + Path.DirectorySeparatorChar + pkg.folder, "*.*", SearchOption.AllDirectories);
                    if (!allfiles.Any())
                    {
                        Info = "Fail - No Files";
                        IsCompleted = true;
                        CancelButtonVisible = false;
                        ProgressCurrent = ProgressBarMax;
                        Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.PrepareModPkg()", "Package folder: " + modFullPath + Path.DirectorySeparatorChar + pkg.folder + " is empty.");
                        throw new TaskCanceledException();
                    }


                    var zipPath = modFullPath + Path.DirectorySeparatorChar + "kn_upload" + Path.DirectorySeparatorChar + pkg.folder + ".7z";
                    if (pkg.environment != null && pkg.environment.ToLower().Contains("macos"))
                    {
                        zipPath = modFullPath + Path.DirectorySeparatorChar + "kn_upload" + Path.DirectorySeparatorChar + pkg.folder;
                    }
                    if (File.Exists(zipPath))
                    {
                        File.Delete(zipPath);
                    }

                    var filelist = new List<ModFilelist>();
                    var pkgFile = new ModFile();
                    var files = new List<ModFile>() { pkgFile };

                    if (pkg.isVp)
                    {
                        Info = "Creating VP";
                        ProgressBarMax = 100;
                        ProgressCurrent = 0;
                        var vpPath = modFullPath + Path.DirectorySeparatorChar + "kn_upload" + Path.DirectorySeparatorChar + "vps" + Path.DirectorySeparatorChar + pkg.name + ".vp";
                        Directory.CreateDirectory(modFullPath + Path.DirectorySeparatorChar + "kn_upload" + Path.DirectorySeparatorChar + "vps");
                        if(File.Exists(vpPath))
                        {
                            File.Delete(vpPath);
                        }
                        var vp = new VPContainer();
                        vp.AddFolderToRoot(modFullPath + Path.DirectorySeparatorChar + pkg.folder);
                        vp.DisableCompression();
                        await vp.SaveAsAsync(vpPath, compressionCallback, cancellationTokenSource);
                        Info = "Get VP Checksum";
                        var checksumVP = await KnUtils.GetFileHash(vpPath);
                        if( checksumVP != null )
                        {
                            Info = "Compressing (7z)";
                            ProgressBarMax = 100;
                            ProgressCurrent = 0;
                            using (var compressor = new SevenZipConsoleWrapper(sevenZipCallback, cancellationTokenSource))
                            {
                                if (!await compressor.CompressFile(vpPath, modFullPath + Path.DirectorySeparatorChar + "kn_upload" + Path.DirectorySeparatorChar + "vps", zipPath, true))
                                {
                                    Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.PrepareModPkg()", "Error while compressing the package");
                                    throw new TaskCanceledException();
                                }
                            }
                            var fl = new ModFilelist();
                            fl.archive = pkg.folder + ".7z";
                            fl.filename = fl.origName = pkg.folder + ".vp";
                            fl.checksum = new string[2] { "sha256", checksumVP };
                            filelist.Add(fl);
                        }
                        else
                        {
                            throw new TaskCanceledException();
                        }
                        
                    }
                    else
                    {
                        Info = "Adding files";
                        foreach (var file in allfiles)
                        {
                            //Do not add symblinks
                            var fi = new FileInfo(file);
                            if (fi.LinkTarget == null)
                            {
                                var relativePath = Path.GetRelativePath(modFullPath + Path.DirectorySeparatorChar + pkg.folder, file).Replace(@"\", @"/");
                                var checksum = await KnUtils.GetFileHash(file);
                                if (checksum != null)
                                {
                                    var fl = new ModFilelist();
                                    fl.archive = pkg.folder + ".7z";
                                    fl.filename = fl.origName = relativePath;
                                    fl.checksum = new string[2] { "sha256", checksum };
                                    filelist.Add(fl);
                                }
                                else
                                {
                                    throw new TaskCanceledException();
                                }
                            }
                        }

                        ProgressBarMax = 100;
                        ProgressCurrent = 0;
                        using (var compressor = new SevenZipConsoleWrapper(sevenZipCallback, cancellationTokenSource))
                        {
                            if (pkg.environment != null && pkg.environment.ToLower().Contains("macos"))
                            {
                                Info = "Compressing (.tar.gz)";
                                if (!await compressor.CompressFolderTarGz(modFullPath + Path.DirectorySeparatorChar + pkg.folder, zipPath))
                                {
                                    Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.PrepareModPkg()", "Error while compressing the package");
                                    throw new TaskCanceledException();
                                }
                                zipPath += ".tar.gz";
                            }
                            else
                            {
                                Info = "Compressing (7z)";
                                if (!await compressor.CompressFolder(modFullPath + Path.DirectorySeparatorChar + pkg.folder, zipPath))
                                {
                                    Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.PrepareModPkg()", "Error while compressing the package");
                                    throw new TaskCanceledException();
                                }
                            }
                        }
                    }

                    Info = "Getting Hash";
                    /*
                     * TODO: it is unclear to me, at this moment, why this would be needed since 7z should extract with fullpath.
                     * Using the pkg as work folder. 
                     * FSO builds seems to use it.
                     *
                    */
                    pkgFile.dest = "";
                    var checksumZip = await KnUtils.GetFileHash(zipPath);
                    if (checksumZip != null)
                    {
                        var fi = new FileInfo(zipPath);
                        pkgFile.filesize = fi.Length;
                        pkgFile.filename = pkg.folder + ".7z";
                        pkgFile.checksum = new string[2] { "sha256", checksumZip };
                    }
                    else
                    {
                        throw new TaskCanceledException();
                    }
                    pkgFile.urls = null;
                    pkg.files = files.ToArray();
                    pkg.filelist = filelist.ToArray();
                    if(pkg.executables == null)
                    {
                        pkg.executables = new List<ModExecutable>();
                    }
                    if(pkg.dependencies == null)
                    {
                        pkg.dependencies = new ModDependency[0];
                    }
                    Info = "OK";
                    IsCompleted = true;
                    CancelButtonVisible = false;
                    ProgressCurrent = ProgressBarMax;
                    return true;
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (TaskCanceledException)
            {
                //Task cancel requested by user
                IsCompleted = false;
                IsCancelled = true;
                CancelButtonVisible = false;
                Info = "Task Cancelled";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                else
                {
                    //Call cancel task on the parent object
                    cancellationTokenSource?.Cancel();
                }
                return false;
            }
            catch (Exception ex)
            {
                //An exception has happened during task run
                IsCompleted = false;
                CancelButtonVisible = false;
                IsCancelled = true;
                Info = "Task Failed";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                else
                {
                    //Call cancel task on the parent object
                    cancellationTokenSource?.Cancel();
                }
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.PrepareModPkg()", ex);
                return false;
            }
        }

        public async Task<bool> UploadModVersion(Mod mod, bool isNewMod, bool metaOnly, CancellationTokenSource? cancelSource = null)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    ProgressBarMax = 5;
                    ProgressCurrent = 0; 
                    ShowProgressText = false;
                    CancelButtonVisible = true;
                    IsTextTask = false;
                    IsFileDownloadTask = false;
                    Info = "In Queue";
                    Name = "Uploading " + mod.ToString();
                    await Dispatcher.UIThread.InvokeAsync(() => TaskRoot.Add(this));

                    if (cancelSource != null)
                        cancellationTokenSource = cancelSource;
                    else
                        cancellationTokenSource = new CancellationTokenSource();

                    //Wait in Queue
                    while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                    {
                        await Task.Delay(1000);
                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }
                    }
                    Info = "";
                    //If we are doing only meta skip to the end
                    if (!metaOnly)
                    {
                        ProgressBarMax += mod.packages.Count() * 2;

                        if (isNewMod)
                        {
                            Info = "Create Mod";
                            if (cancellationTokenSource.IsCancellationRequested)
                                throw new TaskCanceledException();
                            var create = new TaskItemViewModel();
                            await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, create));
                            await create.CreateModNebula(mod, cancellationTokenSource);
                            //If fails it should trigger cancel no need to check the return
                        }

                        ProgressCurrent++;

                        if (cancellationTokenSource.IsCancellationRequested)
                            throw new TaskCanceledException();

                        //At this point the mod id exist and we should have write access

                        /*
                            UPLOAD PROCESS:
                            1) Do pre_flight API call, im guessing that if the mod version is already uploaded Nebula will report that here somehow. YES: "duplicated version"
                            2) Upload mod tile image(check if already uploaded), get checksum and import it on modjson.
                            3) Upload banner image and screenshots(check if already uploaded), get checksum and import it on modjson.
                            4) If package = vp create a vp in mod\kn_upload\vps\{ packagename}.vp(No Compression)
                            5) 7z all packages folders and vp file and place them in kn_upload\{ packagename}.7z
                            6) Wipe and re - generate data in package.files and filelist. "files" is for the 7z file we are uploading to nebula. "filelist" is for all files inside the package folder(folder or vp)
                            7) Use multipartuploader to upload all packages(will auto-skip if already uploaded)
                            8) Api Call to "mod/release" with the mod meta(full json)
                        */

                        //Preflight check
                        Info = "PreFlight Check";
                        var newTask = new TaskItemViewModel();
                        await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, newTask));
                        var preFlightCheck = await newTask.PreFlightCheck(mod, cancellationTokenSource);

                        ProgressCurrent++;

                        if (cancellationTokenSource.IsCancellationRequested)
                            throw new TaskCanceledException();

                        //At this point preflight check was valid otherwise it would trigger a cancel, only check if it is a duplicated version, if it is skip to meta
                        if (preFlightCheck == "ok")
                        {
                            //We are good. Im leaving image upload for meta stage
                            Info = "Prepare Packages";
                            Directory.CreateDirectory(mod.fullPath + Path.DirectorySeparatorChar + "kn_upload");
                            //Prepare packages, update data on mod
                            await Parallel.ForEachAsync(mod.packages, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (pkg, token) =>
                            {
                                if (mod.type != ModType.mod && mod.type != ModType.tc) //Just to be sure
                                    pkg.isVp = false;
                                var newTask = new TaskItemViewModel();
                                await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, newTask));
                                await newTask.PrepareModPkg(pkg, mod.fullPath, cancellationTokenSource);
                                ProgressCurrent++;
                                if (cancellationTokenSource.IsCancellationRequested)
                                    throw new TaskCanceledException();
                            });

                            Info = "Upload Packages";
                            //Upload Packages
                            await Parallel.ForEachAsync(mod.packages, new ParallelOptions { MaxDegreeOfParallelism = 1 }, async (pkg, token) =>
                            {
                                var newTask = new TaskItemViewModel();
                                await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, newTask));
                                await newTask.UploadModPkg(pkg, mod.fullPath, cancellationTokenSource);
                                ProgressCurrent++;
                                if (cancellationTokenSource.IsCancellationRequested)
                                    throw new TaskCanceledException();
                            });
                        }
                        else
                        {
                            if (preFlightCheck == "duplicated version")
                            {
                                ProgressBarMax -= mod.packages.Count() * 2;
                                metaOnly = true;
                            }
                        }
                    }
                    else
                    {
                        ProgressBarMax--;
                    }

                    if (cancellationTokenSource.IsCancellationRequested)
                        throw new TaskCanceledException();

                    //Meta Stage
                    Info = "Upload Images";
                    //I need to save the original data for tile, banner and screenshots in order not to override local paths
                    var origTile = mod.tile;
                    var origBanner = mod.banner;
                    var origScreenshots = mod.screenshots != null ? mod.screenshots.ToArray() : null;

                    var imgs = new TaskItemViewModel();
                    await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, imgs));
                    await imgs.UploadModImages(mod,cancellationTokenSource);

                    ProgressCurrent++;

                    if (cancellationTokenSource.IsCancellationRequested)
                        throw new TaskCanceledException();

                    //Meta
                    Info = "Upload Metadata";
                    var meta = new TaskItemViewModel();
                    await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, meta));
                    await meta.ReleaseMod(mod, metaOnly, cancellationTokenSource);

                    ProgressCurrent++;

                    if (cancellationTokenSource.IsCancellationRequested)
                        throw new TaskCanceledException();

                    //Restore paths & save
                    mod.tile = origTile;
                    mod.banner = origBanner;
                    mod.screenshots = origScreenshots;
                    //mark all pkgs as enabled
                    mod.packages?.ForEach(pkg => pkg.isEnabled = true);
                    mod.SaveJson();

                    ProgressCurrent++;

                    if (!metaOnly)
                        Info = "Upload Complete!";
                    else
                        Info = "Metadata Updated!";

                    //Completed
                    mod.inNebula = true;
                    IsCompleted = true; 
                    CancelButtonVisible = false;
                    ProgressCurrent = ProgressBarMax;

                    //Delete kn_upload folder?
                    if (Knossos.globalSettings.deleteUploadedFiles && Directory.Exists(mod.fullPath + Path.DirectorySeparatorChar + "kn_upload"))
                    {
                        try
                        {
                            Directory.Delete(mod.fullPath + Path.DirectorySeparatorChar + "kn_upload", true);
                        }
                        catch (Exception ex)
                        {
                            Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.UploadModVersion()", ex);
                        }
                    }

                    //Reload version editor if needed
                    if(!metaOnly)
                        DeveloperModsViewModel.Instance?.UpdateVersionManager(mod.id);

                    if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                    {
                        TaskViewModel.Instance!.taskQueue.Dequeue();
                    }

                    return true;
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (TaskCanceledException)
            {
                //Task cancel requested by user
                IsCompleted = false;
                IsCancelled = true;
                CancelButtonVisible = false;
                Info = "Task Cancelled";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                {
                    await Task.Delay(500);
                }
                if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                {
                    TaskViewModel.Instance!.taskQueue.Dequeue();
                }
                return false;
            }
            catch (Exception ex)
            {
                //An exception has happened during task run
                IsCompleted = false;
                CancelButtonVisible = false;
                IsCancelled = true;
                Info = "Task Failed";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                else
                {
                    //Call cancel task on the parent object
                    cancellationTokenSource?.Cancel();
                }
                while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                {
                    await Task.Delay(500);
                }
                if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                {
                    TaskViewModel.Instance!.taskQueue.Dequeue();
                }
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.UploadModVersion()", ex);
                return false;
            }
        }

        public async Task<bool> CreateModVersion(Mod oldMod, string newVersion, CancellationTokenSource? cancelSource = null)
        {
            var newDir = string.Empty;
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    ProgressBarMax = 1;
                    ProgressCurrent = 0;
                    ShowProgressText = false;
                    CancelButtonVisible = true;
                    Name = "Creating Mod Version: " + oldMod.title + " " + newVersion;
                    var currentDir = new DirectoryInfo(oldMod.fullPath);
                    var parentDir = currentDir.Parent;
                    newDir = parentDir!.FullName + Path.DirectorySeparatorChar + oldMod.id + "-" + newVersion;

                    if (cancelSource != null)
                    {
                        cancellationTokenSource = cancelSource;
                    }
                    else
                    {
                        cancellationTokenSource = new CancellationTokenSource();
                    }

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    //Wait in Queue
                    while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                    {
                        await Task.Delay(1000);
                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }
                    }

                    ProgressBarMax = Directory.GetFiles(currentDir.FullName, "*", SearchOption.AllDirectories).Length;

                    Directory.CreateDirectory(newDir);

                    using (StreamWriter writer = new StreamWriter(newDir + Path.DirectorySeparatorChar + "knossos_net_download.token"))
                    {
                        writer.WriteLine("Warning: This token indicates an incomplete folder copy. If this token is present on the next Knet startup this folder WILL BE DELETED.");
                    }

                    await KnUtils.CopyDirectoryAsync(currentDir.FullName, newDir, true, cancellationTokenSource, copyCallback);

                    File.Delete(newDir + Path.DirectorySeparatorChar + "knossos_net_download.token");

                    var newMod = new Mod(newDir, oldMod.id + "-" + newVersion);
                    newMod.version = newVersion;
                    newMod.SaveJson();

                    if (newMod.type == ModType.engine)
                    {
                        var build = new FsoBuild(newMod);
                        await Dispatcher.UIThread.InvokeAsync(() => Knossos.AddBuild(build));
                        await Dispatcher.UIThread.InvokeAsync(() => FsoBuildsViewModel.Instance!.AddBuildToUi(build));
                    }
                    else
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => Knossos.AddMod(newMod));
                        await Dispatcher.UIThread.InvokeAsync(() => MainWindowViewModel.Instance!.AddInstalledMod(newMod));
                        
                    }
                    await Dispatcher.UIThread.InvokeAsync(() => MainWindowViewModel.Instance!.AddDevMod(newMod));

                    Info = "";
                    IsCompleted = true;
                    CancelButtonVisible = false;
                    ProgressCurrent = ProgressBarMax;

                    while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                    {
                        await Task.Delay(500);
                    }
                    if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                    {
                        TaskViewModel.Instance!.taskQueue.Dequeue();
                    }

                    return true;
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
                IsCancelled = true;
                CancelButtonVisible = false;
                Info = "Task Cancelled";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                try
                {
                    Directory.Delete(newDir, true);
                }
                catch { }

                while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                {
                    await Task.Delay(500);
                }
                if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                {
                    TaskViewModel.Instance!.taskQueue.Dequeue();
                }
                return false;
            }
            catch (Exception ex)
            {
                IsCompleted = false;
                CancelButtonVisible = false;
                IsCancelled = true;
                Info = "Task Failed";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.CreateModVersion()", ex);
                try
                {
                    Directory.Delete(newDir, true);
                }
                catch { }

                while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                {
                    await Task.Delay(500);
                }
                if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                {
                    TaskViewModel.Instance!.taskQueue.Dequeue();
                }
                return false;
            }
        }

        private async Task<bool> ExtractVP(FileInfo vpFile, CancellationTokenSource? cancelSource = null)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    ProgressBarMax = 1;
                    ProgressCurrent = 0;
                    ShowProgressText = false;
                    if (cancelSource != null)
                    {
                        cancellationTokenSource = cancelSource;
                    }
                    else
                    {
                        cancellationTokenSource = new CancellationTokenSource();
                    }
                    CancelButtonVisible = false;
                    Name = "Extracting: " + vpFile.Name;

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.ExtractVP()", "Starting to extractVP VP file: " + vpFile.Name);

                    await Task.Run(async () => {
                        var vp = new VPContainer();
                        await vp.LoadVP(vpFile.FullName);
                        await vp.ExtractVpAsync(vpFile.Directory!.FullName,extractCallback);
                    });

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        try
                        {
                            File.Delete(vpFile.FullName);
                        }
                        catch { }
                        throw new TaskCanceledException();
                    }
                    File.Delete(vpFile.FullName);
                    Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.ExtractVP()", "ExtractVP VP finished: " + vpFile.Name + " Processed Files: " + ProgressBarMax);
                    Info = "";
                    IsCompleted = true;
                    ProgressCurrent = ProgressBarMax;
                    return true;
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
                IsCancelled = true;
                CancelButtonVisible = false;
                Info = "Task Cancelled";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                return false;
            }
            catch (Exception ex)
            {
                IsCompleted = false;
                CancelButtonVisible = false;
                IsCancelled = true;
                Info = "Task Failed";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.ExtractVP()", ex);
                return false;
            }
        }

        public void DisplayUpdates(List<Mod> updatedMods)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    IsCompleted = true;
                    IsTextTask = true;
                    var newMods = updatedMods.Where(x => x.isNewMod && x.type == ModType.mod);
                    var newTCs = updatedMods.Where(x => x.isNewMod && x.type == ModType.tc);
                    var newEngine = updatedMods.Where(x => x.type == ModType.engine);
                    var updateMods = updatedMods.Where(x => !x.isNewMod && x.type != ModType.engine);

                    Name = "Repo Changes:";
                    if(newMods != null && newMods.Any()) 
                    {
                        Name += " New Mods: " + newMods.Count();
                        foreach (var nm in newMods)
                        {
                            var newTask = new TaskItemViewModel();
                            newTask.ShowMsg("Mod Released!   " + nm, null, Brushes.Green);
                            Dispatcher.UIThread.InvokeAsync( () =>
                            {
                                TaskList.Add(newTask);
                            });
                        }
                    }
                    if (newTCs != null && newTCs.Any())
                    {
                        Name += " TCs: " + newTCs.Count();
                        foreach (var nTc in newTCs)
                        {
                            var newTask = new TaskItemViewModel();
                            newTask.ShowMsg("Total Conversion Released!  " + nTc, null, Brushes.Green);
                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                TaskList.Add(newTask);
                            });
                        }
                    }
                    if (newEngine != null && newEngine.Any())
                    {
                        Name += " Engine Builds: " + newEngine.Count();
                        foreach (var ne in newEngine)
                        {

                            var newTask = new TaskItemViewModel();
                            newTask.ShowMsg("Engine Build Released!  " + ne, null, Brushes.Yellow);
                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                TaskList.Add(newTask);
                            });
                        }
                    }
                    if (updateMods != null && updateMods.Any())
                    {
                        Name += " Mod Updates: " + updateMods.Count();
                        foreach (var nm in updateMods)
                        {
                            var newTask = new TaskItemViewModel();
                            newTask.ShowMsg("Mod Update Released!  " + nm, null, Brushes.LightBlue);
                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                TaskList.Add(newTask);
                            });
                        }
                    }
                    Dispatcher.UIThread.InvokeAsync(() => TaskRoot.Add(this));
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.DisplayUpdates()", ex);
            }
        }

        public void ShowMsg(string msg, string? tooltip, IBrush? textColor = null)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    IsCompleted = true;
                    IsTextTask = true;
                    Name = msg;
                    if (tooltip != null)
                    {
                        Tooltip = tooltip.Trim();
                        TooltipVisible = true;
                    }
                    if(textColor != null)
                    {
                        TextColor = textColor;
                    }
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.ShowMsg()", ex);
            }
        }

        private async Task<bool> DecompressLosseFiles(List<string> filePaths, int alreadySkipped, CancellationTokenSource? cancelSource = null)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    ProgressBarMax = filePaths.Count();
                    ProgressCurrent = 0;
                    ShowProgressText = false;
                    if (cancelSource != null)
                    {
                        cancellationTokenSource = cancelSource;
                    }
                    else
                    {
                        cancellationTokenSource = new CancellationTokenSource();
                    }
                    CancelButtonVisible = false;
                    Name = "Decompressing loose files";

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    int skippedCount = alreadySkipped;
                    int decompressedCount = 0;

                    Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.DecompressLosseFiles()", "Starting to decompress loose files");

                    await Parallel.ForEachAsync(filePaths, new ParallelOptions { MaxDegreeOfParallelism = Knossos.globalSettings.compressionMaxParallelism }, async (file, token) =>
                    {
                        var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                        BinaryReader br = new BinaryReader(input);

                        if (!input.CanRead)
                        {
                            input.Dispose();
                            throw new TaskCanceledException();
                        }

                        //Verify if it is compressed
                        if (Encoding.ASCII.GetString(br.ReadBytes(4)) == "LZ41")
                        {
                            FileInfo fi = new FileInfo(file);
                            Info = ProgressCurrent + " / " + ProgressBarMax + " " + fi.Name;
                            input.Seek(0, SeekOrigin.Begin);
                            var output = new FileStream(fi.FullName.Replace(".lz41",string.Empty,StringComparison.OrdinalIgnoreCase), FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                            if (!output.CanWrite)
                            {
                                input.Dispose();
                                output.Dispose();
                                throw new TaskCanceledException();
                            }

                            VPCompression.DecompressStream(input, output);

                            //Delete original
                            input.Dispose();
                            output.Dispose();
                            File.Delete(file);
                            decompressedCount++;
                        }
                        else
                        {
                            skippedCount++;
                        }
                        await input.DisposeAsync();
                        ProgressCurrent++;

                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }
                    });

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    IsCompleted = true;
                    ProgressCurrent = ProgressBarMax;
                    Info = "Decompressed: " + decompressedCount + " Skipped: " + skippedCount;
                    Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.DecompressLosseFiles()", "Decompressing loose files finished: " + Info);
                    return true;
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
                IsCancelled = true;
                CancelButtonVisible = false;
                Info = "Task Cancelled";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                return false;
            }
            catch (Exception ex)
            {
                IsCompleted = false;
                CancelButtonVisible = false;
                IsCancelled = true;
                Info = "Task Failed";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.DecompressLosseFiles()", ex);
                return false;
            }
        }

        private async Task<bool> CompressLosseFiles(List<string> filePaths, int alreadySkipped, CancellationTokenSource? cancelSource = null)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    ProgressBarMax = filePaths.Count();
                    ProgressCurrent = 0;
                    ShowProgressText = false;
                    if (cancelSource != null)
                    {
                        cancellationTokenSource = cancelSource;
                    }
                    else
                    {
                        cancellationTokenSource = new CancellationTokenSource();
                    }
                    CancelButtonVisible = false;
                    Name = "Compressing loose files";

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    int skippedCount = alreadySkipped;
                    int compressedCount = 0;

                    Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.CompressLosseFiles()", "Starting to compress loose files" );

                    await Parallel.ForEachAsync(filePaths, new ParallelOptions { MaxDegreeOfParallelism = Knossos.globalSettings.compressionMaxParallelism }, async (file, token) =>
                    {
                        var input = new FileStream(file,FileMode.Open,FileAccess.Read, FileShare.Read);
                        BinaryReader br = new BinaryReader(input);

                        if (!input.CanRead)
                        {
                            input.Dispose();
                            throw new TaskCanceledException();
                        }

                        //Verify if it is compressed
                        if (Encoding.ASCII.GetString(br.ReadBytes(4)) != "LZ41")
                        {
                            FileInfo fi = new FileInfo(file);
                            await Dispatcher.UIThread.InvokeAsync(() => {
                                Info = ProgressCurrent + " / " + ProgressBarMax + " " + fi.Name;
                            });
                            input.Seek(0, SeekOrigin.Begin);
                            var output = new FileStream(fi.FullName+".lz41", FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                            if(!output.CanWrite)
                            {
                                input.Dispose();
                                output.Dispose();
                                throw new TaskCanceledException();
                            }

                            var compressedSize = VPCompression.CompressStream(input,output);
                            output.Dispose();
                            if(compressedSize < input.Length)
                            {
                                //Delete original
                                input.Dispose();
                                output.Dispose();
                                File.Delete(file);
                                compressedCount++;
                            }
                            else
                            {
                                //Roll back
                                input.Dispose();
                                output.Dispose();
                                File.Delete(fi.FullName + ".lz41");
                                skippedCount++;
                            }
                        }
                        else
                        {
                            skippedCount++;
                        }
                        await input.DisposeAsync();
                        ProgressCurrent++;

                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }
                    });
                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    IsCompleted = true;
                    ProgressCurrent = ProgressBarMax;
                    Info = "Compressed: "+compressedCount + " Skipped: "+skippedCount;
                    Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.CompressLosseFiles()", "Compressing Loose files finished: "+ Info);
                    return true;
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
                IsCancelled = true;
                CancelButtonVisible = false;
                Info = "Task Cancelled";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                return false;
            }
            catch (Exception ex)
            {
                IsCompleted = false;
                CancelButtonVisible = false;
                IsCancelled = true;
                Info = "Task Failed";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.CompressLosseFiles()", ex);
                return false;
            }
        }

        private async Task<bool> DecompressVP(FileInfo vpFile, CancellationTokenSource? cancelSource = null)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    ProgressBarMax = 1;
                    ProgressCurrent = 0;
                    ShowProgressText = false;
                    if (cancelSource != null)
                    {
                        cancellationTokenSource = cancelSource;
                    }
                    else
                    {
                        cancellationTokenSource = new CancellationTokenSource();
                    }
                    CancelButtonVisible = false;
                    Name = "Decompressing: " + vpFile.Name;

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.DecompressVP()", "Starting to decompress VP file: " + vpFile.Name);

                    await Task.Run(async () => {
                        var vp = new VPContainer();
                        await vp.LoadVP(vpFile.FullName);
                        vp.DisableCompression();
                        await vp.SaveAsAsync(vpFile.FullName.Replace(".vpc", ".vp", StringComparison.OrdinalIgnoreCase), compressionCallback, cancellationTokenSource);
                    });

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        try
                        {
                            File.Delete(vpFile.FullName.Replace(".vpc", ".vp", StringComparison.OrdinalIgnoreCase));
                            File.Delete(vpFile.FullName.Replace(".vpc", ".vp", StringComparison.OrdinalIgnoreCase) + ".tmp");
                        }
                        catch { }
                        throw new TaskCanceledException();
                    }
                    File.Delete(vpFile.FullName);
                    Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.DecompressVP()", "Decompress VP finished: " + vpFile.Name + " Processed Files: " + ProgressBarMax);
                    Info = "";
                    IsCompleted = true;
                    ProgressCurrent = ProgressBarMax;
                    return true;
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
                IsCancelled = true;
                CancelButtonVisible = false;
                Info = "Task Cancelled";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                return false;
            }
            catch (Exception ex)
            {
                IsCompleted = false;
                CancelButtonVisible = false;
                IsCancelled = true;
                Info = "Task Failed";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.DecompressVP()", ex);
                return false;
            }
        }

        private async Task<bool> CompressVP(FileInfo vpFile, CancellationTokenSource? cancelSource = null)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    ProgressBarMax = 1;
                    ProgressCurrent = 0;
                    ShowProgressText = false;
                    if (cancelSource != null)
                    {
                        cancellationTokenSource = cancelSource;
                    }
                    else
                    {
                        cancellationTokenSource = new CancellationTokenSource();
                    }
                    CancelButtonVisible = false;
                    Name = "Compressing: " + vpFile.Name;

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.CompressVP()", "Starting to compress VP file: " + vpFile.Name);

                    await Task.Run(async() => {
                        var vp = new VPContainer();
                        await vp.LoadVP(vpFile.FullName);
                        vp.EnableCompression();
                        await vp.SaveAsAsync(vpFile.FullName + "c", compressionCallback, cancellationTokenSource);
                    });

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        try
                        {
                            File.Delete(vpFile.FullName + "c");
                            File.Delete(vpFile.FullName + "c.tmp");
                        }
                        catch { }
                        throw new TaskCanceledException();
                    }
                    File.Delete(vpFile.FullName);

                    Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.CompressVP()", "Compress VP finished: " + vpFile.Name + " Processed Files: " + ProgressBarMax );
                    Info = "";
                    IsCompleted = true;
                    ProgressCurrent = ProgressBarMax;
                    return true;
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
                IsCancelled = true;
                CancelButtonVisible = false;
                Info = "Task Cancelled";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                return false;
            }
            catch (Exception ex)
            {
                IsCompleted = false;
                CancelButtonVisible = false;
                IsCancelled = true;
                Info = "Task Failed";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.CompressVP()", ex);
                return false;
            }
        }

        public async Task<bool> CompressMod(Mod mod, CancellationTokenSource? cancelSource = null, bool isSubTask = false)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    if(!isSubTask)
                    {
                        CancelButtonVisible = true;
                        Name = "Compressing mod: " + mod.title + " " + mod.version;
                    }
                    else
                    {
                        Name = "Compressing mod";
                    }
                    
                    ShowProgressText = false;
                    await Dispatcher.UIThread.InvokeAsync(() => {
                        TaskRoot.Add(this);
                    });
                    ProgressBarMin = 0;
                    ProgressCurrent = 0;
                    Info = "In Queue";

                    if (cancelSource != null)
                    {
                        cancellationTokenSource = cancelSource;
                    }
                    else
                    {
                        cancellationTokenSource = new CancellationTokenSource();
                    }

                    //Wait in Queue
                    if (!isSubTask)
                    {
                        while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                        {
                            await Task.Delay(1000);
                            if (cancellationTokenSource.IsCancellationRequested)
                            {
                                throw new TaskCanceledException();
                            }
                        }
                    }

                    Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.CompressMod()", "Starting to compress Mod: " + mod.title);

                    var vpFiles = Directory.GetFiles(mod.fullPath, "*.vp").ToList();
                    ProgressBarMax = vpFiles.Count()+1;

                    //Loose Files Compression
                    if(Directory.Exists(mod.fullPath+Path.DirectorySeparatorChar+"data") || mod.devMode)
                    {
                        var searchDir = mod.devMode ? mod.fullPath : mod.fullPath + Path.DirectorySeparatorChar + "data";
                        var allFilesInDataFolder = Directory.GetFiles(searchDir, "*.*",SearchOption.AllDirectories).ToList();
                        int skipped = 0;
                        //Filter
                        foreach(var fileInData in allFilesInDataFolder.ToList())
                        {
                            var file = new FileInfo(fileInData);

                            if (file.IsReadOnly || file.Length < VPCompression.MinimumSize || VPCompression.ExtensionIgnoreList.Contains(file.Extension.ToLower()) || file.Extension.ToLower() == ".lz41") 
                            { 
                                if(file.Extension.ToLower() == ".vp")
                                {
                                    vpFiles.Add(fileInData);
                                    ProgressBarMax++;
                                }
                                allFilesInDataFolder.Remove(fileInData);
                                skipped++;
                            }
                        }
                        //Process
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            var fileTask = new TaskItemViewModel();
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                TaskList.Insert(0, fileTask);
                            });
                            
                            Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;

                            var result = await fileTask.CompressLosseFiles(allFilesInDataFolder, skipped, cancellationTokenSource);
                            if (cancellationTokenSource.IsCancellationRequested)
                            {
                                throw new TaskCanceledException();
                            }
                        },DispatcherPriority.Background);
                    }
                    ProgressCurrent++;
                    Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;

                    //VP Compression
                    await Parallel.ForEachAsync(vpFiles, new ParallelOptions { MaxDegreeOfParallelism = Knossos.globalSettings.compressionMaxParallelism }, async (file, token) =>
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            var vpTask = new TaskItemViewModel();
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                TaskList.Insert(0, vpTask);
                            });
                            Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;
                            await vpTask.CompressVP(new FileInfo(file), cancellationTokenSource);
                            ProgressCurrent++;
                            Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;
                            if (cancellationTokenSource.IsCancellationRequested)
                            {
                                throw new TaskCanceledException();
                            }
                        },DispatcherPriority.Background);
                    });

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    //Update settings json
                    mod.modSettings.Load(mod.fullPath);
                    mod.modSettings.isCompressed = true;
                    mod.modSettings.Save();

                    IsCompleted = true;
                    ProgressCurrent = ProgressBarMax;
                    Info = string.Empty;
                    CancelButtonVisible = false;

                    if (!isSubTask && TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                    {
                        TaskViewModel.Instance!.taskQueue.Dequeue();
                    }

                    return true;
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (TaskCanceledException)
            {
                Info = "Task Cancelled";
                IsCompleted = false;
                CancelButtonVisible = false;
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                if (!isSubTask)
                {
                    while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                    {
                        await Task.Delay(500);
                    }
                    if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                    {
                        TaskViewModel.Instance!.taskQueue.Dequeue();
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Info = "Task Failed";
                IsCompleted = false;
                CancelButtonVisible = false;
                cancellationTokenSource?.Cancel();
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                if (!isSubTask)
                {
                    while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                    {
                        await Task.Delay(500);
                    }
                    if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                    {
                        TaskViewModel.Instance!.taskQueue.Dequeue();
                    }
                }
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.CompressMod()", ex);
                return false;
            }
        }

        public async Task<bool> DecompressMod(Mod mod, CancellationTokenSource? cancelSource = null)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    CancelButtonVisible = true;
                    Name = "Decompressing mod: " + mod.title + " " + mod.version;
                    ShowProgressText = false;
                    await Dispatcher.UIThread.InvokeAsync(() => TaskRoot.Add(this));
                    ProgressBarMin = 0;
                    ProgressCurrent = 0;
                    Info = "In Queue";

                    if (cancelSource != null)
                    {
                        cancellationTokenSource = cancelSource;
                    }
                    else
                    {
                        cancellationTokenSource = new CancellationTokenSource();
                    }

                    //Wait in Queue
                    while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                    {
                        await Task.Delay(1000);
                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }
                    }

                    Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.DecompressMod()", "Starting to decompress Mod: " + mod.title);

                    var vpcFiles = Directory.GetFiles(mod.fullPath, "*.vpc").ToList();
                    ProgressBarMax = vpcFiles.Count() + 1;

                    //Loose Files Compression
                    if (Directory.Exists(mod.fullPath + Path.DirectorySeparatorChar + "data") || mod.devMode)
                    {
                        var searchDir = mod.devMode ? mod.fullPath : mod.fullPath + Path.DirectorySeparatorChar + "data";
                        var allFilesInDataFolder = Directory.GetFiles(searchDir, "*.*", SearchOption.AllDirectories).ToList();
                        int skipped = 0;
                        //Filter
                        foreach (var fileInData in allFilesInDataFolder.ToList())
                        {
                            var file = new FileInfo(fileInData);

                            if (file.IsReadOnly || file.Extension.ToLower() != ".lz41")
                            {
                                if (file.Extension.ToLower() == ".vpc")
                                {
                                    vpcFiles.Add(fileInData);
                                    ProgressBarMax++;
                                }

                                allFilesInDataFolder.Remove(fileInData);
                                skipped++;
                            }
                        }
                        //Process
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            var fileTask = new TaskItemViewModel();
                            await Dispatcher.UIThread.InvokeAsync(() => { TaskList.Insert(0, fileTask); });
                            Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;

                            var result = await fileTask.DecompressLosseFiles(allFilesInDataFolder, skipped, cancellationTokenSource);
                            if (!result || cancellationTokenSource.IsCancellationRequested)
                            {
                                throw new TaskCanceledException();
                            }
                        },DispatcherPriority.Background);
                    }
                    ProgressCurrent++;
                    Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;

                    //VPC Decompression
                    await Parallel.ForEachAsync(vpcFiles, new ParallelOptions { MaxDegreeOfParallelism = Knossos.globalSettings.compressionMaxParallelism }, async (file, token) =>
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            var vpTask = new TaskItemViewModel();
                            await Dispatcher.UIThread.InvokeAsync(() => { TaskList.Insert(0, vpTask); });
                            Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;
                            await vpTask.DecompressVP(new FileInfo(file), cancellationTokenSource);
                            ProgressCurrent++;
                            Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;
                            if (cancellationTokenSource.IsCancellationRequested)
                            {
                                throw new TaskCanceledException();
                            }
                        },DispatcherPriority.Background);
                    });

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    //Update settings json
                    mod.modSettings.Load(mod.fullPath);
                    mod.modSettings.isCompressed = false;
                    mod.modSettings.Save();

                    IsCompleted = true;
                    ProgressCurrent = ProgressBarMax;
                    Info = string.Empty;
                    CancelButtonVisible = false;

                    if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                    {
                        TaskViewModel.Instance!.taskQueue.Dequeue();
                    }

                    return true;
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (TaskCanceledException)
            {
                Info = "Task Cancelled";
                IsCompleted = false;
                CancelButtonVisible = false;
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                {
                    await Task.Delay(500);
                }
                if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                {
                    TaskViewModel.Instance!.taskQueue.Dequeue();
                }
                return false;
            }
            catch (Exception ex)
            {
                Info = "Task Failed";
                IsCompleted = false;
                CancelButtonVisible = false;
                cancellationTokenSource?.Cancel();
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                {
                    await Task.Delay(500);
                }
                if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                {
                    TaskViewModel.Instance!.taskQueue.Dequeue();
                }
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.CompressMod()", ex);
                return false;
            }
        }

        public async Task<bool> DecompressNebulaFile(string filepath, string? filename, string dest, CancellationTokenSource? cancelSource = null, bool extractFullPath = true)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    CancelButtonVisible = false;
                    Name = "Decompressing " + filename;
                    ShowProgressText = true;
                    ProgressBarMin = 0;
                    ProgressCurrent = 0;
                    ProgressBarMax = 100;
                    if (cancelSource != null)
                    {
                        cancellationTokenSource = cancelSource;
                    }
                    else
                    {
                        cancellationTokenSource = new CancellationTokenSource();
                    }

                    return await KnUtils.DecompressFile(filepath, dest, cancellationTokenSource, extractFullPath, deCompressionCallback);
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch(TaskCanceledException)
            {
                Info = "Task Cancelled";
                IsCompleted = false;
                CancelButtonVisible = false;
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                return false;
            }
            catch(Exception ex)
            {
                Info = "Task Failed";
                IsCompleted = false;
                CancelButtonVisible = false;
                cancellationTokenSource?.Cancel();
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.DecompressTask()", ex);
                return false;
            }
        }

        public async Task<bool> VerifyMod(Mod mod, CancellationTokenSource cancelSource)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    installVersion = mod.version;
                    installID = mod.id;
                    if (cancelSource != null)
                    {
                        cancellationTokenSource = cancelSource;
                    }
                    else
                    {
                        cancellationTokenSource = new CancellationTokenSource();
                    }
                    CancelButtonVisible = true;
                    Name = "Verifying " + mod.ToString();
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

                    ProgressCurrent = 0;
                    ProgressBarMax = 0;
                    foreach (var pkg in mod.packages)
                    {
                        if(pkg.filelist!=null)
                        {
                            ProgressBarMax += pkg.filelist.Count();
                        }
                    }

                    Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.VerifyMod()", "Start verify for :" + mod);
                    mod.ReLoadJson();
                    List<ModPackage> reinstall = new List<ModPackage>();
                    List<string> fileArray = Directory.GetFiles(mod.fullPath, "*.*", SearchOption.AllDirectories).ToList();
                    for (int i = fileArray.Count() - 1; i >= 0; i--)
                    {
                        if (fileArray[i].ToLower().Contains(".json") || fileArray[i].ToLower().Contains(".ini") || mod.tile!= null && fileArray[i].ToLower().Contains(mod.tile) || mod.banner!=null && fileArray[i].ToLower().Contains(mod.banner) || fileArray[i].ToLower().Contains("kn_screen"))
                            fileArray.RemoveAt(i);
                    }
                    foreach (var pkg in mod.packages)
                    {
                        bool pkgPassed = true;
                        if (pkg.filelist != null)
                        {
                            foreach (var file in pkg.filelist)
                            {
                                if (cancellationTokenSource!.IsCancellationRequested)
                                {
                                    throw new TaskCanceledException();
                                }
                                for (int i = fileArray.Count() - 1; i >= 0; i--)
                                {
                                    if (fileArray[i].ToLower().Replace(Path.DirectorySeparatorChar.ToString(),"").Contains(file.filename!.ToLower().Replace(@"./", "").Replace(@"\", "").Replace(@"/", "")))
                                        fileArray.RemoveAt(i);
                                }
                                ProgressCurrent++;
                                Info = "Files: "+ ProgressCurrent + "/"+ ProgressBarMax + " Current File: " + file.filename;
                                //Checksum
                                if (file.checksum != null && file.checksum.Count() > 0)
                                {
                                    if (file.checksum[0].ToLower() == "sha256")
                                    {
                                        try
                                        {
                                            using (FileStream? filehash = new FileStream(mod.fullPath + Path.DirectorySeparatorChar + file.filename, FileMode.Open, FileAccess.Read))
                                            {
                                                using (SHA256 checksum = SHA256.Create())
                                                {
                                                    filehash.Position = 0;
                                                    var hashValue = BitConverter.ToString(await checksum.ComputeHashAsync(filehash)).Replace("-", String.Empty);
                                                    filehash.Close();
                                                    if (hashValue.ToLower() != file.checksum[1].ToLower())
                                                    {
                                                        pkgPassed = false;
                                                        Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.VerifyMod()", file.filename + " failed checksum check! Mod: " + mod);
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            //Filenotfound most likely
                                            pkgPassed = false;
                                            Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.VerifyMod()", ex);
                                        }
                                    }
                                    else
                                    {
                                        Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.VerifyMod()", "Unsupported checksum crypto: " + file.checksum[0]);
                                    }
                                }
                                if(!pkgPassed)
                                {
                                    continue;
                                }
                            }
                        }
                        if (pkgPassed)
                        {
                            Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.VerifyMod()", "Pkg Verify OK: "+ pkg.name +"Mod: " + mod);
                        }
                        else
                        {
                            pkg.isSelected = true;
                            reinstall.Add(pkg);
                            Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.VerifyMod()", "Pkg Verify FAIL: " + pkg.name + "Mod: " + mod);
                        }
                    }

                    if (cancellationTokenSource!.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                    {
                        TaskViewModel.Instance!.taskQueue.Dequeue();
                    }

                    IsCompleted = true;
                    CancelButtonVisible = false;
                    ProgressCurrent = ProgressBarMax;

                    if (!reinstall.Any())
                    {
                        Info = "PASSED";
                        mod.ClearUnusedData();
                    }
                    else
                    {
                        Info = "FAIL";
                        TaskViewModel.Instance?.InstallMod(mod,reinstall);
                    }

                    if(fileArray.Any())
                    {
                        foreach (var file in fileArray)
                        {
                            Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.VerifyMod()", "Unknown file detected during verify: " + file);
                        }
                        Info += " - " + fileArray.Count() + " Unknown files detected, check log or debug console";
                    }

                    return true;
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
                IsCancelled = true;
                CancelButtonVisible = false;
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
                mod.ClearUnusedData();
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                return false;
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
                mod.ClearUnusedData();
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.VerifyMod()", ex);
                return false;
            }
        }

        public async Task<bool> InstallMod(Mod mod, CancellationTokenSource cancelSource, List<ModPackage>? reinstallPkgs = null, bool manualCompress = false, bool cleanupOldVersions = false)
        {
            string? modPath = null;
            Mod? installed = null;
            
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    installVersion = mod.version;
                    installID = mod.id;
                    if (cancelSource != null)
                    {
                        cancellationTokenSource = cancelSource;
                    }
                    else
                    {
                        cancellationTokenSource = new CancellationTokenSource();
                    }
                    CancelButtonVisible = true;
                    Name = "Downloading " + mod.ToString();
                    ShowProgressText = false;
                    await Dispatcher.UIThread.InvokeAsync(() => TaskRoot.Add(this));
                    Info = "In Queue";
                    bool compressMod = false;

                    //Set Mod card as "installing"
                    MainWindowViewModel.Instance?.NebulaModsView.SetInstalling(mod.id, cancellationTokenSource);

                    //Wait in Queue
                    while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                    {
                        await Task.Delay(1000);
                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }
                    }

                    if (!mod.devMode) //Do not compress dev mode mods
                    {
                        compressMod = manualCompress;
                        //Todo add mod fso version checking
                        if (!mod.devMode && Knossos.globalSettings.modCompression == CompressionSettings.Always)
                        {
                            compressMod = true;
                        }
                        if (!mod.devMode && Knossos.globalSettings.modCompression == CompressionSettings.ModSupport)
                        {
                            try
                            {
                                var fso = mod.GetDependency("FSO");
                                if (fso != null && (fso.version == null || SemanticVersion.Compare(fso.version.Replace(">=", "").Replace("<", "").Replace(">", "").Trim(), VPCompression.MinimumFSOVersion) > 0))
                                    compressMod = true;
                            }
                            catch (Exception ex)
                            {
                                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.InstallMod()", ex);
                            }
                        }
                    }

                    /*
                        Check if its installed, even on install task it could have been installed by another task that was in the queue
                    */
                    installed = Knossos.GetInstalledMod(mod.id, mod.version);
                    if (installed != null) 
                    {
                        installed.ReLoadJson();
                        Name = "Modify " + mod.ToString();
                        compressMod = installed.modSettings.isCompressed;
                    }

                    Info = "Starting";

                    /*
                        -Parse all files we need to download
                        -Determine mod folder path and rootpack
                        -Add all "ModFile" to a new list for easy access
                        -Create all folders
                        -Create the download token on the root of the mod.
                        -Set all the data needed here, number of tasks, etc for the progress bar and info
                        -Main progress max value is calculated as follows: ( Number of files to download * 2 ) + 1
                         (Download, Decompression, Download banner/tile images)
                        -+1 task if we have to compress
                        -If the mod is installeds there is no need to download the baners and title image again so -2 to max tasks
                        -If devmode and file is a vp it needs to be decompressed +1 to max tasks
                    */

                    List<ModFile> files = new List<ModFile>();
                    string modFolder = mod.id + "-" + mod.version;
                    string rootPack = string.Empty;
                    if(mod.type == ModType.tc && mod.parent == null)
                    {
                        rootPack = mod.id;
                    }
                    else
                    {
                        if(mod.type == ModType.mod && mod.parent != null)
                        {
                            rootPack = mod.parent;
                        }
                        else
                        {
                            Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.InstallMod()", "Unable to determine mod root pack " + mod.ToString() + " Type: " + mod.type + " Parent: " + mod.parent);
                            throw new TaskCanceledException();
                        }
                    }

                    modPath = Knossos.GetKnossosLibraryPath() + Path.DirectorySeparatorChar +rootPack + Path.DirectorySeparatorChar + modFolder;

                    /* Metadata update */
                    bool metaUpdate = false;
                    if (installed != null && Mod.IsMetaUpdate(mod, installed))
                    {
                        metaUpdate = true;
                        installed.lastUpdate = mod.lastUpdate;
                        installed.firstRelease = mod.firstRelease;
                        installed.releaseThread = mod.releaseThread;
                        installed.title = mod.title;
                        installed.description = mod.description;
                        installed.modFlag = mod.modFlag;
                        installed.videos = mod.videos;
                        foreach (var pkg in installed.packages)
                        {
                            var other = mod.packages.FirstOrDefault(p => p.name == pkg.name);
                            if (other != null)
                            {
                                pkg.dependencies = other.dependencies;
                            }
                        }
                        installed.SaveJson();
                        installed.tile = mod.tile;
                        installed.banner = mod.banner;
                        var msg = new TaskItemViewModel();
                        msg.ShowMsg("Metadata was updated", null);
                        await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, msg));
                    }

                    /* Delete pkgs */
                    if (installed != null)
                    {
                        bool save = false;
                        foreach (var modpkg in mod.packages.ToList())
                        {
                            var installedPkg = installed.packages.FirstOrDefault(p => p.name == modpkg.name);
                            if (modpkg.filelist != null && !modpkg.isSelected && installedPkg != null)
                            {
                                int delCount = 0;
                                var newTask = new TaskItemViewModel();
                                Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.InstallMod(delete mod file)", "Deleting package: " + modpkg.name + " MOD: " + mod);
                                Dispatcher.UIThread.Invoke(() =>
                                {
                                    newTask.ShowMsg("Deleting pkg: " + modpkg.name, null, Brushes.Red);
                                    TaskList.Add(newTask);
                                });
                                foreach (var file in modpkg.filelist)
                                {
                                    try
                                    {
                                        if (File.Exists(installed.fullPath + Path.DirectorySeparatorChar + file.filename))
                                        {
                                            File.Delete(installed.fullPath + Path.DirectorySeparatorChar + file.filename);
                                            delCount++;
                                        }
                                    }
                                    catch(Exception ex)
                                    {
                                        Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.InstallMod(delete mod file)", ex);
                                    }
                                }
                                Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.InstallMod(delete mod file)", "Files deleted: " + delCount);
                                installed.packages.Remove(installedPkg);
                                mod.packages.Remove(modpkg);
                                save = true;
                            }
                        }
                        if(save)
                        {
                            installed.SaveJson();
                        }
                    }

                    int vPExtractionNeeded = 0;

                    for (int i = mod.packages.Count - 1; i >= 0; i--)
                    {
                        bool alreadyInstalled = false;
                        if(installed!=null)
                        {
                            foreach(var pkg in installed.packages)
                            {
                                if(pkg.name == mod.packages[i].name)
                                {
                                    if (reinstallPkgs == null || !reinstallPkgs.Where(re => re.name == mod.packages[i].name).Any())
                                    {
                                        alreadyInstalled = true;
                                        continue;
                                    }
                                }
                            }
                        }
                        
                        if (mod.packages[i].isSelected && !alreadyInstalled || !alreadyInstalled && mod.devMode)
                        {
                            if (mod.packages[i].files != null)
                            {
                                if (mod.devMode)
                                {
                                    foreach (var file in mod.packages[i].files!)
                                    {
                                        file.dest = mod.packages[i].folder + Path.DirectorySeparatorChar + file.dest;
                                        if (mod.packages[i].isVp)
                                            vPExtractionNeeded++;
                                    }
                                }
                                files.AddRange(mod.packages[i].files!);
                            }
                        }
                        else
                        {
                            mod.packages.RemoveAt(i);
                        }
                    }


                    /* Is there is nothing new to install just end the task */
                    if(files.Count == 0 && !metaUpdate)
                    {
                        Info = string.Empty;
                        IsCompleted = true;
                        CancelButtonVisible = false;
                        if (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() == this)
                        {
                            TaskViewModel.Instance!.taskQueue.Dequeue();
                        }
                        return true;
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
                    ProgressCurrent = 0;
                    ProgressBarMax = installed == null ? (files.Count * 2) + 1 : (files.Count * 2);
                    ProgressBarMax += vPExtractionNeeded;
                    if (compressMod)
                    {
                        ProgressBarMax += 1;
                    }
                    Info = "Tasks: 0/" + ProgressBarMax;

                    /* Do not create the token on mod updates */
                    if (installed == null)
                    {
                        try
                        {
                            File.Create(modPath + Path.DirectorySeparatorChar + "knossos_net_download.token").Close();
                        }
                        catch { }
                    }

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
                                            throw new Exception("The downloaded file hash was incorrect, expected: " + file.checksum[1] + " Calculated Hash: " + hashValue);
                                        }
                                    }
                                    fileTask.Info = " Checksum OK!";
                                    await Dispatcher.UIThread.InvokeAsync(() => TaskList.Remove(fileTask));
                                }
                            }
                            else
                            {
                                Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.InstallMod()", "Unsupported checksum crypto, skipping checksum check :" + file.checksum[0]);
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
                            Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.InstallMod()", "Error while decompressing the file " + fileFullPath);
                            CancelTaskCommand();
                        }
                        File.Delete(fileFullPath);
                        ++ProgressCurrent;
                        Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;

                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
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
                        -Compress Mod if we had to
                    */

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    //Download Tile image
                    if (!string.IsNullOrEmpty(mod.tile) && ( installed == null || metaUpdate ) )
                    {
                        Directory.CreateDirectory(modPath + Path.DirectorySeparatorChar + "kn_images");
                        var uri = new Uri(mod.tile);
                        using (var fs = await KnUtils.GetImageStream(mod.tile))
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
                        mod.tile = "kn_images" + Path.DirectorySeparatorChar + Path.GetFileName(uri.LocalPath);
                    }

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    //Download Banner
                    if (!string.IsNullOrEmpty(mod.banner) && (installed == null || metaUpdate))
                    {
                        Directory.CreateDirectory(modPath + Path.DirectorySeparatorChar + "kn_images");
                        Directory.CreateDirectory(modPath + Path.DirectorySeparatorChar + "kn_images");
                        var uri = new Uri(mod.banner);
                        using (var fs = await KnUtils.GetImageStream(mod.banner))
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
                        mod.banner = "kn_images" + Path.DirectorySeparatorChar + Path.GetFileName(uri.LocalPath);
                    }

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    //Download Screenshots
                    if (mod.screenshots != null && mod.screenshots.Any() && installed == null)
                    {
                        Directory.CreateDirectory(modPath + Path.DirectorySeparatorChar + "kn_images");
                        var scList = new List<string>();
                        foreach (var sc in mod.screenshots)
                        {
                            if (cancellationTokenSource.IsCancellationRequested)
                            {
                                throw new TaskCanceledException();
                            }
                            var uri = new Uri(sc);
                            using (var fs = await KnUtils.GetImageStream(sc))
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
                        mod.screenshots = scList.ToArray();
                    }

                    ++ProgressCurrent;
                    Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;
                    mod.fullPath = modPath + Path.DirectorySeparatorChar;
                    mod.folderName = modFolder;
                    mod.installed = true;
                    mod.inNebula = true;

                    if (installed == null)
                    {
                        //mark all pkgs as enabled
                        mod.packages?.ForEach(pkg => pkg.isEnabled = true);
                        mod.SaveJson();
                    }
                    else
                    {
                        installed.ReLoadJson();
                        if (reinstallPkgs == null)
                        {
                            installed.packages.AddRange(mod.packages);
                        }
                        //mark all pkgs as enabled
                        mod.packages?.ForEach(pkg => pkg.isEnabled = true);
                        installed.SaveJson();
                        mod.ClearUnusedData();
                    }

                    //We have to compress?
                    if (compressMod)
                    {
                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }

                        var cpTask = new TaskItemViewModel();
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            TaskList.Insert(0, cpTask);
                        });
                        await cpTask.CompressMod(mod, cancellationTokenSource, true);
                        ProgressCurrent++;
                        Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;
                    }

                    //ExtractVPs if needed
                    if (mod.devMode)
                    {
                        var vpFiles = Directory.GetFiles(modPath, "*.*", SearchOption.AllDirectories).Where(file => Regex.IsMatch(file.ToLower(), @"^.+\.(vp|vpc)$"));
                        await Parallel.ForEachAsync(vpFiles, new ParallelOptions { MaxDegreeOfParallelism = Knossos.globalSettings.maxConcurrentSubtasks }, async (vp, token) =>
                        {
                            var extractTask = new TaskItemViewModel();
                            await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, extractTask));
                            var extractResult = await extractTask.ExtractVP(new FileInfo(vp), cancellationTokenSource);
                            if (!extractResult)
                            {
                                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.InstallMod()", "Error while extracting vp file " + vp);
                            }
                            ++ProgressCurrent;
                            Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;
                        });
                    }

                    try
                    {
                        File.Delete(mod.fullPath + Path.DirectorySeparatorChar + "knossos_net_download.token");
                    }
                    catch {}

                    //Remove Mod card, unmark update available, re-run dependencies checks
                    if (installed == null)
                    {
                        MainWindowViewModel.Instance?.NebulaModsView.RemoveMod(mod.id);
                        Knossos.AddMod(mod);
                        await Dispatcher.UIThread.InvokeAsync(() => MainWindowViewModel.Instance?.AddInstalledMod(mod), DispatcherPriority.Background);
                        //We cant determine if the version we are installing is the newer one at this point, but this will determine if it is newer than anything was was installed previously, what is good enoght
                        var newer = Knossos.GetInstalledModList(mod.id)?.MaxBy(x => new SemanticVersion(x.version));
                        if (newer == mod)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() => MainWindowViewModel.Instance?.MarkAsUpdateAvailable(mod.id, false), DispatcherPriority.Background);
                        }
                        if (mod.devMode)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() => MainWindowViewModel.Instance!.AddDevMod(mod), DispatcherPriority.Background);
                            //Reload version editor if needed
                            DeveloperModsViewModel.Instance?.UpdateVersionManager(mod.id);
                        }
                        MainWindowViewModel.Instance?.RunModStatusChecks();
                    }

                    // Clean old versions
                    if(cleanupOldVersions)
                    {
                        try
                        {
                            var versions = Knossos.GetInstalledModList(mod.id);
                            if (versions != null)
                            {
                                versions.Remove(mod);
                                if (versions.Any())
                                {
                                    foreach (var version in versions.ToList())
                                    {
                                        //Check if it is inferior to the one we just installed
                                        if (SemanticVersion.Compare(mod.version, version.version) >= 1)
                                        {
                                            bool inUse = false;
                                            string inUseMods = "";
                                            foreach (var m in Knossos.GetInstalledModList(null))
                                            {
                                                if (m != null && m.id != mod.id)
                                                {
                                                    var deps = m.GetModDependencyList();
                                                    if (deps != null)
                                                    {
                                                        foreach (var dep in deps)
                                                        {
                                                            var depMod = dep.SelectMod();
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
                                                Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.InstallMod()", "Cleanup: " + version + " is in use by these mods: " + inUseMods + ". Skipping.");
                                            }
                                            else
                                            {
                                                //Safe to delete
                                                Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.InstallMod()", "Cleanup: " + version + " is not in use, deleting...");
                                                var msgtask = new TaskItemViewModel();
                                                msgtask.ShowMsg("Cleanup: Deleting " + version.version, null);
                                                await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, msgtask));
                                                //Remove the mod version from Knossos and physical files
                                                await Task.Run(() => Knossos.RemoveMod(version));
                                                //Remove mod version from UI mod versions list
                                                await Dispatcher.UIThread.InvokeAsync(() => MainWindowViewModel.Instance!.RemoveInstalledModVersion(version));
                                                //If the dev editor is open and loaded this mod id, reset it
                                                await Dispatcher.UIThread.InvokeAsync(() => DeveloperModsViewModel.Instance!.ResetModEditor(mod.id));
                                            }
                                        }
                                        else
                                        {
                                            Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.InstallMod()", "Cleanup: " + version + " is newer than " + mod + ". Skipping.");
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
                        catch(Exception ex)
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

                    Info = string.Empty;
                    IsCompleted = true;
                    CancelButtonVisible = false;

                    return true;
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
                IsCancelled = true;
                CancelButtonVisible = false;
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
                    /* If a error ocurred while updating do not delete the whole mod */
                    if (modPath != null && installed == null)
                    {
                        Directory.Delete(modPath, true);
                    }
                }
                catch { }
                await Dispatcher.UIThread.InvokeAsync(() => MainWindowViewModel.Instance?.CancelModInstall(mod.id), DispatcherPriority.Background);
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                return false;
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
                    await MessageBox.Show(MainWindow.instance!, "An error was ocurred during the download of the mod: " + mod.ToString() + ". Error: " + ex.Message, "Error", MessageBox.MessageBoxButtons.OK);
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
                    /* If a error ocurred while updating do not delete the whole mod */
                    if (modPath != null && installed == null)
                    {
                        Directory.Delete(modPath, true);
                    }
                }
                catch { }
                await Dispatcher.UIThread.InvokeAsync(() => MainWindowViewModel.Instance?.CancelModInstall(mod.id), DispatcherPriority.Background);
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.InstallMod()", ex);
                return false;
            }
        }

        public async Task<FsoBuild?> InstallBuild(FsoBuild build, FsoBuildItemViewModel sender, CancellationTokenSource? cancelSource = null, Mod? modJson = null, List<ModPackage>? modifyPkgs = null)
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
                    if(modifyPkgs != null)
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
                        modPath = Knossos.GetKnossosLibraryPath() + Path.DirectorySeparatorChar + "bin"+ Path.DirectorySeparatorChar + modFolder;
                        if(modifyPkgs != null)
                        {
                            //Modify Build
                            foreach(var pkg in modifyPkgs)
                            {
                                var installedPkg = modJson.packages.FirstOrDefault(p => p.name == pkg.name && p.folder == pkg.folder);

                                if(installedPkg != null && !pkg.isSelected)
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
                                    catch(Exception ex)
                                    {
                                        Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.InstallBuild()", ex);
                                    }
                                    modJson.packages.Remove(installedPkg);
                                    continue;
                                }

                                if(installedPkg == null && pkg.isSelected)
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
                                    if(bestExec != null)
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
                            if(bestpkg != null && bestpkg.files != null)
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
                            using (var fs = await KnUtils.GetImageStream(modJson.tile))
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
                            using (var fs = await KnUtils.GetImageStream(modJson.banner))
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
                                using (var fs = await KnUtils.GetImageStream(sc))
                                {
                                    var scTask = new TaskItemViewModel();
                                    await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, scTask));
                                    scTask.ShowMsg("Getting screenshot #"+ scList.Count() + " image", null);
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
                            Knossos.AddBuild(newBuild);
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
                        if(!Knossos.flagDataLoaded)
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
                while(TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
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

        public async Task<bool?> DownloadFile(string url, string dest, string msg, bool showStopButton, string? tooltip, CancellationTokenSource? cancelSource = null)
        {
            string[] mirrors = { url };
            return await DownloadFile(mirrors, dest, msg, showStopButton, tooltip, cancelSource);
        }

        public async Task<bool?> DownloadFile(string[] mirrors, string dest, string msg, bool showStopButton, string? tooltip, CancellationTokenSource? cancelSource = null)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    ProgressBarMax = 100;
                    ProgressCurrent = 0;
                    IsFileDownloadTask = true;
                    if (cancelSource != null)
                    {
                        cancellationTokenSource = cancelSource;
                    }
                    else
                    {
                        cancellationTokenSource = new CancellationTokenSource();
                    }
                    CancelButtonVisible = showStopButton;
                    Name = msg;
                    if (tooltip != null)
                    {
                        Tooltip = tooltip.Trim();
                        TooltipVisible = true;
                    }

                    var downloadProgress = (long? filesize, long bytesDownloaded, string speed, double? progressPercentage) =>
                    {
                        if (progressPercentage.HasValue && filesize.HasValue)
                        {
                            ProgressCurrent = (float)progressPercentage.Value;
                            Info = KnUtils.FormatBytes(bytesDownloaded) + " / " + KnUtils.FormatBytes(filesize.Value) + " @ " + speed ;
                        }

                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }
                        return restartDownload;
                    };

                    bool result = false;

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    result = await Download(mirrors, dest, downloadProgress);

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }
                    CancelButtonVisible = false;
                    if (result)
                    {
                        IsCompleted = true;
                        return true;
                    }
                    else
                    {
                        IsCompleted = false;
                        return false;
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
                IsCancelled = true;
                CancelButtonVisible = false;
                Info = "Task Cancelled";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                return false;
            }
            catch (Exception ex)
            {
                IsCompleted = false;
                CancelButtonVisible = false;
                IsCancelled = true;
                Info = "Task Failed";
                //Only dispose the token if it was created locally
                if (cancelSource == null)
                {
                    cancellationTokenSource?.Dispose();
                }
                Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.DownloadFile()", ex);
                return false;
            }
        }

        private async Task<bool> Download(string[] downloadMirrors, string destinationFilePath, Func<long?, long, string, double?, bool> progressChanged)
        {
            Random rnd = new Random();
            int maxRetries = 15;
            int count = 0;
            bool result = false;
            IsFileDownloadTask = true;
            int lastMirrorIndex = -1;
            do
            {
                if(restartDownload)
                {
                    restartDownload = false;
                }
                else
                {
                    count++;
                }
                var mirrorIndex = rnd.Next(downloadMirrors.Count());
                Uri uri = new Uri(downloadMirrors[mirrorIndex]);
                
                while (downloadMirrors.Count() > 1 && ( (mirrorIndex == lastMirrorIndex && (Knossos.globalSettings.mirrorBlacklist == null || downloadMirrors.Count() - Knossos.globalSettings.mirrorBlacklist.Count() > 1 )) || (Knossos.globalSettings.mirrorBlacklist != null && Knossos.globalSettings.mirrorBlacklist.Contains(uri.Host)) ) )
                {
                    mirrorIndex = rnd.Next(downloadMirrors.Count());
                    uri = new Uri(downloadMirrors[mirrorIndex]);
                }

                CurrentMirror = uri.Host;
                lastMirrorIndex = mirrorIndex;

                if (count > 1)
                {
                    Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.Download(List<mirrors>)", "Retrying download of file: " + uri.ToString());
                }
                result = await Download(uri, destinationFilePath, progressChanged);
                if (cancellationTokenSource!.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
            } while (result != true && count < maxRetries);
            
            return result;
        }

        private async Task<bool> Download(Uri downloadUrl, string destinationFilePath, Func<long?, long, string, double?, bool> progressChanged)
        {
            try
            {
                bool isJson = false;
                Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.Download()", "Downloading file: " + downloadUrl);
                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

                var httpClient = KnUtils.GetHttpClient();
                if (downloadUrl.ToString().ToLower().Contains(".json"))
                {
                    httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "br, gzip, deflate");
                    isJson = true;
                }
                using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;

                if (!totalBytes.HasValue)
                {
                    foreach (string s in response.Headers.Vary)
                    {
                        if (s == "Accept-Encoding")
                        {
                            var c = KnUtils.GetHttpClient();
                            c.Timeout = TimeSpan.FromSeconds(30);
                            var r = await c.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                            totalBytes = r.Content.Headers.ContentLength;
                            r.Dispose(); c.Dispose();
                            continue;
                        }
                    }
                }

                using var contentStream = Knossos.globalSettings.maxDownloadSpeed > 0 && !isJson ? new ThrottledStream(response.Content.ReadAsStream(), Knossos.globalSettings.maxDownloadSpeed) : response.Content.ReadAsStream();
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
                    while(pauseDownload && !restartDownload)
                    {
                        await Task.Delay(500);
                        if (cancellationTokenSource!.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }
                    }

                    if (cancellationTokenSource!.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

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
                        speed = KnUtils.FormatBytes(totalBytesPerSecond)+"/s";
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

        /* Progress Callbacks */
        private async void multiuploaderCallback(string text, int currentPart, int maxParts)
        {
            await Dispatcher.UIThread.InvokeAsync(() => {
                try
                {
                    ProgressBarMax = maxParts;
                    ProgressCurrent = currentPart;
                    Info = text;
                }
                catch { }
            });
        }

        private async void sevenZipCallback(int percentage)
        {
            await Dispatcher.UIThread.InvokeAsync(() => {
                try
                {
                    ProgressCurrent = percentage;
                }
                catch { }
            });
        }

        private async void copyCallback(string filename)
        {
            if (filename != string.Empty)
            {
                ProgressCurrent++;
            }
            else
            {
                ProgressCurrent = 0;
            }
            await Dispatcher.UIThread.InvokeAsync(() => {
                try
                {
                    Info = ProgressCurrent.ToString() + " / " + ProgressBarMax.ToString() + "  -  " + filename;
                }
                catch { }
            });
        }

        private async void compressionCallback(string filename, int maxFiles)
        {
            if (filename != string.Empty)
            {
                ProgressCurrent++;
            }
            else
            {
                ProgressCurrent = 0;
            }
            await Dispatcher.UIThread.InvokeAsync(() => {
                try
                {
                    ProgressBarMax = maxFiles;
                    Info = ProgressCurrent.ToString() + " / " + ProgressBarMax.ToString() + "  -  " + filename;
                }
                catch { }
            });
        }

        private async void extractCallback(string filename, int _, int maxFiles)
        {
            if (filename != string.Empty)
            {
                ProgressCurrent++;
            }
            else
            {
                ProgressCurrent = 0;
            }
            await Dispatcher.UIThread.InvokeAsync(() => {
                try
                {
                    ProgressBarMax = maxFiles;
                    Info = ProgressCurrent.ToString() + " / " + ProgressBarMax.ToString() + "  -  " + filename;
                }
                catch { }
            });
        }

        private void deCompressionCallback(int progress)
        {
            Dispatcher.UIThread.InvokeAsync(() => {
                ProgressCurrent = progress;
            });
        }

        /* Button Commands */
        public void CancelTaskCommand()
        {
            if (!IsCompleted)
            {
                cancellationTokenSource?.Cancel();
                IsCancelled = true;
            }
        }

        internal void PauseDownloadCommand()
        {
            pauseDownload = !pauseDownload;
            if (pauseDownload)
                PauseButtonText = "Resume";
            else
                PauseButtonText = "Pause";
        }

        internal void RestartDownloadCommand()
        {
            restartDownload = true;
            if (pauseDownload)
                PauseDownloadCommand();
        }
    }
}
