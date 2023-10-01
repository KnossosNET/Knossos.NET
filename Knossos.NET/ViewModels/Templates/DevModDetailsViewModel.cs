using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
    public partial class DevModDetailsViewModel : ViewModelBase
    {
        private DevModEditorViewModel? editor;
        [ObservableProperty]
        private string modName = string.Empty;
        [ObservableProperty]
        private string modId = string.Empty;
        [ObservableProperty]
        private string? modType = string.Empty;
        [ObservableProperty]
        private string? modParent = string.Empty;
        [ObservableProperty]
        private string modDescription = string.Empty;
        [ObservableProperty]
        private string? modForumLink = string.Empty;
        [ObservableProperty]
        private string? modVideos = string.Empty;
        [ObservableProperty]
        private Bitmap? tileImage;
        [ObservableProperty]
        private string? tileImagePath;
        [ObservableProperty]
        private Bitmap? bannerImage;
        [ObservableProperty]
        private string? bannerImagePath;

        public DevModDetailsViewModel() 
        { 
        }

        public DevModDetailsViewModel(DevModEditorViewModel devModEditorViewModel)
        {
            editor = devModEditorViewModel;
            ModName = editor.ActiveVersion.title;
            ModId = editor.ActiveVersion.id;
            ModType = editor.ActiveVersion.type.ToString();
            ModParent = editor.ActiveVersion.parent;
            ModDescription = editor.ActiveVersion.description != null ? editor.ActiveVersion.description : string.Empty;
            ModForumLink = editor.ActiveVersion.releaseThread;
            if(editor.ActiveVersion.videos != null && editor.ActiveVersion.videos.Length > 0)
            {
                ModVideos = String.Join(Environment.NewLine, editor.ActiveVersion.videos);
            }
            TileImagePath = editor.ActiveVersion.tile;
            LoadTileImage();
            BannerImagePath = editor.ActiveVersion.banner;
            LoadBannerImage();
        }

        internal void Save()
        {
            if(editor != null)
            {
                editor.ActiveVersion.title = ModName;
                editor.ActiveVersion.description = ModDescription;
                editor.ActiveVersion.releaseThread = ModForumLink;
                if(ModVideos != null && ModVideos.Trim() != string.Empty)
                {
                    editor.ActiveVersion.videos = ModVideos.Split(Environment.NewLine,StringSplitOptions.RemoveEmptyEntries);
                }
                editor.ActiveVersion.tile = TileImagePath;
                editor.ActiveVersion.banner = BannerImagePath;
                editor.ActiveVersion.SaveJson();
            }
        }

        internal void OpenDescriptionEditor()
        {

        }

        internal async void ChangeTileImage()
        {
            try
            {
                //get file
                FilePickerOpenOptions options = new FilePickerOpenOptions();
                options.AllowMultiple = false;

                var result = await MainWindow.instance!.StorageProvider.OpenFilePickerAsync(options);

                if (editor != null && result != null && result.Count > 0)
                {
                    var filepath = result[0].Path.LocalPath.ToString();

                    //Get File Hash (new filename)
                    using (FileStream? file = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                    {
                        var filename = string.Empty;
                        using (SHA256 checksum = SHA256.Create())
                        {
                            filename = BitConverter.ToString(await checksum.ComputeHashAsync(file)).Replace("-", String.Empty);
                        }
                        //Copy to new location
                        Directory.CreateDirectory(Path.Combine(editor.ActiveVersion.fullPath, "kn_images"));

                        var extension = Path.GetExtension(filepath);

                        using (FileStream? dest = new FileStream(editor.ActiveVersion.fullPath + Path.DirectorySeparatorChar + "kn_images" + Path.DirectorySeparatorChar + filename + extension, FileMode.Create, FileAccess.ReadWrite))
                        {
                            file.Position = 0;
                            await file.CopyToAsync(dest);
                            dest.Close();
                        }
                        file.Close();
                        TileImagePath = "kn_images" + Path.DirectorySeparatorChar + filename + extension;
                        LoadTileImage();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "DevModDetailsViewModel.ChangeTileImage", ex);
            }
        }

        internal void RemoveTileImage()
        {
            TileImagePath = null;
            TileImage?.Dispose();
            TileImage = new Bitmap(AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/general/NebulaDefault.png")));
        }

        internal async void ChangeBannerImage()
        {
            try
            {
                //get file
                FilePickerOpenOptions options = new FilePickerOpenOptions();
                options.AllowMultiple = false;

                var result = await MainWindow.instance!.StorageProvider.OpenFilePickerAsync(options);

                if (editor != null && result != null && result.Count > 0)
                {
                    var filepath = result[0].Path.LocalPath.ToString();

                    //Get File Hash (new filename)
                    using (FileStream? file = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                    {
                        var filename = string.Empty;
                        using (SHA256 checksum = SHA256.Create())
                        {
                            filename = BitConverter.ToString(await checksum.ComputeHashAsync(file)).Replace("-", String.Empty);
                        }
                        //Copy to new location
                        Directory.CreateDirectory(Path.Combine(editor.ActiveVersion.fullPath, "kn_images"));

                        var extension = Path.GetExtension(filepath);

                        using (FileStream? dest = new FileStream(editor.ActiveVersion.fullPath + Path.DirectorySeparatorChar + "kn_images" + Path.DirectorySeparatorChar + filename + extension, FileMode.Create, FileAccess.ReadWrite))
                        {
                            file.Position = 0;
                            await file.CopyToAsync(dest);
                            dest.Close();
                        }
                        file.Close();
                        BannerImagePath = "kn_images" + Path.DirectorySeparatorChar + filename + extension;
                        LoadBannerImage();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "DevModDetailsViewModel.ChangeTileImage", ex);
            }
        }

        internal void RemoveBannerImage()
        {
            BannerImagePath = null;
            BannerImage?.Dispose();
            BannerImage = null;
        }

        private void LoadBannerImage()
        {
            try
            {
                if (!string.IsNullOrEmpty(BannerImagePath))
                {
                    if (!BannerImagePath.ToLower().Contains("http") && editor != null)
                    {
                        BannerImage = new Bitmap(editor.ActiveVersion.fullPath + Path.DirectorySeparatorChar + BannerImagePath);
                    }
                    else
                    {
                        Task.Run(async () => {
                            using (HttpClient client = new HttpClient())
                            {
                                HttpResponseMessage response = await client.GetAsync(TileImagePath);
                                byte[] content = await response.Content.ReadAsByteArrayAsync();
                                using (Stream stream = new MemoryStream(content))
                                {
                                    BannerImage = new Bitmap(stream);
                                }
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "DevModDetailsViewModel.LoadBannerImage", ex);
            }
        }

        private void LoadTileImage()
        {
            try
            {
                if (!string.IsNullOrEmpty(TileImagePath))
                {
                    if (!TileImagePath.ToLower().Contains("http") && editor != null)
                    {
                        TileImage = new Bitmap(editor.ActiveVersion.fullPath + Path.DirectorySeparatorChar + TileImagePath);
                    }
                    else
                    {
                        Task.Run( async () => { 
                            using (HttpClient client = new HttpClient())
                            {
                                HttpResponseMessage response = await client.GetAsync(TileImagePath);
                                byte[] content =  await response.Content.ReadAsByteArrayAsync();
                                using (Stream stream = new MemoryStream(content))
                                {
                                    TileImage = new Bitmap(stream);
                                }
                            }
                        });
                    }
                }
                else
                {
                    TileImage?.Dispose();
                    TileImage = new Bitmap(AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/general/NebulaDefault.png")));
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "DevModDetailsViewModel.LoadTileImage", ex);
            }
        }
    }
}
