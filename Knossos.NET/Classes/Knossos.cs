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

namespace Knossos.NET
{
    public static class Knossos
    {
        private static List<Mod> installedMods = new List<Mod>();
        private static List<FsoBuild> engineBuilds = new List<FsoBuild>();
        public static GlobalSettings globalSettings = new GlobalSettings();
        public static bool retailFs2RootFound = false;
        public static bool flagDataLoaded = false;
        private static object? ttsObject = null;

        public static async void StartUp()
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
            }catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error,"Knossos.StartUp()",ex);
                if (MainWindow.instance != null)
                {
                    await MessageBox.Show(MainWindow.instance, "Unable to write to KnossosNET data folder." , "KnossosNET Error", MessageBox.MessageBoxButtons.OK);
                }
            }

            Log.Add(Log.LogSeverity.Information, "Knossos.StartUp()", "=== KnossosNET Start ===");

            //Load language files
            Lang.LoadFiles();

            //Load knossos config
            globalSettings.Load();

            //Load base path from knossos legacy
            if (globalSettings.basePath == null)
            {
                globalSettings.basePath = SysInfo.GetBasePathFromKnossosLegacy();
            }

            LoadBasePath();

            if (globalSettings.basePath == null)
                OpenQuickSetup();
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

        public async static void LoadBasePath()
        {
            if (globalSettings.basePath != null)
            {
                await FolderSearchRecursive(globalSettings.basePath);

                //Red border for mod with missing deps
                MainWindowViewModel.Instance?.RunDependenciesCheck();

                //Load config options to view, must be done after loading the fso builds due to flag data
                MainWindowViewModel.Instance?.GlobalSettingsLoadData();

                //Enter the nebula
                //Note: this has to be done after scanning the local folder
                await Task.Run(() => { Nebula.Trinity(); });
            }
        }

        public static string? GetKnossosLibraryPath()
        {
            return globalSettings.basePath;
        }

        public static async void PlayMod(Mod mod, FsoExecType fsoExecType)
        {
            if (TaskViewModel.Instance!.IsSafeState() == false)
            {
                await MessageBox.Show(MainWindow.instance!, "You can not launch a mod while other tasks are running, wait until they finish and try again.", "Tasks are running", MessageBox.MessageBoxButtons.OK);
                return;
            }

            Log.Add(Log.LogSeverity.Information, "Knossos.PlayMod()", "Launching Mod: " + mod.folderName);

            /* Check the dependencies and stop if there are unresolved ones */
            var missingDeps = mod.GetMissingDependenciesList();
            var errorMsg = string.Empty;
            foreach (var dependency in missingDeps)
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
                        foreach (var pkg in mod.packages)
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
                        cmdline += " -" + s.Trim();
                    }

                }
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

            if (modFlag.Length > 0)
            {
                cmdline += " -mod " + modFlag;
            }

            Log.Add(Log.LogSeverity.Information, "Knossos.PlayMod()", "Used cmdLine : " + cmdline);

            try
            {
                //In Linux make sure it is marked as executable
                if (SysInfo.IsLinux)
                {
                    SysInfo.Chmod(execPath, "+x");
                }

                //In Windows enable the High DPI aware
                if (SysInfo.IsWindows)
                {
                    using var dpiProccess = new Process();
                    dpiProccess.StartInfo.FileName = "REG";
                    dpiProccess.StartInfo.Arguments = "ADD \"HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers\" /V \""+execPath+"\" /T REG_SZ /D HIGHDPIAWARE /F";
                    dpiProccess.StartInfo.UseShellExecute = false;
                    dpiProccess.StartInfo.Verb = "runas";
                    dpiProccess.Start();
                    dpiProccess.WaitForExit();
                }

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

        private static async Task FolderSearchRecursive(string path, int folderLevel=0)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(@path);
                DirectoryInfo[] arrDir = di.GetDirectories();

                if (folderLevel <= 1)
                {
                    foreach (DirectoryInfo dir in arrDir)
                    {
                        await FolderSearchRecursive(dir.ToString() + Path.DirectorySeparatorChar, folderLevel + 1);
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
                                    if (File.Exists(modJson.fullPath + Path.DirectorySeparatorChar + "root_fs2.vp"))
                                    {
                                        retailFs2RootFound = true;
                                        Log.Add(Log.LogSeverity.Information, "Knossos.FolderSearchRecursive", "Found FS2 Root Pack!");
                                    }
                                }
                                await Dispatcher.UIThread.InvokeAsync(() => MainWindowViewModel.Instance?.AddInstalledMod(modJson), DispatcherPriority.Background);
                                break;

                            case "engine":
                                var build = new FsoBuild(modJson);
                                engineBuilds.Add(build);
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
                                var assets = Avalonia.AvaloniaLocator.Current.GetService<IAssetLoader>();
                                if (assets != null)
                                {
                                    if (SysInfo.CpuArch == "X86")
                                    {
                                        using (var fileStream = File.Create(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "KSapi.exe"))
                                        {
                                            assets.Open(new Uri("avares://Knossos.NET/Assets/utils/KSapi_x86.exe")).CopyTo(fileStream);
                                            fileStream.Close();
                                        }
                                    }
                                    else
                                    {
                                        using (var fileStream = File.Create(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "KSapi.exe"))
                                        {
                                            assets.Open(new Uri("avares://Knossos.NET/Assets/utils/KSapi.exe")).CopyTo(fileStream);
                                            fileStream.Close();
                                        }
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
