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
                            var output = new FileStream(fi.FullName.Replace(".lz41", string.Empty, StringComparison.OrdinalIgnoreCase), FileMode.Create, FileAccess.ReadWrite, FileShare.None);
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
    }
}
