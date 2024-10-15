using Knossos.NET.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Knossos.NET.ViewModels
{
    public partial class TaskItemViewModel : ViewModelBase
    {
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
    }
}
