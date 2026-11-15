using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using VP.NET;
using System.Text;

namespace Knossos.NET.ViewModels
{
    public partial class TaskItemViewModel : ViewModelBase
    {
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

                    await Parallel.ForEachAsync(filePaths, new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Knossos.globalSettings.compressionMaxParallelism,
                        CancellationToken = cancellationTokenSource.Token
                    }, (file, token) =>
                    {
                        Info = "Processing " + Path.GetFileName(file);
                        var destination = file[..^".lz41".Length];
                        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
                        try
                        {
                            using (var input = File.OpenRead(file))
                            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                                VPCompression.DecompressStream(input, output, CompressionHeader.LZ41, cancellationToken: token);

                            token.ThrowIfCancellationRequested();
                            File.Move(temporary, destination, overwrite: true);
                            File.Delete(file);
                            Interlocked.Increment(ref decompressedCount);
                        }
                        finally { File.Delete(temporary); }
                        ProgressCurrent++;
                        return ValueTask.CompletedTask;
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
            catch (OperationCanceledException)
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
    }
}
