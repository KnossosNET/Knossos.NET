using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Knossos.NET.ViewModels
{
    /// <summary>
    /// View Model for the Server Creator View
    /// </summary>
    public partial class ServerCreatorViewModel : ViewModelBase
    {
        [ObservableProperty]
        internal List<Mod> listOfMods = Knossos.GetInstalledModList(null);
        internal int modIndex = 0;
        internal int ModIndex
        {
            get
            {
                return modIndex;
            }
            set
            {
                if (modIndex != value)
                {
                    this.SetProperty(ref modIndex, value);
                    LoadCFG();
                }
            }
        }

        internal MultiCfg Cfg = new MultiCfg();

        [ObservableProperty]
        internal string extraOptions = string.Empty;

        [ObservableProperty]
        internal string banList = string.Empty;

        [ObservableProperty]
        internal string pxoChannel = "#Eleh";

        [ObservableProperty]
        internal bool usePxo = false;

        [ObservableProperty]
        public bool noVoice  = false;

        [ObservableProperty]
        public int port = 0;

        [ObservableProperty]
        public string? name = null;

        [ObservableProperty]
        public string? password = null;

        /// <summary>
        /// 0 = Release
        /// 1 = Debug
        /// </summary>
        [ObservableProperty]
        public int buildType = 0;

        /// <summary>
        /// Load multi.cfg file for the currently selected mod in the list
        /// </summary>
        internal void LoadCFG()
        {
            BanList = ExtraOptions = string.Empty;
            Name = Password = null;
            UsePxo = NoVoice = false;
            Port = BuildType = 0;
            PxoChannel = "#Eleh";
            try
            {
                if (ModIndex >= 0)
                {
                    Cfg.LoadData(ListOfMods[ModIndex]);
                    UsePxo = Cfg.UsePXO;
                    Name = Cfg.Name;
                    Password = Cfg.Password;
                    Port = Cfg.Port;
                    NoVoice = Cfg.NoVoice;
                    if (Cfg.UsePXO && Cfg.PXOChannel != null)
                    {
                        PxoChannel = Cfg.PXOChannel;
                    }
                    if(Cfg.Ban.Any())
                    {
                        BanList = string.Join(Environment.NewLine, Cfg.Ban);
                    }
                    if (Cfg.Others.Any())
                    {
                        ExtraOptions = string.Join(Environment.NewLine, Cfg.Others);
                    }
                }
            }catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "ServerCreatorViewModel.LoadCFG()", ex);
            }
        }

        /// <summary>
        /// Save multi.cfg for the currently selected mod 
        /// </summary>
        internal void SaveCFG()
        {
            try
            {
                if (ModIndex >= 0)
                {
                    Cfg.UsePXO = UsePxo;
                    Cfg.Name = Name;
                    Cfg.Password = Password;
                    Cfg.Port = Port;
                    Cfg.NoVoice = NoVoice;
                    Cfg.Ban.Clear();
                    Cfg.Others.Clear();
                    if (Cfg.UsePXO && PxoChannel.Trim() != string.Empty)
                    {
                        Cfg.PXOChannel = PxoChannel;
                    }
                    if (BanList.Trim() != string.Empty)
                    {
                        Cfg.Ban = BanList.Split(Environment.NewLine,StringSplitOptions.RemoveEmptyEntries).ToList();
                    }
                    if (ExtraOptions.Trim() != string.Empty)
                    {
                        Cfg.Others = ExtraOptions.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();
                    }
                    Cfg.SaveData(ListOfMods[ModIndex]);
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "ServerCreatorViewModel.SaveCFG()", ex);
            }
        }

        /// <summary>
        /// Open multi.cfg for the currently selected mod in a external text editor
        /// </summary>
        internal void OpenCFG()
        {
            try
            {
                if (!File.Exists(Path.Combine(ListOfMods[ModIndex].fullPath, "data", "multi.cfg")))
                {
                    var f = File.Create(Path.Combine(ListOfMods[ModIndex].fullPath, "data", "multi.cfg"));
                    f.Close();
                }
                using (var cmd = new Process())
                {
                    cmd.StartInfo.FileName = Path.Combine(ListOfMods[ModIndex].fullPath, "data", "multi.cfg");
                    cmd.StartInfo.UseShellExecute = true;
                    cmd.Start();
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "ServerCreatorViewModel.OpenCFG", ex);
            }
        }

        /// <summary>
        /// Open the dedicated server
        /// </summary>
        internal void LaunchServer()
        {
            if (BuildType < 1)
                Knossos.PlayMod(ListOfMods[ModIndex], FsoExecType.Release, true, Cfg.Port);
            else
                Knossos.PlayMod(ListOfMods[ModIndex], FsoExecType.Debug, true, Cfg.Port);

        }

        /// <summary>
        /// Open mod settings
        /// </summary>
        internal async void OpenModSettings()
        {
            if (MainWindow.instance != null)
            {
                var dialog = new ModSettingsView();
                dialog.DataContext = new ModSettingsViewModel(ListOfMods[ModIndex]);
                await dialog.ShowDialog<ModSettingsView?>(MainWindow.instance);
            }
        }
    }
}
