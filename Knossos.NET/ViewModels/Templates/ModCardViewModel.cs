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

namespace Knossos.NET.ViewModels
{
    public partial class ModCardViewModel : ViewModelBase
    {
        /* General Mod variables */
        private List<Mod> modVersions = new List<Mod>();
        private int activeVersionIndex = 0;
        private CancellationTokenSource? cancellationTokenSource = null;
        public string ID { get; set; }

        /* UI Bindings */
        [ObservableProperty]
        private string? name;
        [ObservableProperty]
        private string? modVersion;
        [ObservableProperty]
        private Bitmap? image;
        [ObservableProperty]
        private bool visible = true;
        [ObservableProperty]
        private bool updateAvalible = false;
        [ObservableProperty]
        private bool isInstalled = false;
        [ObservableProperty]
        private IBrush borderColor = Brushes.Green;
        [ObservableProperty]
        private bool buttonPage1 = true;
        [ObservableProperty]
        private string? tooltip;
        [ObservableProperty]
        private bool isInstalling = false;


        /* Should only be used by the editor preview */
        public ModCardViewModel()
        {
            Name = "default test string very long";
            ModVersion = "1.0.0";
            ID = "test";
            var assets = Avalonia.AvaloniaLocator.Current.GetService<IAssetLoader>();
            if (assets != null)
            {
                Image = new Bitmap(assets.Open(new Uri("avares://Knossos.NET/Assets/NebulaDefault.png")));
            }
        }

        public ModCardViewModel(Mod modJson)
        {
            Log.Add(Log.LogSeverity.Information, "ModCardViewModel(Constructor)", "Creating mod card for " + modJson.title +" "+ modJson.version);
            modJson.ClearUnusedData();
            modVersions.Add(modJson);
            Name = modJson.title;
            ModVersion = modJson.version;
            ID = modJson.id;
            IsInstalled = modJson.installed;
            if (modJson.description != null)
            {
                if (modJson.description.Length > 500)
                {
                    Tooltip = Regex.Replace(modJson.description.Substring(0, 500) + "\n...", @" ?\[.*?\]", string.Empty);

                }
                else
                {
                    Tooltip = Regex.Replace(modJson.description, @" ?\[.*?\]", string.Empty);
                }
            }
            LoadImage();
        }
        
        public void AddModVersion(Mod modJson)
        {
            modJson.ClearUnusedData();
            Log.Add(Log.LogSeverity.Information, "ModCardViewModel.AddModVersion()", "Adding additional version for mod id: " + ID + " -> " + modJson.folderName);
            modVersions.Add(modJson);
            if (SemanticVersion.Compare(modJson.version, modVersions[activeVersionIndex].version) > 0)
            {
                Log.Add(Log.LogSeverity.Information, "ModCardViewModel.AddModVersion()", "Changing active version for this mod from " + modVersions[activeVersionIndex].version + " to " + modJson.version);
                activeVersionIndex = modVersions.Count - 1;
                Name = modJson.title;
                ModVersion = modJson.version + " (+" + (modVersions.Count - 1) + ")";
                LoadImage();
            }
        }

        public void SwitchModVersion(int newIndex)
        {
            if (newIndex != activeVersionIndex)
            {
                Log.Add(Log.LogSeverity.Information, "ModCardViewModel.SwitchModVersion()", "Changing active version for mod id " + ID + " to " + modVersions[newIndex].version);
                activeVersionIndex = newIndex;
                Name = modVersions[newIndex].title;
                ModVersion = modVersions[newIndex].version + " (+" + (modVersions.Count - 1) + ")";
                if (modVersions[newIndex].description != null)
                {
                    if (modVersions[newIndex].description!.Length > 500)
                    {
                        Tooltip = Regex.Replace(modVersions[newIndex].description!.Substring(0, 500) + "\n...", @" ?\[.*?\]", string.Empty);

                    }
                    else
                    {
                        Tooltip = Regex.Replace(modVersions[newIndex].description!, @" ?\[.*?\]", string.Empty);
                    }
                }
                LoadImage();
            }
        }


        /* Button Commands */
        private void ButtonCommandPlay()
        {
            Knossos.PlayMod(modVersions[activeVersionIndex],FsoExecType.Release);
        }

        private void ButtonCommandFred2()
        {
            Knossos.PlayMod(modVersions[activeVersionIndex], FsoExecType.Fred2);
        }

        private void ButtonCommandDebug()
        {
            Knossos.PlayMod(modVersions[activeVersionIndex], FsoExecType.Debug);
        }

        private void ButtonCommandQtFred() 
        {
            Knossos.PlayMod(modVersions[activeVersionIndex], FsoExecType.QtFred);
        }

        private void ButtonCommandUpdate()
        {

        }

        private async void ButtonCommandInstall()
        {
            var dialog = new ModInstallView();
            dialog.DataContext = new ModInstallViewModel(modVersions[0]);
            await dialog.ShowDialog<ModInstallView?>(MainWindow.instance);
        }

        public void InstallingMod(CancellationTokenSource cancelToken)
        {
            IsInstalling = true;
            cancellationTokenSource = cancelToken;
        }

        public void CancelInstall()
        {
            IsInstalling = false;
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = null;
        }

        private async void ButtonCommandDelete()
        {
            if(MainWindow.instance!= null)
            {
                var result = await MessageBox.Show(MainWindow.instance, "You are going to remove mod " + Name + " .This will delete ALL versions of the mod. If you only want to delete a specific version you can do it from mod details.\n Do you really want to delete the mod?", "Delete mod", MessageBox.MessageBoxButtons.OKCancel);
                if(result == MessageBox.MessageBoxResult.OK)
                {
                    Knossos.RemoveMod(modVersions[activeVersionIndex].id);
                }
            }
        }

        private void ButtonCommandGoPage2()
        {
            ButtonPage1 = false;
        }

        private void ButtonCommandGoPage1()
        {
            ButtonPage1 = true;
        }

        private async void ButtonCommandDetails()
        {
            if (MainWindow.instance != null)
            {
                var dialog = new ModDetailsView();
                dialog.DataContext = new ModDetailsViewModel(modVersions,activeVersionIndex,this);

                await dialog.ShowDialog<ModDetailsView?>(MainWindow.instance);
            }
        }

        private async void ButtonCommandSettings()
        {
            if (MainWindow.instance != null)
            {
                var dialog = new ModSettingsView();
                dialog.DataContext = new ModSettingsViewModel(modVersions[activeVersionIndex]);

                await dialog.ShowDialog<ModSettingsView?>(MainWindow.instance);
            }
        }

        private void LoadImage()
        {
            Image?.Dispose();
            var assets = Avalonia.AvaloniaLocator.Current.GetService<IAssetLoader>();
            if (assets != null)
            {
                Image = new Bitmap(assets.Open(new Uri("avares://Knossos.NET/Assets/NebulaDefault.png")));
            }

            try
            {
                var tile = modVersions[activeVersionIndex].tile;
                if (!string.IsNullOrEmpty(tile))
                {
                    if (!tile.ToLower().Contains("http"))
                    {
                        Image = new Bitmap(modVersions[activeVersionIndex].fullPath + @"\" + tile);
                    }
                    else
                    {
                        Task.Run(() => DownloadImage(tile));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "ModCardViewModel.LoadImage", ex);
            }
        }

        private async void DownloadImage(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    byte[] content = await response.Content.ReadAsByteArrayAsync();
                    await using (Stream stream = new MemoryStream(content))
                    {
                        
                        Image = new Bitmap(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "ModCardViewModel.DownloadImage", ex);
            }
        }
    }
}
