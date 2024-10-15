using System;
using System.Threading.Tasks;
using System.Threading;
using Knossos.NET.Classes;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace Knossos.NET.ViewModels
{
    public partial class TaskItemViewModel : ViewModelBase
    {
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
                            Info = KnUtils.FormatBytes(bytesDownloaded) + " / " + KnUtils.FormatBytes(filesize.Value) + " @ " + speed;
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
                if (restartDownload)
                {
                    restartDownload = false;
                }
                else
                {
                    count++;
                }
                var mirrorIndex = rnd.Next(downloadMirrors.Count());
                Uri uri = new Uri(downloadMirrors[mirrorIndex]);

                while (downloadMirrors.Count() > 1 && ((mirrorIndex == lastMirrorIndex && (Knossos.globalSettings.mirrorBlacklist == null || downloadMirrors.Count() - Knossos.globalSettings.mirrorBlacklist.Count() > 1)) || (Knossos.globalSettings.mirrorBlacklist != null && Knossos.globalSettings.mirrorBlacklist.Contains(uri.Host))))
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
                    while (pauseDownload && !restartDownload)
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
                        speed = KnUtils.FormatBytes(totalBytesPerSecond) + "/s";
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
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.Download", ex);
                return false;
            }
        }
    }
}
