using Avalonia;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using System;
using Knossos.NET.Models;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using Knossos.NET.Classes;
using Knossos.NET.Views;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;
using Avalonia.Threading;

namespace Knossos.NET.ViewModels
{
    public partial class NebulaModCardViewModel : ViewModelBase
    {
        private CancellationTokenSource? cancellationTokenSource = null;
        public Mod? modJson { get; set; }
        public string? ID { get { return modJson != null ? modJson.id : null; } }

        /* UI Bindings */
        internal string? Name { get { return modJson != null ? modJson.title : null; } }
        internal string? ModVersion { get { return modJson != null ? modJson.version : null; } }
        [ObservableProperty]
        internal Bitmap? tileImage;
        [ObservableProperty]
        internal bool visible = true;
        [ObservableProperty]
        internal bool isInstalling = false;


        /* Should only be used by the editor preview */
        public NebulaModCardViewModel()
        {
            modJson = new Mod();
            modJson.title = "default test string very long";
            modJson.version = "1.0.0";
            modJson.id = "test";
            TileImage = new Bitmap(AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/general/NebulaDefault.png")));
        }

        public NebulaModCardViewModel(Mod modJson)
        {
            Log.Add(Log.LogSeverity.Information, "NebulaModCardViewModel(Constructor)", "Creating mod card for " + modJson);
            modJson.ClearUnusedData();
            this.modJson = modJson;
            LoadImage(modJson.fullPath,modJson.tile);
        }

        /* Button Commands */
        internal void ButtonCommand(object command)
        {
            switch((string)command)
            {
                case "details": ButtonCommandDetails(); break;
                case "install": ButtonCommandInstall(); break;
                case "cancel": CancelInstallCommand(); break;
            }
        }

        internal async void ButtonCommandInstall()
        {
            if (MainWindow.instance != null && ModVersion != null)
            {
                if (modJson != null)
                {
                    var dialog = new ModInstallView();
                    dialog.DataContext = new ModInstallViewModel(modJson, dialog);
                    await dialog.ShowDialog<ModInstallView?>(MainWindow.instance!);
                }
            }
        }

        public void InstallingMod(CancellationTokenSource cancelToken)
        {
            IsInstalling = true;
            cancellationTokenSource = cancelToken;
        }

        public void CancelInstall()
        {
            IsInstalling = false;
            cancellationTokenSource = null;
        }

        public void CancelInstallCommand()
        {
            IsInstalling = false;
            try
            {
                cancellationTokenSource?.Cancel();
            }
            catch { }
            cancellationTokenSource = null;
            TaskViewModel.Instance?.CancelAllInstallTaskWithID(ID!, ModVersion);
        }

        internal async void ButtonCommandDetails()
        {
            if (MainWindow.instance != null && ModVersion != null)
            {
                if(modJson != null)
                {
                    var dialog = new ModDetailsView();
                    dialog.DataContext = new ModDetailsViewModel(modJson, dialog);
                    await dialog.ShowDialog<ModDetailsView?>(MainWindow.instance);
                }
            }
        }

        private void LoadImage(string modFullPath, string? tileString)
        {
            TileImage?.Dispose();
            TileImage = new Bitmap(AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/general/NebulaDefault.png")));

            try
            {
                if (!string.IsNullOrEmpty(tileString))
                {
                    if (!tileString.ToLower().Contains("http"))
                    {
                        TileImage = new Bitmap(modFullPath + Path.DirectorySeparatorChar + tileString);
                    }
                    else
                    {
                        Task.Run(async () =>
                        {
                            using (var fs = await KnUtils.GetImageStream(tileString))
                            {
                                if(fs != null)
                                    TileImage = new Bitmap(fs);
                            }
                        }); 
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "NebulaModCardViewModel.LoadImage", ex);
            }
        }
    }
}
