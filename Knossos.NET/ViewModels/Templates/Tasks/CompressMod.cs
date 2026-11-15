using Avalonia.Threading;
using Knossos.NET.Models;
using Knossos.NET.Classes;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using VP.NET;

namespace Knossos.NET.ViewModels
{
    public partial class TaskItemViewModel : ViewModelBase
    {
        public async Task<bool> CompressMod(Mod mod, CancellationTokenSource? cancelSource = null, bool isSubTask = false, bool onlyIfNeeded = false)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    if (!isSubTask)
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

                    if (!isSubTask)
                    {
                        while (TaskViewModel.Instance!.taskQueue.Count > 0 && TaskViewModel.Instance!.taskQueue.Peek() != this)
                            await Task.Delay(1000, cancellationTokenSource.Token);
                        cancellationTokenSource.Token.ThrowIfCancellationRequested();
                        mod = Knossos.GetInstalledMod(mod.id, mod.version) ?? mod;
                        mod.modSettings.Load(mod.fullPath);
                        if (onlyIfNeeded && !await Task.Run(() => InstalledModFile.NeedsCompressionChange(mod, true, cancellationTokenSource.Token), cancellationTokenSource.Token))
                        {
                            Info = "Already compressed";
                            IsCompleted = true;
                            return true;
                        }
                    }

                    Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.CompressMod()", "Starting to compress Mod: " + mod.title);

                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    var vpFiles = Directory.EnumerateFiles(mod.fullPath, "*.vp", new EnumerationOptions { AttributesToSkip = FileAttributes.ReparsePoint, MatchCasing = MatchCasing.CaseInsensitive }).ToList();
                    ProgressBarMax = vpFiles.Count() + 1;

                    //Loose Files Compression
                    if (Directory.Exists(mod.fullPath + Path.DirectorySeparatorChar + "data") || mod.devMode)
                    {
                        var searchDir = mod.devMode ? mod.fullPath : mod.fullPath + Path.DirectorySeparatorChar + "data";
                        var allFilesInDataFolder = Directory.EnumerateFiles(searchDir, "*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint }).ToList();
                        int skipped = 0;
                        //Filter
                        foreach (var fileInData in allFilesInDataFolder.ToList())
                        {
                            var file = new FileInfo(fileInData);

                            if (file.IsReadOnly || file.Length < VPCompression.MinimumSize || VPCompression.ExtensionIgnoreList.Contains(file.Extension.ToLower()) || file.Extension.ToLower() == ".lz41")
                            {
                                if (!file.IsReadOnly && file.Extension.Equals(".vp", StringComparison.OrdinalIgnoreCase))
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
                            cancellationTokenSource.Token.ThrowIfCancellationRequested();
                            if (!result) throw new IOException("Could not convert all loose files.");
                        }, DispatcherPriority.Background);
                    }
                    ProgressCurrent++;
                    Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;

                    foreach (var file in vpFiles)
                    {
                        cancellationTokenSource.Token.ThrowIfCancellationRequested();
                        var vpTask = new TaskItemViewModel();
                        await Dispatcher.UIThread.InvokeAsync(() => TaskList.Insert(0, vpTask));
                        var result = await vpTask.CompressVP(new FileInfo(file), cancellationTokenSource);
                        cancellationTokenSource.Token.ThrowIfCancellationRequested();
                        if (!result) throw new IOException("Could not convert " + Path.GetFileName(file));
                        ProgressCurrent++;
                        Info = "Tasks: " + ProgressCurrent + "/" + ProgressBarMax;
                    }

                    cancellationTokenSource.Token.ThrowIfCancellationRequested();

                    //Update settings json
                    mod.modSettings.Load(mod.fullPath);
                    mod.modSettings.isCompressed = true;
                    mod.modSettings.Save();

                    IsCompleted = true;
                    ProgressCurrent = ProgressBarMax;
                    Info = string.Empty;
                    CancelButtonVisible = false;

                    return true;
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (OperationCanceledException)
            {
                Info = "Task Cancelled";
                IsCancelled = true;
                return false;
            }
            catch (Exception ex)
            {
                Info = ex.Message;
                IsCancelled = true;
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.CompressMod()", ex);
                return false;
            }
            finally
            {
                CancelButtonVisible = false;
                if (cancelSource == null) cancellationTokenSource?.Dispose();
                if (!isSubTask) TaskViewModel.Instance?.CompleteQueuedTask(this);
            }
        }
    }
}
