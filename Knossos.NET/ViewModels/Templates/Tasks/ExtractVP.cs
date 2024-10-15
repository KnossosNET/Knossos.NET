using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using VP.NET;

namespace Knossos.NET.ViewModels
{
    public partial class TaskItemViewModel : ViewModelBase
    {
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
                        await vp.ExtractVpAsync(vpFile.Directory!.FullName, extractCallback);
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
    }
}
