using Knossos.NET.Models;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace Knossos.NET.ViewModels
{
    public partial class TaskItemViewModel : ViewModelBase
    {
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
    }
}
