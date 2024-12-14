using Knossos.NET.Models;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace Knossos.NET.ViewModels
{
    public partial class TaskItemViewModel : ViewModelBase
    {
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
                    if (result == null || (result != "ok" && result.ToLower() != "duplicated version"))
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
    }
}
