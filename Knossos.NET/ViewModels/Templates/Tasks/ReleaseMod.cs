using Knossos.NET.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Knossos.NET.ViewModels
{
    public partial class TaskItemViewModel : ViewModelBase
    {
        private async Task<string> ReleaseMod(Mod mod, bool metaUpdate, CancellationTokenSource? cancelSource = null)
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

                    if (cleanMod.packages != null && cleanMod.packages.Any())
                    {
                        foreach (var pkg in cleanMod.packages)
                        {
                            if (pkg.dependencies == null)
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
    }
}
