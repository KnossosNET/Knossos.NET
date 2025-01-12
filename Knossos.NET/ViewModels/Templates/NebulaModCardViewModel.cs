using Avalonia.Platform;
using Avalonia.Media.Imaging;
using System;
using Knossos.NET.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Views;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
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
        internal bool visible = false;
        internal bool Visible
        {
            get 
            { 
                return visible; 
            }
            set
            {
                if(visible != value)
                {
                    SetProperty(ref visible, value);
                    if(value && TileImage == null && modJson != null)
                    {
                        Dispatcher.UIThread.Invoke(() => {
                            LoadImage(modJson.fullPath, modJson.tile);
                        });
                    }
                }
            }
        }
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
            //Moved to load when visible only
            //LoadImage(modJson.fullPath, modJson.tile);
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
                            using (var fs = await KnUtils.GetRemoteResourceStream(tileString).ConfigureAwait(false))
                            {
                                Dispatcher.UIThread.Invoke(() =>
                                {
                                    if (fs != null)
                                        TileImage = new Bitmap(fs);
                                });
                            }
                        }).ConfigureAwait(false); 
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
