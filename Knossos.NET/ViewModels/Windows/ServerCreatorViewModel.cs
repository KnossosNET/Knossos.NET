using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
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
                    modIndex = value;
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

        [ObservableProperty]
        public int buildType = 0;

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

        internal void OpenCFG()
        {
            try
            {
                if (!File.Exists(Path.Combine(ListOfMods[ModIndex].fullPath, "data", "multi.cfg")))
                {
                    var f = File.Create(Path.Combine(ListOfMods[ModIndex].fullPath, "data", "multi.cfg"));
                    f.Close();
                }
                var cmd = new Process();
                cmd.StartInfo.FileName = Path.Combine(ListOfMods[ModIndex].fullPath, "data", "multi.cfg");
                cmd.StartInfo.UseShellExecute = true;
                cmd.Start();
                cmd.Dispose();
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "ServerCreatorViewModel.OpenCFG", ex);
            }
        }

        internal void LaunchServer()
        {
            if (BuildType < 1)
                Knossos.PlayMod(ListOfMods[ModIndex], FsoExecType.Release, true, Cfg.Port);
            else
                Knossos.PlayMod(ListOfMods[ModIndex], FsoExecType.Debug, true, Cfg.Port);

        }

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
