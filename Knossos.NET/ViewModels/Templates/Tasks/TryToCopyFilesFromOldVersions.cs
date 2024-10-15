using Knossos.NET.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Knossos.NET.Classes;
using System.Text;
using Avalonia.Media;

namespace Knossos.NET.ViewModels
{
    public partial class TaskItemViewModel : ViewModelBase
    {
        /// <summary>
        /// Try to get and copy files for this package from old versions of this mod only if matching files are found
        /// It will use the nebula file sha256 hash to look for others versions with that same nebula file
        /// Then it will check each files included in that 7z to see if they are all present and the sha256
        /// still matches what we expect it to be.
        /// Note: sha256 check will be skipped for compressed files, as it would not match.
        /// Only if all files are present and the sha256 matches for all files of the package that the files will be copied
        /// It will only pick other versions that have the same compression status to the one we are installing, so
        /// if we are installing a mod whiout compressing it, all other compressed versions of this mod will be ignored.
        /// 
        /// If useHardlinks is true, it will try to hardlink files, that would create "copies" of the files whiout increasing
        /// disk usage, if it fails it will revert to copy files for this entire package at least.
        /// 
        /// Hardlinks failing could be caused by user not having permissions or the filesystem not supporting hardlinks (FAT32, EXFAT).
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="oldVersions"></param>
        /// <param name="file"></param>
        /// <param name="package"></param>
        /// <param name="compressMod"></param>
        /// <param name="useHardlinks"></param>
        /// <param name="cancelSource"></param>
        /// <returns>true if sucessfully copied all files for this package from an old version, false otherwise</returns>
        public async Task<bool> TryToCopyFilesFromOldVersions(Mod mod, List<Mod> oldVersions, ModFile file, ModPackage? package, bool compressMod, bool useHardlinks, CancellationTokenSource? cancelSource = null)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    ProgressBarMax = 1;
                    ProgressCurrent = 0;
                    ShowProgressText = false;
                    CancelButtonVisible = false;
                    IsTextTask = false;
                    IsFileDownloadTask = false;
                    TextColor = Brushes.White;
                    Name = "Get " + file.filename + " from installed versions";

                    var fileHash = "";

                    if (file.checksum != null && file.checksum.Count() >= 1 && file.checksum[0].ToLower() == "sha256")
                    {
                        fileHash = file.checksum[1].ToLower();
                    }
                    else
                    {
                        Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.TryToCopyFilesFromOldVersions()", "Unable to get the sha256 checksum for file " + file.filename + " for mod " + mod + ". This should not happen.");
                        Name = "Unable to parse checksum";
                        return false;
                    }

                    if (!oldVersions.Any())
                    {
                        Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.TryToCopyFilesFromOldVersions()", "No old versions for mod " + mod + ". This should not happen.");
                        Name = "No old versions";
                        return false;
                    }
                    if (package == null)
                    {
                        Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.TryToCopyFilesFromOldVersions()", "Requested package was null for file " + file.filename + " for mod " + mod + ". This should not happen.");
                        Name = "Aborted, not package was found";
                        return false;
                    }
                    if (package.filelist == null)
                    {
                        Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.TryToCopyFilesFromOldVersions()", "Requested package had a null filelist for file " + file.filename + " for mod " + mod + ". This should not happen.");
                        Name = "Aborted, package had no files";
                        return false;
                    }

                    if (cancelSource != null)
                        cancellationTokenSource = cancelSource;
                    else
                        cancellationTokenSource = new CancellationTokenSource();

                    if (cancellationTokenSource.IsCancellationRequested)
                        throw new TaskCanceledException();

                    //Lets try all old versions
                    foreach (var oldVer in oldVersions)
                    {
                        Info = "Checking on version: " + oldVer.version;
                        //Compression rule: Only select mods with the same compression status to the one we want to install
                        if (oldVer.packages != null && oldVer.packages.Any() && compressMod == oldVer.modSettings.isCompressed)
                        {
                            //Get the old package of the old version if the source nebula file has the same sha256 hash
                            var oldPkg = oldVer.packages.FirstOrDefault(p => p.files != null && p.files.FirstOrDefault(f => f.checksum != null && f.checksum.Contains(fileHash)) != null);

                            //If we found a package then we have to verify that all files exist and their checksum is ok
                            if (oldPkg != null)
                            {
                                Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.TryToCopyFilesFromOldVersions()", "Found match of old files belonging to mod version " + oldVer + " for the requested nebula file " + file.filename + ". Checking individual files...");
                                //store all old file path and new dest paths to copy later
                                var copySrcList = new List<string>();
                                var copyDstList = new List<string>();
                                foreach (var f in package.filelist)
                                {
                                    if (f.filename != null && (!oldVer.devMode || (oldVer.devMode && oldPkg.folder != null)))
                                    {
                                        var oldPath = oldVer.devMode ? Path.Combine(oldVer.fullPath, oldPkg.folder!, f.filename) : Path.Combine(oldVer.fullPath, f.filename);
                                        if (File.Exists(oldPath))
                                        {
                                            var isCompressed = false;
                                            //Check if local file is compressed
                                            using (var input = new FileStream(oldPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                                            {
                                                BinaryReader br = new BinaryReader(input);

                                                //Verify if it is compressed
                                                if (input.CanRead && Encoding.ASCII.GetString(br.ReadBytes(4)) == "LZ41")
                                                    isCompressed = true;
                                            }

                                            //Check sha256 only if not compressed
                                            if (!isCompressed)
                                            {
                                                var oldHash = await KnUtils.GetFileHash(oldPath);
                                                if (f.checksum != null && f.checksum.Count() >= 1 && oldHash != f.checksum[1])
                                                {
                                                    Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.TryToCopyFilesFromOldVersions()", "Requested file " + oldPath + " had a sha256 hash: " + oldHash + " . Expected: " + f.checksum[1]);
                                                    //Verify fail, clear list and break to next modversion
                                                    copySrcList.Clear();
                                                    copyDstList.Clear();
                                                    break;
                                                }
                                            }

                                            copySrcList.Add(oldPath);
                                            var newPath = mod.devMode ? Path.Combine(mod.fullPath, package.folder!, f.filename) : Path.Combine(mod.fullPath, f.filename);
                                            copyDstList.Add(newPath);
                                        }
                                        else
                                        {
                                            Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.TryToCopyFilesFromOldVersions()", "Requested file " + oldPath + " was not present on the old mod files. " + oldVer + " cant be used as source of files for " + file.filename);
                                            //Verify fail, clear list and break to next modversion
                                            copySrcList.Clear();
                                            copyDstList.Clear();
                                            break;
                                        }
                                    }
                                    if (cancellationTokenSource.IsCancellationRequested)
                                        throw new TaskCanceledException();
                                }

                                //we can know if the verify completed by checking the copySrcList
                                if (copySrcList.Any() && copySrcList.Count() == copyDstList.Count())
                                {
                                    ProgressBarMax = copySrcList.Count();
                                    //Copy files
                                    for (int i = 0; i < copySrcList.Count(); i++)
                                    {
                                        //Make sure the dest folder structure exist
                                        Directory.CreateDirectory(Path.GetDirectoryName(copyDstList[i])!);
                                        //First lets try to hardlink, if one fails or it is disabled, revert to copy files
                                        if (useHardlinks)
                                        {
                                            Info = "Hardlink file: " + (i + 1).ToString() + " / " + copySrcList.Count() + " (" + Path.GetFileName(copyDstList[i]) + ")";
                                            useHardlinks = HardLink.CreateFileLink(copySrcList[i], copyDstList[i]);
                                        }
                                        if (!useHardlinks)
                                        {
                                            Info = "Copy file: " + (i + 1).ToString() + " / " + copySrcList.Count() + " (" + Path.GetFileName(copyDstList[i]) + ")";
                                            File.Copy(copySrcList[i], copyDstList[i], true);
                                        }
                                        ++ProgressCurrent;
                                        if (cancellationTokenSource.IsCancellationRequested)
                                            throw new TaskCanceledException();
                                    }
                                    //If we get here without any exceptions it means it completed successfully
                                    Log.Add(Log.LogSeverity.Information, "TaskItemViewModel.TryToCopyFilesFromOldVersions()", "All files needed for nebula file " + file.filename + " were copied from " + oldVer + ". Download from nebula was skipped successfully.");
                                    if (!useHardlinks)
                                    {
                                        Info = copySrcList.Count() + " files copied OK";
                                    }
                                    else
                                    {
                                        Info = copySrcList.Count() + " files hardlinked OK";
                                    }
                                    //IsCompleted = true;
                                    ProgressCurrent = ProgressBarMax;
                                    return true;
                                }
                            }
                        }
                    }

                    Info = "Not found: Downloading new file";
                    ProgressCurrent = ProgressBarMax;
                    return false;
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
                Info = "Cancelled";
                return false;
            }
            catch (Exception ex)
            {
                //An exception has happened during task run
                IsCompleted = false;
                CancelButtonVisible = false;
                IsCancelled = true;
                Info = "Aborted, check log";
                Log.Add(Log.LogSeverity.Error, "TaskItemViewModel.TryToCopyFilesFromOldVersions()", ex);
                return false;
            }
        }
    }
}
