using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using Avalonia.Threading;
using System.IO;
using System;

namespace Knossos.NET.ViewModels
{
    public partial class ModSettingsViewModel : ViewModelBase
    {
        private Mod? modJson;
        private ModCardViewModel? modCardViewModel;

        private ObservableCollection<DependencyItemViewModel> DepItems { get; set; } = new ObservableCollection<DependencyItemViewModel>();

        /* UI Variables with ObervableProperty*/
        [ObservableProperty]
        private bool configureBuildOpen = false;
        [ObservableProperty]
        private string title = string.Empty;
        [ObservableProperty]
        private bool customDependencies = false;
        [ObservableProperty]
        private FsoBuildPickerViewModel fsoPicker;
        [ObservableProperty]
        private FsoFlagsViewModel? fsoFlags = null;
        [ObservableProperty]
        private string modSize = "0GB";
        [ObservableProperty]
        private bool compression = false;
        [ObservableProperty]
        private bool compressionAvalible = false;
        [ObservableProperty]
        private bool isDevMode = false;

        public ModSettingsViewModel()
        {
            fsoPicker = new FsoBuildPickerViewModel();
        }

        public ModSettingsViewModel(Mod modJson, ModCardViewModel? modCard=null)
        {
            //this.mainWindowViewModel = mainWindowViewModel;
            this.modJson = modJson;
            modCardViewModel = modCard;
            isDevMode = modJson.devMode;
            compression = modJson.modSettings.isCompressed;
            if(Knossos.globalSettings.modCompression != CompressionSettings.Disabled && !modJson.modSettings.isCompressed)
            {
                compressionAvalible = true;
            }

            Title = modJson.title + " " + modJson.version + " Mod " + "Settings";
            CreateDependencyItems();

            if (modJson.modSettings.customDependencies != null)
            {
                CustomDependencies = true;
                CustomDependenciesClick();
            }
            
            if(modJson.modSettings.customBuildId == null)
            {
                fsoPicker = new FsoBuildPickerViewModel(null);
            }
            else
            {
                if (modJson.modSettings.customBuildExec != null)
                {
                    fsoPicker = new FsoBuildPickerViewModel(new FsoBuild(modJson.modSettings.customBuildExec));
                }
                else
                {
                    var matchingBuilds = Knossos.GetInstalledBuildsList(modJson.modSettings.customBuildId);
                    if (matchingBuilds.Any())
                    {
                        var theBuild = matchingBuilds.FirstOrDefault(build => build.version == modJson.modSettings.customBuildVersion);
                        if (theBuild != null)
                        {
                            fsoPicker = new FsoBuildPickerViewModel(theBuild);
                        }
                        else
                        {
                            Log.Add(Log.LogSeverity.Warning, "ModSettingsViewModel.Constructor()", "Missing user-saved build version for mod: " + modJson.tile + " - " + modJson.version + " requested build id: " + modJson.modSettings.customBuildId + " and version: " + modJson.modSettings.customBuildVersion);
                            fsoPicker = new FsoBuildPickerViewModel(null);
                        }
                    }
                    else
                    {
                        Log.Add(Log.LogSeverity.Warning, "ModSettingsViewModel.Constructor()", "Missing user-saved build id for mod: " + modJson.tile + " - " + modJson.version + " requested build id: " + modJson.modSettings.customBuildId);
                        fsoPicker = new FsoBuildPickerViewModel(null);
                    }
                }
            }

            //Size
            Task.Factory.StartNew(() => UpdateModSize());
        }

        private void UpdateModSize()
        {
            if(modJson == null || modJson.fullPath == string.Empty)
            {
                return;
            }

            try
            {
                long bytes = 0;
                var files = Directory.GetFiles(modJson.fullPath, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var fi = new FileInfo(file);
                    bytes += fi.Length;
                }
                ModSize = SysInfo.FormatBytes(bytes);
            }catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "ModSettingsViewModel.UpdateModSize()", ex);
            }
        }

        private void CreateDependencyItems(bool overrideSettings = false)
        {
            if (modJson != null)
            {
                var allDeps = modJson.GetModDependencyList(overrideSettings)?.ToList();
                foreach (var modid in modJson.GetModFlagList(overrideSettings))
                {
                    /* This mod */
                    if (modid == modJson.id)
                    {
                        DepItems.Add(new DependencyItemViewModel(modJson.id, this));
                    }
                    else
                    {
                        var dep = modJson.GetDependency(modid, overrideSettings);
                        if (dep != null)
                        {
                            if (allDeps != null && allDeps.IndexOf(dep) != -1)
                            {
                                allDeps.Remove(dep);
                            }
                            DepItems.Add(new DependencyItemViewModel(dep, this));
                        }
                    }
                }
                if (allDeps != null)
                {
                    /* Only the ones outside modflag should be left, what should be a FSO build*/
                    foreach (var dep in allDeps)
                    {
                        DepItems.Add(new DependencyItemViewModel(dep, this));
                    }
                }
            }
        }

        /* External Commands */
        public void DepUP(DependencyItemViewModel item)
        {
            int index = DepItems.IndexOf(item);
            if(index > 0)
            {
                DepItems.Move(index, index-1);
            }
        }

        public void DepDOW(DependencyItemViewModel item)
        {
            int index = DepItems.IndexOf(item);
            if (index < DepItems.Count-1)
            {
                DepItems.Move(index, index + 1);
            }
        }

        public void DepDEL(DependencyItemViewModel item)
        {
            DepItems.Remove(item);
        }


        /* UI */
        private void SaveSettingsCommand()
        {
            if(modJson!= null)
            {
                //Dependencies
                if (CustomDependencies)
                {
                    modJson.modSettings.customDependencies = new List<ModDependency>();
                    modJson.modSettings.customModFlags = new List<string>();

                    foreach (var item in DepItems)
                    {
                        var dep = item.GetModDependency();

                        if(dep != null)
                        {
                            if (dep.id != modJson.id)
                            {
                                modJson.modSettings.customDependencies.Add(dep);
                            }

                            /* Check if the ID is a FSO build, if not add to modflag */
                            var isBuild = Knossos.GetInstalledBuildsList(dep.id);
                            if(isBuild.Count==0)
                            {
                                modJson.modSettings.customModFlags.Add(dep.id);
                            }
                        }
                    }
                }
                else
                {
                    modJson.modSettings.customDependencies = null;
                    modJson.modSettings.customModFlags = null;
                }

                //FSO Build
                var customBuild = FsoPicker.GetSelectedFsoBuild();
                if (customBuild != null)
                {
                    modJson.modSettings.customBuildId = customBuild.id;
                    modJson.modSettings.customBuildVersion = customBuild.version;
                    modJson.modSettings.customBuildExec = customBuild.directExec;
                }
                else
                {
                    modJson.modSettings.customBuildId = null;
                    modJson.modSettings.customBuildVersion = null;
                    modJson.modSettings.customBuildExec = null;
                }

                //FSO Flags
                if(FsoFlags != null)
                {

                    if(FsoFlags.GetCmdLine() == modJson.GetModCmdLine(true))
                    {
                        modJson.modSettings.customCmdLine = null;
                    }
                    else
                    {
                        modJson.modSettings.customCmdLine = FsoFlags.GetCmdLine();
                    }
                }
                modJson.modSettings.Save();
                Knossos.globalSettings.Save(false);
                if(modCardViewModel != null)
                {
                    modCardViewModel.CheckDependencyActiveVersion();
                }
            }
        }

        private void ConfigureBuildCommand(bool ignoreUserSettings)
        {
            if (modJson == null)
                return;
            ConfigureBuildOpen = true;
            FsoBuild? fsoBuild = FsoPicker.GetSelectedFsoBuild();

            /* If set to "mod select" pick the one from the current dependency list displayed in this dialog */
            if (fsoBuild == null)
            {
                foreach (var item in DepItems)
                {
                    var dep = item.GetModDependency();

                    if (dep != null && Knossos.GetInstalledBuildsList(dep.id).Any())
                    {
                        fsoBuild = dep.SelectBuild();
                        break;
                    }
                }
            }

            if(fsoBuild != null)
            {
                var flagsV1=fsoBuild.GetFlagsV1();
                if(flagsV1 != null)
                {
                    FsoFlags = new FsoFlagsViewModel(flagsV1,modJson.GetModCmdLine(ignoreUserSettings));
                }
                else
                {
                    MessageBox.Show(MainWindow.instance!, "Unable to get flag data from build " + fsoBuild + " It might be below the minimal version supported (3.8.1) or some other error ocurred.", "Invalid flag data", MessageBox.MessageBoxButtons.OK);
                }
            }
            else
            {
                /* No valid build found, send message */
                MessageBox.Show(MainWindow.instance!, "Unable to resolve FSO build dependency, download the correct one or manually select a FSO version. ", "Not engine build found", MessageBox.MessageBoxButtons.OK);
            }
        }

        private void ResetSettingsCommand()
        {
            CustomDependencies = false;
            FsoPicker = new FsoBuildPickerViewModel(null);
            FsoFlags = null;
            ConfigureBuildCommand(true);
            DepItems.Clear();
            CreateDependencyItems(true);
        }

        private void CustomDependenciesClick()
        {

            if (CustomDependencies)
            {
                foreach(var items in DepItems)
                {
                    items.SetReadOnly(false);
                }
            }
            else
            {
                DepItems.Clear();
                CreateDependencyItems(true);
            }
        }

        private void AddDependencyCommand()
        {
            DepItems.Add(new DependencyItemViewModel(this));
        }

        private void CompressCommand()
        {
            //TODO: add FSO version check and warning first
            if(modJson != null && modJson.fullPath != string.Empty)
                TaskViewModel.Instance?.CompressMod(modJson);
        }

        private void DecompressCommand()
        {
            if (modJson != null && modJson.fullPath != string.Empty)
                TaskViewModel.Instance?.DecompressMod(modJson);
        }
    }
}
