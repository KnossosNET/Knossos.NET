using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BBcodes;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Knossos.NET.ViewModels
{
    public class ScreenshotItem
    {
        public Bitmap image { get; set; }
        public bool video { get; set; } = false;

        public string url { get; set; }

        public ScreenshotItem(Bitmap image, bool isVideo = false)
        {
            this.image = image;
            this.video = isVideo;
            this.url = string.Empty;
        }

        internal void OpenVideo(object url)
        {
            try
            {
                KnUtils.OpenBrowserURL((string)url);
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "ScreenshotItem.OpenVideo", ex);
            }
        }
    }

    public partial class ModDetailsViewModel : ViewModelBase
    {
        private List<Mod> modVersions = new List<Mod>();
        private ModCardViewModel? modCard = null;
        internal bool devMode { get; set; } = false;
        [ObservableProperty]
        internal string owners = string.Empty;
        /* UI Variables */
        [ObservableProperty]
        internal string name = string.Empty;
        [ObservableProperty]
        internal string? released = string.Empty;
        [ObservableProperty]
        internal string? lastUpdated = string.Empty;
        [ObservableProperty]
        internal string? description = string.Empty;
        [ObservableProperty]
        internal Bitmap? banner = null;
        [ObservableProperty]
        internal bool forumAvalible = true;
        [ObservableProperty]
        internal bool isInstalled = true;
        [ObservableProperty]
        internal bool isPlayingTTS = false;
        [ObservableProperty]
        internal bool ttsAvalible = true;
        [ObservableProperty]
        internal bool hasBanner = false;
        [ObservableProperty]
        internal bool isLocalMod = false;
        [ObservableProperty]
        internal ObservableCollection<ScreenshotItem> screenshots = new ObservableCollection<ScreenshotItem>();
        internal ObservableCollection<ComboBoxItem> VersionItems { get; set; } = new ObservableCollection<ComboBoxItem>();

        internal int itemSelectedIndex = 0;
        internal int ItemSelectedIndex
        {
            get
            {
                return itemSelectedIndex;
            }
            set
            {
                /* Workaround for the combo box selecting a -1 when deleting the selected item */
                if (value != -1)
                {
                    if (itemSelectedIndex != value)
                    {
                        itemSelectedIndex = value;
                        LoadVersion(value);
                    }
                    this.SetProperty(ref itemSelectedIndex, value);
                }
            }
        }



        /* Only used in preview */
        public ModDetailsViewModel()
        {
        }

        public ModDetailsViewModel(Mod modJson)
        {
            this.modVersions = new List<Mod>() { modJson };
            this.modCard = null;
            ItemSelectedIndex = 0;
            LoadVersion(0);
            devMode = modJson.devMode;
            if (Knossos.globalSettings.ttsDescription && Knossos.globalSettings.enableTts)
            {
                //PlayDescription(200);
            }
            else
            {
                TtsAvalible = false;
            }
        }

        public ModDetailsViewModel(List<Mod> modVersions, int selectedIndex, ModCardViewModel modCard)
        {
            this.modVersions = modVersions;
            this.modCard = modCard;
            foreach (var mod in modVersions)
            {
                var item = new ComboBoxItem();
                item.Content = mod.version;
                VersionItems.Add(item);
                devMode= mod.devMode;
            }
            //Data loads on selected index change
            ItemSelectedIndex = selectedIndex;

            if (modVersions.Count == 1)
                LoadVersion(0);

            if(Knossos.globalSettings.ttsDescription && Knossos.globalSettings.enableTts) 
            {
                //PlayDescription(200);
            }
            else
            {
                TtsAvalible = false;
            }
        }

        internal void PlayDescriptionCommand()
        {
            PlayDescription(0);
        }

        private async void PlayDescription(int delay = 0)
        {
            if(delay > 0)
            {
                await Task.Delay(delay);
            }
            IsPlayingTTS = true;
            var cleanDescriptionString = Regex.Replace(modVersions[ItemSelectedIndex].description!, @" ?\[.*?\]", string.Empty);
            cleanDescriptionString = Regex.Replace(cleanDescriptionString, @" ?\<.*?\>", string.Empty);
            Knossos.Tts(cleanDescriptionString, null, null, null, CompletedCallback);
        }

        private bool CompletedCallback()
        {
            IsPlayingTTS = false;
            return true;
        }

        private async void LoadVersion(int index)
        {
            try
            {
                Name = modVersions[index].title;
                IsLocalMod = modVersions[index].modSource == ModSource.local? true : false;
                LastUpdated = modVersions[index].lastUpdate;
                if(!IsLocalMod && !modVersions[index].installed)
                {
                    await modVersions[index].LoadFulLNebulaData();
                }
                if (modVersions[index].description != null)
                {
                    var html = BBCode.ConvertToHtml(modVersions[index].description!, BBCode.BasicRules);
                    Description = "<body style=\"overflow: hidden;\"><span style=\"white-space: pre-line;color:white;overflow: hidden;\">" + html + "</span></body>";
                    //Log.WriteToConsole(html);
                }
                if (modVersions[index].owners != null && modVersions[index].owners!.Any()) 
                {
                    Owners = string.Join(", ", modVersions[index].owners!);
                }
                else
                {
                    Owners = string.Empty;
                }
                Released = modVersions[index].firstRelease;
                IsInstalled = modVersions[index].installed;
                if (modVersions[index].releaseThread != null)
                {
                    ForumAvalible = true;
                }
                else
                {
                    ForumAvalible = false;
                }
                LoadBanner(index);
                LoadScreenshots(index);
                modCard?.SwitchModVersion(index);
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "ModDetailsViewModel.LoadVersion()", ex);
            }
        }

        private void LoadBanner(int selectedIndex)
        {
            try
            {
                if (!string.IsNullOrEmpty(modVersions[selectedIndex].banner))
                {
                    HasBanner = true;
                    if (System.IO.File.Exists(modVersions[selectedIndex].fullPath + Path.DirectorySeparatorChar + modVersions[selectedIndex].banner))
                    {
                        Banner = new Bitmap(modVersions[selectedIndex].fullPath + Path.DirectorySeparatorChar + modVersions[selectedIndex].banner);
                    }
                    else
                    {
                        var url = modVersions[selectedIndex].banner;
                        if (url != null && url.ToLower().Contains("http"))
                        {
                            Banner?.Dispose();
                            Banner = new Bitmap(AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/general/loading.png")));
                            DownloadImage(url);
                        }
                    }
                }
                else
                {
                    HasBanner = false;
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "ModDetailsViewModel.LoadImage", ex);
            }
        }

        private void LoadScreenshots(int selectedIndex)
        {
            try
            {
                Screenshots.Clear();
                //Add Videos
                if(modVersions[selectedIndex].videos != null && modVersions[selectedIndex].screenshots!.Length > 0)
                {
                    foreach (var vid in modVersions[selectedIndex].videos!)
                    {
                        try
                        {
                            DownloadVideoThumbnail(vid);
                        }
                        catch (Exception ex)
                        {
                            Log.Add(Log.LogSeverity.Warning, "ModDetailsViewModel.LoadScreenshots", ex);
                        }
                    }
                }

                if (modVersions[selectedIndex].screenshots != null && modVersions[selectedIndex].screenshots!.Length > 0 )
                {
                    foreach (var scn in modVersions[selectedIndex].screenshots!)
                    {
                        try
                        {
                            if (System.IO.File.Exists(modVersions[selectedIndex].fullPath + Path.DirectorySeparatorChar + scn))
                            {
                                var bitmap = new Bitmap(modVersions[selectedIndex].fullPath + Path.DirectorySeparatorChar + scn);
                                var item = new ScreenshotItem(bitmap);
                                Screenshots.Add(item);
                            }
                            else
                            {
                                if (scn.ToLower().Contains("http"))
                                {
                                    DownloadImage(scn, true);
                                }
                            }
                        }catch (Exception ex)
                        {
                            Log.Add(Log.LogSeverity.Warning, "ModDetailsViewModel.LoadScreenshots", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "ModDetailsViewModel.LoadScreenshots", ex);
            }
        }

        private async void DownloadImage(string url, bool screenshoot = false)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    HttpResponseMessage response = await client.GetAsync(url);
                    byte[] content = await response.Content.ReadAsByteArrayAsync();
                    Stream stream = new MemoryStream(content);
                    if (!screenshoot)
                        Banner = new Bitmap(stream);
                    else
                    {
                        var item = new ScreenshotItem(new Bitmap(stream));
                        Screenshots.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "ModDetailsViewModel.DownloadImage", ex);
            }
        }

        private async void DownloadVideoThumbnail(string url)
        {
            try
            {
                var imageUrl = string.Empty;

                if (url.ToLower().Contains("youtu"))
                {
                    string? id = GetYouTubeVideoId(new Uri(url));
                    if(id != null)
                        imageUrl = "https://img.youtube.com/vi/" + id + "/hqdefault.jpg";
                }


                if (imageUrl != string.Empty)
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(5);
                        HttpResponseMessage response = await client.GetAsync(imageUrl);
                        byte[] content = await response.Content.ReadAsByteArrayAsync();
                        Stream stream = new MemoryStream(content);
                        var item = new ScreenshotItem(new Bitmap(stream), true);
                        item.url = url;
                        Screenshots.Add(item);
                    }
                }
                else
                {
                    var item = new ScreenshotItem(new Bitmap(AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/general/loading.png"))), true);
                    item.url = url;
                    Screenshots.Add(item);
                }
            }
            catch (Exception ex)
            {
                var item = new ScreenshotItem(new Bitmap(AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/general/loading.png"))), true);
                item.url = url;
                Screenshots.Add(item);
                Log.Add(Log.LogSeverity.Warning, "ModDetailsViewModel.DownloadVideoThumbnail", ex);
            }
        }

        private string? GetYouTubeVideoId(Uri uri)
        {
            var query = HttpUtility.ParseQueryString(uri.Query);

            if (query.AllKeys.Contains("v"))
            {
                return Regex.Match(query["v"]!, @"^[a-zA-Z0-9_-]{11}$").Value;
            }
            else if (query.AllKeys.Contains("u"))
            {
                return Regex.Match(query["u"]!, @"/watch\?v=([a-zA-Z0-9_-]{11})").Groups[1].Value;
            }
            else
            {
                var last = uri.Segments.Last().Replace("/", "");
                if (Regex.IsMatch(last, @"^v=[a-zA-Z0-9_-]{11}$"))
                    return last.Replace("v=", "");

                string[] segments = uri.Segments;
                if (segments.Length > 2 && segments[segments.Length - 2] != "v/" && segments[segments.Length - 2] != "watch/")
                    return "";

                return Regex.Match(last, @"^[a-zA-Z0-9_-]{11}$").Value;
            }
        }

        /* Button Commands */
        internal void StopTts()
        {
            IsPlayingTTS = false;
            Knossos.Tts(string.Empty);
        }

        internal void ButtonCommandPlay()
        {
            Knossos.PlayMod(modVersions[ItemSelectedIndex], FsoExecType.Release);
        }

        internal void ButtonCommandPlayDebug()
        {
            Knossos.PlayMod(modVersions[ItemSelectedIndex], FsoExecType.Debug);
        }

        internal void ButtonCommandFred2()
        {
            Knossos.PlayMod(modVersions[ItemSelectedIndex], FsoExecType.Fred2);
        }

        internal void ButtonCommandQtFred()
        {
            Knossos.PlayMod(modVersions[ItemSelectedIndex], FsoExecType.QtFred);
        }

        internal async void ButtonCommandSettings()
        {
            if (MainWindow.instance != null)
            {
                var dialog = new ModSettingsView();
                dialog.DataContext = new ModSettingsViewModel(modVersions[ItemSelectedIndex]);

                await dialog.ShowDialog<ModSettingsView?>(MainWindow.instance);
            }
        }

        internal async void ButtonCommandDelete(object window)
        {
            if (!modVersions[ItemSelectedIndex].devMode)
            {
                if (TaskViewModel.Instance!.IsSafeState())
                {
                    if (modVersions.Count > 1)
                    {
                        var resp = await MessageBox.Show(MainWindow.instance!, "You are about to delete version " + modVersions[ItemSelectedIndex].version + " of this mod, this will remove this version only. Do you want to continue?", "Delete mod version", MessageBox.MessageBoxButtons.YesNo);
                        if (resp == MessageBox.MessageBoxResult.Yes)
                        {
                            var delete = modVersions[ItemSelectedIndex];
                            var verDel = VersionItems[ItemSelectedIndex];
                            modVersions.Remove(delete);
                            Knossos.RemoveMod(delete);
                            ItemSelectedIndex = modVersions.Count - 1;
                            VersionItems.Remove(verDel);
                            MainWindowViewModel.Instance?.RunModStatusChecks();
                        }
                    }
                    else
                    {
                        var resp = await MessageBox.Show(MainWindow.instance!, "You are about to delete the last installed version of this mod. Do you want to continue?", "Delete mod version", MessageBox.MessageBoxButtons.YesNo);
                        if (resp == MessageBox.MessageBoxResult.Yes)
                        {
                            //Last version
                            modVersions[0].installed = false;
                            if (modVersions[0].modSource != ModSource.local)
                            {
                                MainWindowViewModel.Instance?.AddNebulaMod(modVersions[0]);
                            }
                            Knossos.RemoveMod(modVersions[0].id);
                            MainWindowViewModel.Instance?.RunModStatusChecks();
                            if (window != null)
                            {
                                var w = (ModDetailsView)window;
                                w.Close();
                            }
                        }
                    }
                }
                else
                {
                    await MessageBox.Show(MainWindow.instance!, "You can not delete a mod while other install tasks are running, wait until they finish and try again.", "Tasks are running", MessageBox.MessageBoxButtons.OK);
                }
            }
            else
            {
                await MessageBox.Show(MainWindow.instance!, "Dev mode mods can not be delated from the main view, go to the Development section.", "Mod is dev mode", MessageBox.MessageBoxButtons.OK);
            }
        }

        internal void ButtonCommandForum()
        {
            var url = modVersions[ItemSelectedIndex].releaseThread;
            if (url != null)
            {
                KnUtils.OpenBrowserURL(url);
            }
        }

        internal async void ButtonCommandReport()
        {
            if (!Nebula.userIsLoggedIn)
            {
                await MessageBox.Show(MainWindow.instance!, "You need to be logged to Nebula (in the Develop tab) to upload a report.", "Nebula loggin needed", MessageBox.MessageBoxButtons.OK);
                return;
            }
            var dialog = new ReportModView();
            dialog.DataContext = new ReportModViewModel(modVersions[ItemSelectedIndex]);

            await dialog.ShowDialog<ReportModView?>(MainWindow.instance!);
        }
    }
}
