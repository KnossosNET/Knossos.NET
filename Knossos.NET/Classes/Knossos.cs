using Avalonia.Threading;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.ViewModels;
using Knossos.NET.Views;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Avalonia.Platform;
using Avalonia;
using VP.NET;
using SharpCompress.Archives;
using SharpCompress.Readers;
using SharpCompress.Common;

namespace Knossos.NET
{
    public static class Knossos
    {
        public static readonly string AppVersion = "0.0.10";
        private static List<Mod> installedMods = new List<Mod>();
        private static List<FsoBuild> engineBuilds = new List<FsoBuild>();
        public static GlobalSettings globalSettings = new GlobalSettings();
        public static bool retailFs2RootFound = false;
        public static bool flagDataLoaded = false;
        private static object? ttsObject = null;

        public static async void StartUp(bool isQuickLaunch)
        {
            try
            {
                //See if the data folder works
                try
                {
                    if (!Directory.Exists(SysInfo.GetKnossosDataFolderPath()))
                    {
                        Directory.CreateDirectory(SysInfo.GetKnossosDataFolderPath());
                    }
                    else
                    {
                        // Test if we can write to the data directory
                        using (StreamWriter writer = new StreamWriter(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "test.txt"))
                        {
                            writer.WriteLine("test");
                        }
                        File.Delete(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "test.txt");
                    }
                }
                catch (Exception ex)
                {
                    Log.Add(Log.LogSeverity.Error, "Knossos.StartUp()", ex);
                    if (MainWindow.instance != null)
                    {
                        await MessageBox.Show(MainWindow.instance, "Unable to write to KnossosNET data folder.", "KnossosNET Error", MessageBox.MessageBoxButtons.OK);
                    }
                }

                Log.Add(Log.LogSeverity.Information, "Knossos.StartUp()", "=== KnossosNET Start ===");

                //Load language files
                Lang.LoadFiles();

                //Load knossos config
                globalSettings.Load();

                //Check for updates
                if (globalSettings.checkUpdate && !isQuickLaunch)
                {
                    await CheckKnetUpdates();
                    await Task.Delay(300);
                    CleanUpdateFiles();
                }

                //Load base path from knossos legacy
                if (globalSettings.basePath == null)
                {
                    globalSettings.basePath = SysInfo.GetBasePathFromKnossosLegacy();
                }

                LoadBasePath(isQuickLaunch);

                if (globalSettings.basePath == null && !isQuickLaunch)
                    OpenQuickSetup();

            }catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Knossos.StartUp", ex);
            }
        }

        private static async Task QuickLaunch()
        {
            string[] args = Environment.GetCommandLineArgs();
            string modid = string.Empty;
            string modver = string.Empty;
            bool saveID = false;
            bool saveVer = false;

            foreach (var arg in args)
            {
                Log.Add(Log.LogSeverity.Information, "Knossos.StartUp", "Knet Cmdline Arg: " + arg);
                if (saveID)
                {
                    saveID = false;
                    modid = arg;
                }
                if (saveVer)
                {
                    saveVer = false;
                    modver = arg;
                }
                if (arg.ToLower() == "-playmod")
                {
                    saveID = true;
                }
                if (arg.ToLower() == "-version")
                {
                    saveVer = true;
                }
            }

            if(modid != string.Empty)
            {
                if(modver != string.Empty)
                {
                    var mod = GetInstalledMod(modid, modver);
                    if(mod != null)
                    {
                        PlayMod(mod, FsoExecType.Release);
                    }
                    else
                    {
                        Log.Add(Log.LogSeverity.Error, "Knossos.QuickLaunch", "Quick launch was used but the modid and modver was not found. Used modid: "+modid + " modver: "+modver);
                    }
                }
                else
                {
                    var modlist = GetInstalledModList(modid);
                    if(modlist != null && modlist.Count() > 0)
                    {
                        Mod? mod = null;
                        if (modlist.Count() > 1)
                        {
                            foreach (var m in modlist)
                            {
                                if (mod == null || SemanticVersion.Compare(m.version, mod.version) > 0)
                                {
                                    mod = m;
                                }
                            }
                        }
                        else
                        {
                            mod = modlist[0];
                        }

                        if(mod != null)
                        {
                            PlayMod(mod, FsoExecType.Release);
                        }
                    }
                    else
                    {
                        Log.Add(Log.LogSeverity.Error, "Knossos.QuickLaunch", "Quick launch was used but the modid was not found. Used modid: " + modid);
                    }
                }
            }
            else
            {
                Log.Add(Log.LogSeverity.Error, "Knossos.QuickLaunch", "Quick launch was used but the modid was not detected.");
            }
            await Task.Delay(2000);
            MainWindow.instance!.Close();
        }

        private static void CleanUpdateFiles()
        {
            try
            {
                if(File.Exists(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "update.zip"))
                    File.Delete(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "update.zip");
            }
            catch { }
            try
            {
                var appDirPath = System.AppDomain.CurrentDomain.BaseDirectory;
                var oldFiles = System.IO.Directory.GetFiles(appDirPath, "*.old");
                foreach (string f in oldFiles)
                {
                    File.Delete(f);
                }
            }
            catch { }
        }

        private static async Task CheckKnetUpdates()
        {
            try
            {
                var latest = await GitHubApi.GetLastRelease();
                if (latest != null && latest.tag_name != null)
                {
                    if (SemanticVersion.Compare(AppVersion, latest.tag_name.ToLower().Replace("v", "").Trim()) <= -1)
                    {
                        if (latest.assets != null)
                        {
                            GitHubReleaseAsset? releaseAsset = null;
                            foreach (GitHubReleaseAsset a in latest.assets)
                            {
                                if(a.name != null && ( a.name.ToLower().Contains(".zip") || a.name.ToLower().Contains(".7z")))
                                {
                                    if(a.name.ToLower().Contains(SysInfo.GetOSNameString().ToLower()))
                                    {
                                        if (a.name.ToLower().Contains(SysInfo.CpuArch.ToLower()) && ( SysInfo.CpuArch != "Arm" || SysInfo.CpuArch == "Arm" && !a.name.ToLower().Contains("arm64")))
                                        {
                                            releaseAsset = a;
                                            continue;
                                        }
                                    }
                                }
                            }

                            if (releaseAsset != null && releaseAsset.browser_download_url != null)
                            {
                                if (!globalSettings.autoUpdate)
                                {
                                    var result = await MessageBox.Show(null, "Knossos.NET " + latest.tag_name + ":\n" + latest.body + "\n\n\nIf you continue Knossos.NET will be re-started after download.", "An update is avalible", MessageBox.MessageBoxButtons.ContinueCancel);
                                    if(result != MessageBox.MessageBoxResult.Continue)
                                    {
                                        return;
                                    }
                                }
                                else
                                {
                                    await Task.Delay(500);
                                }
                                var download = await Dispatcher.UIThread.InvokeAsync(async () => await TaskViewModel.Instance!.AddFileDownloadTask(releaseAsset.browser_download_url, SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "update.zip", "Downloading "+latest.tag_name+" "+releaseAsset.name, true, "This is a Knossos.NET update"), DispatcherPriority.Background);
                                if (download != null && download == true)
                                {
                                    //Rename files in app folder
                                    var appDirPath = System.AppDomain.CurrentDomain.BaseDirectory;
                                    var execName = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
                                    File.Move(execName!, execName + ".old", true);
                                    if (SysInfo.IsWindows)
                                    {
                                        try
                                        {
                                            File.Move(Path.Combine(appDirPath,"av_libglesv2.dll"), Path.Combine(appDirPath, "av_libglesv2.dll.old"), true);
                                        }
                                        catch { }
                                        try
                                        {
                                            File.Move(Path.Combine(appDirPath, "libSkiaSharp.dll"), Path.Combine(appDirPath, "libSkiaSharp.dll.old"), true);
                                        }
                                        catch { }
                                        try
                                        {
                                            File.Move(Path.Combine(appDirPath, "libHarfBuzzSharp.dll"), Path.Combine(appDirPath, "libHarfBuzzSharp.dll.old"), true);
                                        }
                                        catch { }
                                    } 
                                    if(SysInfo.IsLinux)
                                    {
                                        try
                                        {
                                            File.Move(Path.Combine(appDirPath, "libHarfBuzzSharp.so"), Path.Combine(appDirPath, "libHarfBuzzSharp.so.old"), true);
                                        }
                                        catch { }
                                        try
                                        {
                                            File.Move(Path.Combine(appDirPath, "libSkiaSharp.so"), Path.Combine(appDirPath, "libSkiaSharp.so.old"), true);
                                        }
                                        catch { }
                                    }
                                    if(SysInfo.IsMacOS)
                                    {
                                        try
                                        {
                                            File.Move(Path.Combine(appDirPath, "libAvaloniaNative.dylib"), Path.Combine(appDirPath, "libAvaloniaNative.dylib.old"), true);
                                        }
                                        catch { }
                                        try
                                        {
                                            File.Move(Path.Combine(appDirPath, "libHarfBuzzSharp.dylib"), Path.Combine(appDirPath, "libHarfBuzzSharp.dylib.old"), true);
                                        }
                                        catch { }
                                        try
                                        {
                                            File.Move(Path.Combine(appDirPath, "libSkiaSharp.dylib"), Path.Combine(appDirPath, "libSkiaSharp.dylib.old"), true);
                                        }
                                        catch { }
                                    }

                                    //Decompress new files
                                    try
                                    {
                                        using (var archive = ArchiveFactory.Open(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "update.zip"))
                                        {
                                            try
                                            {
                                                var reader = archive.ExtractAllEntries();
                                                while (reader.MoveToNextEntry())
                                                {
                                                    if (!reader.Entry.IsDirectory)
                                                    {
                                                        reader.WriteEntryToDirectory(appDirPath!, new ExtractionOptions() { ExtractFullPath = false, Overwrite = true });
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Log.Add(Log.LogSeverity.Error, "Knossos.CheckKnetUpdates()", ex);
                                            }

                                            //Start again
                                            try
                                            {
                                                if (SysInfo.IsMacOS || SysInfo.IsLinux)
                                                {
                                                    SysInfo.Chmod(Path.Combine(appDirPath, execName!), "+x");
                                                }
                                                Process p = new Process();
                                                p.StartInfo.FileName = Path.Combine(appDirPath, execName!);
                                                p.Start();
                                            }
                                            catch (Exception ex)
                                            {
                                                Log.Add(Log.LogSeverity.Error, "Knossos.CheckKnetUpdates()", ex);
                                            }

                                            //Close App
                                            MainWindow.instance!.Close();
                                        }
                                    }catch(Exception ex)
                                    {
                                        //Rollback
                                        try
                                        {
                                            File.Move(execName + ".old", execName!, true);
                                        }
                                        catch { }
                                        if (SysInfo.IsWindows)
                                        {
                                            try
                                            {
                                                File.Move(Path.Combine(appDirPath, "av_libglesv2.dll.old"), Path.Combine(appDirPath, "av_libglesv2.dll"), true);
                                            }
                                            catch { }
                                            try
                                            {
                                                File.Move(Path.Combine(appDirPath, "libSkiaSharp.dll.old"), Path.Combine(appDirPath, "libSkiaSharp.dll"), true);
                                            }
                                            catch { }
                                            try
                                            {
                                                File.Move(Path.Combine(appDirPath, "libHarfBuzzSharp.dll.old"), Path.Combine(appDirPath, "libHarfBuzzSharp.dll"), true);
                                            }
                                            catch { }
                                        }
                                        if (SysInfo.IsLinux)
                                        {
                                            try
                                            {
                                                File.Move(Path.Combine(appDirPath, "libHarfBuzzSharp.so.old"), Path.Combine(appDirPath, "libHarfBuzzSharp.so"), true);
                                            }
                                            catch { }
                                            try
                                            {
                                                File.Move(Path.Combine(appDirPath, "libSkiaSharp.so.old"), Path.Combine(appDirPath, "libSkiaSharp.so"), true);
                                            }
                                            catch { }
                                        }
                                        if (SysInfo.IsMacOS)
                                        {
                                            try
                                            {
                                                File.Move(Path.Combine(appDirPath, "libAvaloniaNative.dylib.old"), Path.Combine(appDirPath, "libAvaloniaNative.dylib"), true);
                                            }
                                            catch { }
                                            try
                                            {
                                                File.Move(Path.Combine(appDirPath, "libHarfBuzzSharp.dylib.old"), Path.Combine(appDirPath, "libHarfBuzzSharp.dylib"), true);
                                            }
                                            catch { }
                                            try
                                            {
                                                File.Move(Path.Combine(appDirPath, "libSkiaSharp.dylib.old"), Path.Combine(appDirPath, "libSkiaSharp.dylib"), true);
                                            }
                                            catch { }
                                        }
                                        await MessageBox.Show(MainWindow.instance, "Error during update file decompression. Update was cancelled.", "Decompression failed", MessageBox.MessageBoxButtons.OK);
                                        Log.Add(Log.LogSeverity.Error, "Knossos.CheckKnetUpdates()", ex);
                                    }
                                }
                            }
                            else
                            {
                                Log.Add(Log.LogSeverity.Warning, "Knossos.CheckKnetUpdates()", "Unable to find a matching OS and cpu arch build in the latest Github release, update has been skipped. CPU Arch:"+ SysInfo.CpuArch + " OS: "+ SysInfo.GetOSNameString());
                            }
                        }
                        else
                        {
                            Log.Add(Log.LogSeverity.Error, "Knossos.CheckKnetUpdates()", "Last version from Github has a null asset array.");
                        }
                    }
                }
                else
                {
                    Log.Add(Log.LogSeverity.Error, "Knossos.CheckKnetUpdates()", "Get latest version from github resulted in null or tag_name being null.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Knossos.CheckKnetUpdates()", ex);
            }
        }

        public static void OpenQuickSetup()
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var dialog = new QuickSetupView();
                dialog.DataContext = new QuickSetupViewModel();
                dialog.Show(MainWindow.instance!);
            });
        }

        public static void ResetBasePath()
        {
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                MainWindowViewModel.Instance?.ClearViews();
                installedMods.Clear();
                engineBuilds.Clear();
                retailFs2RootFound = false;
                TaskViewModel.Instance?.CancelAllRunningTasks();
                Nebula.CancelOperations();
                await Task.Delay(2000);
                LoadBasePath();
            });
        }

        public async static void LoadBasePath(bool isQuickLaunch = false)
        {
            if (globalSettings.basePath != null)
            {
                await FolderSearchRecursive(globalSettings.basePath, isQuickLaunch);

                if (!isQuickLaunch)
                {
                    //Red border for mod with missing deps
                    MainWindowViewModel.Instance?.RunDependenciesCheck();

                    //Load config options to view, must be done after loading the fso builds due to flag data
                    MainWindowViewModel.Instance?.GlobalSettingsLoadData();

                    //Enter the nebula
                    //Note: this has to be done after scanning the local folder
                    await Task.Run(() => { Nebula.Trinity(); });
                }
                else
                {
                    await QuickLaunch();
                }
            }
        }

        public static string? GetKnossosLibraryPath()
        {
            return globalSettings.basePath;
        }

        public static async void PlayMod(Mod mod, FsoExecType fsoExecType, bool standaloneServer = false, int standalonePort = 0)
        {
            if (TaskViewModel.Instance!.IsSafeState() == false)
            {
                var result=await MessageBox.Show(MainWindow.instance!, "Other important tasks are running, it is recommended that you wait until they finish before launching the game because it may cause them to fail.\nIf you are absolutely sure those tasks cannot interfere you can continue.", "Tasks are running", MessageBox.MessageBoxButtons.ContinueCancel);
                if(result != MessageBox.MessageBoxResult.Continue)
                    return;
            }

            Log.Add(Log.LogSeverity.Information, "Knossos.PlayMod()", "Launching Mod: " + mod.folderName);

            /* Check the dependencies and stop if there are unresolved ones */
            var missingDeps = mod.GetMissingDependenciesList();
            var errorMsg = string.Empty;

            foreach (var dependency in missingDeps.ToList())
            {
                //If we are missing the FSO dep and the user selected a custom build, ignore it
                if (mod.modSettings.customBuildId != null && dependency.id == "FSO")
                {
                    missingDeps.Remove(dependency);
                }
                else
                {
                    Log.Add(Log.LogSeverity.Warning, "Knossos.PlayMod()", "Unable to resolve dependency : " + dependency.id + " - " + dependency.version);
                    errorMsg += "\n" + "Unable to resolve dependency : " + dependency.id + " - " + dependency.version;
                    if (dependency.packages != null)
                    {
                        var selected = dependency.SelectMod();
                        foreach (string okg in dependency.packages)
                        {
                            if (selected != null)
                            {
                                Log.Add(Log.LogSeverity.Warning, "Knossos.PlayMod()", "Missing Package for : " + dependency.id + " - " + selected.version + " - PKG NAME: " + okg);
                                errorMsg += "\n" + "Missing Package for : " + dependency.id + " - " + selected.version + " - PKG NAME: " + okg;
                            }
                            else
                            {
                                Log.Add(Log.LogSeverity.Warning, "Knossos.PlayMod()", "Missing Package for : " + dependency.id + " - PKG NAME: " + okg);
                                errorMsg += "\n" + "Missing Package for : " + dependency.id + " - PKG NAME: " + okg;
                            }
                        }
                    }
                }
            }

            if(missingDeps.Count > 0)
            {
                if (MainWindow.instance != null)
                {
                    await MessageBox.Show(MainWindow.instance, errorMsg, "Unable to resolve dependencies", MessageBox.MessageBoxButtons.OK);
                }

                return;
            }


            /* Build cmdline */
            var cmdline = "-parse_cmdline_only";
            var modFlag = string.Empty;
            var modList = new List<Mod>();
            FsoBuild? fsoBuild = null;

            /* Resolve Dependencies should be all valid at this point */
            var dependencyList = mod.GetModDependencyList();
            bool hasBuildDependency = false;
            if (dependencyList != null)
            {
                foreach (var dep in dependencyList)
                {
                    var selectedMod = dep.SelectMod();
                    if (selectedMod != null)
                    {
                        modList.Add(selectedMod);
                    }
                    else
                    {
                        /* 
                            It has to be the engine dependency, "SelectMod" does not select engine builds
                            Do not do this if there is a user selected mod-especific or global build
                        */
                        //TODO: What if there are more than one engine build in dependency list? Im not sure what to do in that case.
                        if (fsoBuild == null && mod.modSettings.customBuildId == null)
                        {
                            hasBuildDependency = true;
                            fsoBuild = dep.SelectBuild();
                        }
                    }
                }
            }

            /* Search for the user-especified FSO build */
            if (fsoBuild == null && mod.modSettings.customBuildId != null && mod.modSettings.customBuildVersion != null)
            {
                if (mod.modSettings.customBuildExec == null)
                {
                    fsoBuild = engineBuilds.FirstOrDefault(builds => builds.id == mod.modSettings.customBuildId && builds.version == mod.modSettings.customBuildVersion);
                }
                else
                {
                    fsoBuild = new FsoBuild(mod.modSettings.customBuildExec);
                }

                if(fsoBuild == null)
                {
                    Log.Add(Log.LogSeverity.Warning, "Knossos.PlayMod()", "Unable to find the user selected build for mod " + mod.title + " " + mod.version + " requested build " + mod.modSettings.customBuildId + " " + mod.modSettings.customBuildVersion);
                }
            }

            /* Build mod flag */
            foreach (var modid in mod.GetModFlagList())
            {
                /* Load this mod */
                if (modid == mod.id)
                {
                    /* Dev Mode ON */
                    if (mod.devMode)
                    {
                        foreach (var pkg in mod.packages.OrderBy(x => x.name))
                        {
                            if (modFlag.Length > 0)
                            {
                                modFlag += "," + mod.folderName + Path.DirectorySeparatorChar + pkg.folder;
                            }
                            else
                            {
                                modFlag += mod.folderName + Path.DirectorySeparatorChar + pkg.folder;
                            }
                        }

                        //If standalone the multi.cfg will be on mod root\data to avoid uploading it to nebula
                        if (standaloneServer)
                        {
                            if (modFlag.Length > 0)
                            {
                                modFlag += "," + mod.folderName;
                            }
                            else
                            {
                                modFlag += mod.folderName;
                            }
                        }
                    }
                    else
                    {
                        //avoid passing FS2 in modflag because fs2retail runs off the working folder
                        if (mod.id != "FS2")
                        {
                            if (modFlag.Length > 0)
                            {
                                modFlag += "," + mod.folderName;
                            }
                            else
                            {
                                modFlag += mod.folderName;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var depMod in modList)
                    {
                        if (depMod.id == modid)
                        {
                            /* Dev Mode ON */
                            if (depMod.devMode)
                            {
                                foreach (var pkg in depMod.packages)
                                {
                                    if(modFlag.Length > 0)
                                    {
                                        modFlag += "," + depMod.folderName + Path.DirectorySeparatorChar + pkg.folder;
                                    }
                                    else
                                    {
                                        modFlag += depMod.folderName + Path.DirectorySeparatorChar + pkg.folder;
                                    }
                                    
                                }
                            }
                            else
                            {
                                if (modFlag.Length > 0)
                                {
                                    modFlag += "," + depMod.folderName;
                                }
                                else
                                {
                                    modFlag += depMod.folderName;
                                }
                            }
                        }
                    }
                }
            }

            /* Determine root folder */
            var rootPath = string.Empty;

            /* Others TCs: WCS, Solaris, etc */
            if (mod.type == "tc" && mod.parent == null)
            {
                rootPath = mod.fullPath.Replace(mod.folderName+Path.DirectorySeparatorChar,"");
            }
            else
            {
                /* Most likely FS2 Retail only */
                if (mod.type == "tc" && mod.parent != null || mod.parent == mod.id)
                {
                    rootPath = mod.fullPath;
                }
                else
                {
                    /* Regular mods */
                    if(mod.type == "mod" && mod.parent != null)
                    {
                        foreach(var installedMod in installedMods) 
                        { 
                            if(installedMod.type == "tc" && installedMod.id == mod.parent)
                            {
                                if (installedMod.parent != null)
                                {
                                    rootPath = installedMod.fullPath;
                                }
                                else
                                {
                                    rootPath = mod.fullPath.Replace(mod.folderName + Path.DirectorySeparatorChar, "");
                                }
                            }
                        }
                    }
                }
            }

            if(rootPath == string.Empty)
            {
                Log.Add(Log.LogSeverity.Error, "Knossos.PlayMod()", "Unable to determine working folder for mod: " + mod.folderName);
                if (MainWindow.instance != null)
                {
                    await MessageBox.Show(MainWindow.instance, "Unable to determine working folder for mod: " + mod.folderName, "Error launching mod", MessageBox.MessageBoxButtons.OK);
                }
                return;
            }
            else
            {
                Log.Add(Log.LogSeverity.Information, "Knossos.PlayMod()", "Used working folder : " + rootPath);
            }

            if(fsoBuild == null)
            {
                Log.Add(Log.LogSeverity.Error, "Knossos.PlayMod()", "Unable to find a valid FSO build for this mod!");
                if(hasBuildDependency)
                {
                    await MessageBox.Show(MainWindow.instance!, "Unable to find a valid FSO build for this mod!", "Error launching mod", MessageBox.MessageBoxButtons.OK);
                }
                else
                {
                    await MessageBox.Show(MainWindow.instance!, "This mod does not especifies a engine build to use, you should select one in the mod settings.", "Error launching mod", MessageBox.MessageBoxButtons.OK);
                }

                return;
            }
            else
            {
                Log.Add(Log.LogSeverity.Information, "Knossos.PlayMod()", "Used FSO build : " + fsoBuild.version);
                try
                {
                    if (SemanticVersion.Compare(fsoBuild.version, VPCompression.MinimumFSOVersion) < 0)
                    {
                        if (mod.modSettings.isCompressed)
                        {
                            var result = await MessageBox.Show(MainWindow.instance!, "This mod currently resolves to FSO build: " + fsoBuild.version + " and it is compressed, the minimum to fully support all compression features is: " + VPCompression.MinimumFSOVersion + ".\n23.0.0 may work if the mod do not have loose files, older versions are not going to work. Use a newer FSO version or uncompress this mod.", "FSO Version below minimum for compression", MessageBox.MessageBoxButtons.ContinueCancel);
                            if (result != MessageBox.MessageBoxResult.Continue)
                                return;
                        }
                        else
                        {
                            var compressedMods = string.Empty;
                            if (mod.parent == "FS2")
                            {
                                var fs2 = Knossos.GetInstalledMod("FS2", "1.20.0");
                                if (fs2 != null && fs2.modSettings.isCompressed)
                                {
                                    compressedMods += fs2 + ", ";
                                }
                            }
                            var depMods = mod.GetModDependencyList();
                            if (depMods != null)
                            {
                                foreach (var depMod in depMods)
                                {
                                    var m = depMod.SelectMod();
                                    if (m != null && m.modSettings.isCompressed)
                                    {
                                        compressedMods += m + ", ";
                                    }
                                }
                            }
                            if (compressedMods != string.Empty)
                            {
                                var result = await MessageBox.Show(MainWindow.instance!, "This mod currently resolves to FSO build: " + fsoBuild.version + " and depends on mods: " + compressedMods + " that are currently compressed, the minimum to fully support all compression features is: " + VPCompression.MinimumFSOVersion + ".\n23.0.0 may work if the mod do not have loose files, older versions are not going to work. Use a newer FSO version or uncompress those mods.", "FSO Version below minimum for compression", MessageBox.MessageBoxButtons.ContinueCancel);
                                if (result != MessageBox.MessageBoxResult.Continue)
                                    return;
                            }
                        }
                    }
                }catch(Exception ex)
                {
                    Log.Add(Log.LogSeverity.Error, "Knossos.PlayMod()", ex);
                }
            }

            //Get full path for requested exec type
            var execPath = fsoBuild.GetExec(fsoExecType);

            if (execPath == null)
            {
                Log.Add(Log.LogSeverity.Error, "Knossos.PlayMod()", "Could not find a executable type for the requested fso build :" +fsoBuild.ToString() + " Requested Type: "+fsoExecType );
                if (MainWindow.instance != null)
                {
                    await MessageBox.Show(MainWindow.instance, "Could not find a executable type for the requested fso build :" + fsoBuild.ToString() + " Requested Type: " + fsoExecType, "Error launching mod", MessageBox.MessageBoxButtons.OK);
                }
                return;
            }

            /* Build the cmdline, take in consideration systemcmd, globalcmd, modcmd(with user changes if any) */
            var modCmd = mod.GetModCmdLine()?.Split('-');
            var systemCmd = Knossos.globalSettings.GetSystemCMD(fsoBuild)?.Split('-');
            var globalCmd = Knossos.globalSettings.globalCmdLine?.Split('-');

            if (systemCmd != null)
            {
                foreach (var s in systemCmd)
                {
                    if (s.Trim().Length > 0)
                    {
                        if(!standaloneServer || standaloneServer && standalonePort == 0 || standaloneServer && standalonePort != 0 && !s.Contains("-port"))
                            cmdline += " -" + s.Trim();
                    }

                }
            }

            if (standaloneServer && standalonePort > 0)
            {
                cmdline += " -port " + standalonePort;
            }

            if (globalCmd != null)
            {
                foreach (var s in globalCmd)
                {
                    if (s.Trim().Contains(" "))
                    {
                        var param = s.Trim().Split(' ')[0];
                        if (!cmdline.Contains(param))
                        {
                            if (s.Trim().Length > 0)
                            {
                                cmdline += " -" + s.Trim();
                            }
                        }
                    }
                    else
                    {
                        if (!cmdline.Contains(s.Trim()))
                        {
                            if (s.Trim().Length > 0)
                            {
                                cmdline += " -" + s.Trim();
                            }
                        }
                    }

                }
            }

            if (modCmd != null)
            {
                foreach (var s in modCmd)
                {
                    if (s.Trim().Contains(" "))
                    {
                        var param = s.Trim().Split(' ')[0];
                        if (!cmdline.Contains(param))
                        {
                            if (s.Trim().Length > 0)
                            {
                                cmdline += " -" + s.Trim();
                            }
                        }
                    }
                    else
                    {
                        if (!cmdline.Contains(s.Trim()))
                        {
                            if (s.Trim().Length > 0)
                            {
                                cmdline += " -" + s.Trim();
                            }
                        }
                    }

                }
            }

            if(standaloneServer)
            {
                cmdline += " -standalone";
            }

            if (modFlag.Length > 0)
            {
                cmdline += " -mod " + modFlag;
            }

            Log.Add(Log.LogSeverity.Information, "Knossos.PlayMod()", "Used cmdLine : " + cmdline);

            try
            {
                //In Linux make sure it is marked as executable
                if (SysInfo.IsLinux || SysInfo.IsMacOS)
                {
                    SysInfo.Chmod(execPath, "+x");
                }

                //In Windows enable the High DPI aware
                try
                {
                    if (SysInfo.IsWindows)
                    {
                        using var dpiProccess = new Process();
                        dpiProccess.StartInfo.FileName = "REG";
                        dpiProccess.StartInfo.Arguments = "ADD \"HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers\" /V \"" + execPath + "\" /T REG_SZ /D HIGHDPIAWARE /F";
                        dpiProccess.StartInfo.UseShellExecute = false;
                        dpiProccess.StartInfo.Verb = "runas";
                        dpiProccess.Start();
                        dpiProccess.WaitForExit();
                    }
                }catch { }

                //LAUNCH!! FINALLY!
                using var fso = new Process();
                fso.StartInfo.FileName = execPath;
                fso.StartInfo.Arguments = cmdline;
                fso.StartInfo.UseShellExecute = false;
                fso.StartInfo.WorkingDirectory = rootPath;
                fso.Start();
            }catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Knossos.PlayMod()", ex);
                await MessageBox.Show(MainWindow.instance!, ex.Message, "Error launching fso", MessageBox.MessageBoxButtons.OK);
            }
        }

        public static List<Mod> GetInstalledModList(string? id)
        {
            if (id != null)
            {
                var modList = new List<Mod>();
                foreach (var mod in installedMods)
                {
                    if (id == mod.id)
                    {
                        modList.Add(mod);
                    }
                }
                return modList;
            }
            else
            {
                return installedMods;
            }
        }

        public static Mod? GetInstalledMod(string id, string version)
        {
            return installedMods.FirstOrDefault(m => m.id == id && m.version == version);
        }

        public static List<FsoBuild> GetInstalledBuildsList(string? id=null, FsoStability? stability = null)
        {
            if (id != null)
            {
                if(stability != null)
                {
                    return engineBuilds.Where(build => build.id == id && build.stability == stability).ToList();
                }
                else
                {
                    return engineBuilds.Where(build => build.id == id).ToList();
                }
            }
            else
            {
                if(stability != null)
                {
                    return engineBuilds.Where(build => build.stability == stability).ToList();
                }
                else
                {
                    return engineBuilds;
                }
            }
        }

        private static async Task FolderSearchRecursive(string path, bool isQuickLaunch, int folderLevel=0)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(@path);
                DirectoryInfo[] arrDir = di.GetDirectories();

                if (folderLevel <= 1)
                {
                    foreach (DirectoryInfo dir in arrDir)
                    {
                        await FolderSearchRecursive(dir.ToString() + Path.DirectorySeparatorChar, isQuickLaunch, folderLevel + 1);
                    }
                }

                if(File.Exists(path + Path.DirectorySeparatorChar + "knossos_net_download.token"))
                {
                    /* This is a incomplete download, delete the folder */
                    Log.Add(Log.LogSeverity.Warning, "Knossos.FolderSearchRecursive()", "Deleting incomplete download found at "+path);
                    Directory.Delete(path, true);
                }
                else if (File.Exists(path + Path.DirectorySeparatorChar + "mod.json"))
                {
                    try
                    {
                        var modJson = new Mod(path,di.Name);
                        switch (modJson.type)
                        {
                            case "tc":
                            case "mod": 
                                installedMods.Add(modJson);
                                if (modJson.id == "FS2" && modJson.type == "tc" && modJson.parent == "FS2")
                                {
                                    if (File.Exists(modJson.fullPath + Path.DirectorySeparatorChar + "root_fs2.vp") || File.Exists(modJson.fullPath + Path.DirectorySeparatorChar + "root_fs2.vpc"))
                                    {
                                        retailFs2RootFound = true;
                                        Log.Add(Log.LogSeverity.Information, "Knossos.FolderSearchRecursive", "Found FS2 Root Pack!");
                                    }
                                }
                                if(!isQuickLaunch)
                                    await Dispatcher.UIThread.InvokeAsync(() => MainWindowViewModel.Instance?.AddInstalledMod(modJson), DispatcherPriority.Background);
                                break;

                            case "engine":
                                var build = new FsoBuild(modJson);
                                engineBuilds.Add(build);
                                if(!isQuickLaunch)
                                    await Dispatcher.UIThread.InvokeAsync(() => FsoBuildsViewModel.Instance?.AddBuildToUi(build), DispatcherPriority.Background);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        /* Likely if json parsing fails */
                        Log.Add(Log.LogSeverity.Error, "Knossos.ModSearchRecursive", ex);
                    }
                }
            }catch (Exception ex)
            {
                /* Likely file/folder permission issues */
                Log.Add(Log.LogSeverity.Error, "Knossos.ModSearchRecursive", ex);
            }
        }

        public static void OpenBrowserURL(string url)
        {
            try
            {
                if (SysInfo.IsWindows)
                {
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
                }
                else if (SysInfo.IsLinux)
                {
                    Process.Start("xdg-open", url);
                }
                else if (SysInfo.IsMacOS)
                {
                    Process.Start("open", url);
                }
                else
                {
                }
            }
            catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error,"Knossos.OpenBrowser()",ex);
            }
        }

        public static void WriteToUIConsole(string message)
        {
            MainWindowViewModel.Instance?.WriteToUIConsole(message);
        }

        public static void RemoveBuild(FsoBuild build)
        {
            engineBuilds.Remove(build);
        }

        public static void AddBuild(FsoBuild build)
        {
            engineBuilds.Add(build);
        }

        public static void AddMod(Mod mod)
        {
            installedMods.Add(mod);
        }

        /* Remove ALL */
        public static void RemoveMod(string modId)
        {
            try
            {
                var delete = installedMods.Where(m => m.id == modId).ToList();
                if (delete.Any())
                {
                    MainWindowViewModel.Instance?.RemoveInstalledMod(modId);
                    foreach (var mod in delete)
                    {
                        Log.Add(Log.LogSeverity.Information, "Knossos.RemoveMod()", "Deleting mod: "+mod.title + " " +mod.version);
                        Directory.Delete(mod.fullPath, true); 
                        installedMods.Remove(mod);
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Knossos.RemoveMod()", ex);
            }
        }

        /* Remove one version */
        public static void RemoveMod(Mod mod)
        {
            try
            {
                Log.Add(Log.LogSeverity.Information, "Knossos.RemoveMod()", "Deleting mod: " + mod.title + " " + mod.version);
                Directory.Delete(mod.fullPath, true);
                installedMods.Remove(mod);
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Knossos.RemoveMod()", ex);
            }
        }

        public async static void Tts(string text, int? voice_index = null, int? volume = null, Func<bool>? callBack = null)
        {
            try
            {
                if (globalSettings.enableTts)
                {
                    if (SysInfo.IsWindows)
                    {
                        if (ttsObject != null)
                        {
                            var sp = (Process)ttsObject;
                            sp.Kill();
                            ttsObject = null;
                        }
                        if (text != string.Empty)
                        {
                            if (!File.Exists(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "KSapi.exe"))
                            {
                                if (SysInfo.CpuArch == "X86")
                                {
                                    using (var fileStream = File.Create(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "KSapi.exe"))
                                    {
                                        AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/utils/KSapi_x86.exe")).CopyTo(fileStream);
                                        fileStream.Close();
                                    }
                                }
                                else
                                {
                                    using (var fileStream = File.Create(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "KSapi.exe"))
                                    {
                                        AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/utils/KSapi.exe")).CopyTo(fileStream);
                                        fileStream.Close();
                                    }
                                }
                            }
                            await Task.Run(async () =>
                            {
                                //Max cmdline lenght is 8192, lets limit the text to 7500
                                if(text.Length > 7500)
                                {
                                    text = text.Substring(0, 7500);
                                }
                                await Task.Delay(300);
                                var voice = globalSettings.ttsVoice;
                                var vol = globalSettings.ttsVolume;
                                if (voice_index.HasValue)
                                    voice = voice_index.Value;
                                if (volume.HasValue)
                                    vol = volume.Value;
                                using var ttsProcess = new Process();
                                ttsProcess.StartInfo.FileName = SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "KSapi.exe";
                                ttsProcess.StartInfo.Arguments = "-text \"" + text + "\" -voice " + voice + " -vol " + vol;
                                ttsProcess.StartInfo.UseShellExecute = false;
                                ttsProcess.StartInfo.CreateNoWindow = true;
                                ttsObject = ttsProcess;
                                ttsProcess.Start();
                                ttsProcess.WaitForExit();
                                ttsObject = null;
                                if (callBack != null)
                                    await Dispatcher.UIThread.InvokeAsync(() => callBack(), DispatcherPriority.Background);
                            });
                        }
                    }
                    if(SysInfo.IsLinux)
                    {
                        //unimplemented
                        if (callBack != null)
                            await Dispatcher.UIThread.InvokeAsync(() => callBack(), DispatcherPriority.Background);
                    }
                    if(SysInfo.IsMacOS)
                    {
                        //unimplemented
                        if (callBack != null)
                            await Dispatcher.UIThread.InvokeAsync(() => callBack(), DispatcherPriority.Background);
                    }
                }
                else
                {
                    if (callBack != null)
                        await Dispatcher.UIThread.InvokeAsync(() => callBack(), DispatcherPriority.Background);
                }
            }catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error,"Knossos.TTS()",ex);
                if (callBack != null)
                    callBack();
            }
        }
    }
}
