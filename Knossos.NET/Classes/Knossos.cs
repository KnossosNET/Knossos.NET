using Avalonia.Threading;
using Knossos.NET.Models;
using Knossos.NET.ViewModels;
using Knossos.NET.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Knossos.NET
{
    public static class Knossos
    {
        private static List<Mod> installedMods = new List<Mod>();
        private static List<FsoBuild> engineBuilds = new List<FsoBuild>();
        public static GlobalSettings globalSettings = new GlobalSettings();
        public static bool retailFs2RootFound = false;
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
                    using (StreamWriter writer = new StreamWriter(SysInfo.GetKnossosDataFolderPath() + @"\test.txt"))
                    {
                        writer.WriteLine("test");
                    }
                    File.Delete(SysInfo.GetKnossosDataFolderPath() + @"\test.txt");
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
            LoadKnossosSettings();

            //Load base path from knossos legacy
            if(globalSettings.basePath == null)
            {
                globalSettings.basePath = SysInfo.GetBasePathFromKnossosLegacy();
            }

            LoadBasePath();
        }

        public static void ResetBasePath()
        {
            MainWindowViewModel.Instance?.ClearBasePathViews();
            installedMods.Clear();
            engineBuilds.Clear();
            retailFs2RootFound = false;
            TaskViewModel.Instance?.CancelAllRunningTasks();
            LoadBasePath();
        }

        public async static void LoadBasePath()
        {
            if (globalSettings.basePath != null)
            {
                await FolderSearchRecursive(globalSettings.basePath);

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
        
        private static void LoadKnossosSettings()
        {
            try
            {
                if (File.Exists(SysInfo.GetKnossosDataFolderPath() + @"\settings.json"))
                {
                    using FileStream jsonFile = File.OpenRead(SysInfo.GetKnossosDataFolderPath() + @"\settings.json");
                    var tempSettings = JsonSerializer.Deserialize<GlobalSettings>(jsonFile)!;
                    jsonFile.Close();
                    if (tempSettings != null)
                    {
                        globalSettings = tempSettings;

                        Log.Add(Log.LogSeverity.Information, "GlobalSettings.Load()", "Global seetings has been loaded");
                    }

                }
                else
                {
                    Log.Add(Log.LogSeverity.Information, "GlobalSettings.Load()", "File settings.json does not exist.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "GlobalSettings.Load()", ex);
            }
        }

        public static async void PlayMod(Mod mod, FsoExecType fsoExecType)
        {
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

            /* Build the cmdline, take in consideration systemcmd, globalcmd, modcmd(with user changes if any) */
            var modCmd = mod.GetModCmdLine()?.Split('-');
            var systemCmd = Knossos.globalSettings.GetSystemCMD()?.Split('-');
            var globalCmd = Knossos.globalSettings.globalCmdLine?.Split('-');

            if(systemCmd!= null)
            {
                foreach(var s in systemCmd)
                {
                    if(s.Trim().Length > 0)
                    {
                        cmdline += " -" + s.Trim();
                    }
                        
                }
            }

            if (globalCmd != null)
            {
                foreach (var s in globalCmd)
                {
                    if(s.Trim().Contains(" "))
                    {
                        var param = s.Trim().Split(' ')[0];
                        if(!cmdline.Contains(param))
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
                                modFlag += "," + mod.folderName + "\\" + pkg.folder;
                            }
                            else
                            {
                                modFlag += mod.folderName + "\\" + pkg.folder;
                            }
                            
                        }
                    }
                    else
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
                                        modFlag += "," + depMod.folderName + "\\" + pkg.folder;
                                    }
                                    else
                                    {
                                        modFlag += depMod.folderName + "\\" + pkg.folder;
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

            cmdline += " -mod " + modFlag;

            Log.Add(Log.LogSeverity.Information, "Knossos.PlayMod()", "Used cmdLine : " + cmdline);

            /* Determine root folder */
            var rootPath = string.Empty;

            /* Others TCs: WCS, Solaris, etc */
            if (mod.type == "tc" && mod.parent == null)
            {
                rootPath = mod.fullPath.Replace(mod.folderName+"\\","");
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
                                    rootPath = mod.fullPath.Replace(mod.folderName + "\\", "");
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

            //Write fs2_open.ini
            Fs2OpenIni iniFile = new Fs2OpenIni();

            if(iniFile.WriteIniFile(fsoBuild))
            {
                //LAUNCH!! FINALLY!
                var fso = new Process();
                fso.StartInfo.FileName = execPath;
                fso.StartInfo.Arguments = cmdline;
                fso.StartInfo.UseShellExecute = false;
                fso.StartInfo.WorkingDirectory = rootPath;
                fso.Start();
            }
            else
            {
                if (MainWindow.instance != null)
                {
                    await MessageBox.Show(MainWindow.instance, "Unable to write fs2_open.ini file!", "Error launching mod", MessageBox.MessageBoxButtons.OK);
                }
                return;
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
                        await FolderSearchRecursive(dir.ToString() + @"\", folderLevel + 1);
                    }
                }

                if (File.Exists(path + @"\mod.json"))
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
                                    if (File.Exists(modJson.fullPath + @"\root_fs2.vp"))
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

        public static void Tts(string text, string? voiceName = null, int? volume = null)
        {
            if(globalSettings.enableTts)
            {
                #pragma warning disable CA1416 //Im using sysinfo to ensure the incompatible code is not executed on a unsupported os
                try
                {
                    if (SysInfo.IsWindows)
                    {
                        if(ttsObject != null)
                        {
                            var sp = (System.Speech.Synthesis.SpeechSynthesizer)ttsObject;
                            sp.SpeakAsyncCancelAll();
                        }
                        if (text != string.Empty)
                        {
                            Task.Run(() =>
                            {
                                using (var sp = new System.Speech.Synthesis.SpeechSynthesizer())
                                {
                                    if (volume.HasValue)
                                    {
                                        sp.Volume = volume.Value;
                                    }
                                    else
                                    {
                                        sp.Volume = globalSettings.ttsVolume;
                                    }
                                    try
                                    {
                                        if (voiceName != null)
                                        {
                                            //I need to adjust for SAPI->OneCore voices name selector, remove "Desktop" and everything after "-"
                                            sp.SelectVoice(voiceName.Replace("Desktop", "").Split("-")[0].Trim());
                                        }
                                        else
                                        {
                                            if (globalSettings.ttsVoiceName != null)
                                                sp.SelectVoice(globalSettings.ttsVoiceName.Replace("Desktop", "").Split("-")[0].Trim());
                                        }
                                    }
                                    catch (ArgumentException ex)
                                    {
                                        Log.Add(Log.LogSeverity.Error, "Knossos.Tts()", ex);
                                    }
                                    ttsObject = sp;
                                    try
                                    {
                                        sp.Speak(text);
                                    }
                                    catch (OperationCanceledException) { }
                                    ttsObject = null;
                                }
                            });
                        }
                    }
                } catch (Exception ex)
                {
                    Log.Add(Log.LogSeverity.Error, "Knossos.Tts()", ex);
                }
                #pragma warning restore CA1416
            }
        }
    }
}
