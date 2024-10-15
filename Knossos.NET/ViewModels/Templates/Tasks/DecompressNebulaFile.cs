using System;
using System.Threading.Tasks;
using System.Threading;

namespace Knossos.NET.ViewModels
{
    public partial class TaskItemViewModel : ViewModelBase
    {
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
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.DecompressTask()", ex);
                return false;
            }
        }
    }
}
