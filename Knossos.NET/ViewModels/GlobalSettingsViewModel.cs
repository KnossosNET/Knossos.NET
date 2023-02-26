

using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.Views;
using Knossos.NET.Views.Windows;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Knossos.NET.ViewModels
{
    public partial class GlobalSettingsViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool flagDataLoaded = false;
        [ObservableProperty]
        private bool enable16BitColor = false;
        [ObservableProperty]
        private bool windowsOS = false;

        /* Knossos */
        [ObservableProperty]
        private string basePath = string.Empty;
        [ObservableProperty]
        private bool enableLogFile = true;
        [ObservableProperty]
        private int logLevel = 1;
        [ObservableProperty]
        private bool fs2RootPack = false;
        [ObservableProperty]
        private string numberOfMods = string.Empty;
        [ObservableProperty]
        private string numberOfBuilds = string.Empty;
        [ObservableProperty]
        private string detectedOS = string.Empty;
        [ObservableProperty]
        private string cpuArch = string.Empty;
        [ObservableProperty]
        private bool isAVX = false;
        [ObservableProperty]
        private bool isAVX2 = false;

        /*VIDEO*/
        [ObservableProperty]
        private int bitsSelectedIndex = 0;
        [ObservableProperty]
        private int resolutionSelectedIndex = 0;
        [ObservableProperty]
        private int textureSelectedIndex = 0;
        private ObservableCollection<ComboBoxItem> ResolutionItems { get; set; } = new ObservableCollection<ComboBoxItem>();
        [ObservableProperty]
        private bool enableShadows = true;
        [ObservableProperty]
        private int shadowQualitySelectedIndex = 3;
        [ObservableProperty]
        private bool enableAA = true;
        [ObservableProperty]
        private int aaSelectedIndex = 4;
        [ObservableProperty]
        private bool enableSoftParticles = true;
        [ObservableProperty]
        private bool runInWindow = false;
        [ObservableProperty]
        private bool borderlessWindow = false;
        [ObservableProperty]
        private bool noVsync = false;
        [ObservableProperty]
        private bool noProstProcess = false;
        [ObservableProperty]
        private bool noFpsCapping = false;
        [ObservableProperty]
        private bool showFps = false;

        /*AUDIO*/
        [ObservableProperty]
        private int playbackSelectedIndex = 0;
        private ObservableCollection<ComboBoxItem> PlaybackItems { get; set; } = new ObservableCollection<ComboBoxItem>();
        [ObservableProperty]
        private int captureSelectedIndex = 0;
        private ObservableCollection<ComboBoxItem> CaptureItems { get; set; } = new ObservableCollection<ComboBoxItem>();
        [ObservableProperty]
        private int sampleRateSelectedIndex = 0;
        [ObservableProperty]
        private bool enableEFX = false;
        [ObservableProperty]
        private bool disableAudio = false;
        [ObservableProperty]
        private bool disableMusic = false;
        [ObservableProperty]
        private bool enableTTS = true;
        [ObservableProperty]
        private int voiceSelectedIndex = 0;
        private ObservableCollection<ComboBoxItem> VoiceItems { get; set; } = new ObservableCollection<ComboBoxItem>();
        [ObservableProperty]
        private bool ttsTechroom = true;
        [ObservableProperty]
        private bool ttsBriefings = true;
        [ObservableProperty]
        private bool ttsIngame = true;
        [ObservableProperty]
        private bool ttsMulti = true;
        [ObservableProperty]
        private int ttsVolume = 100;

        /*JOYSTICK*/
        private ObservableCollection<ComboBoxItem> Joystick1Items { get; set; } = new ObservableCollection<ComboBoxItem>();
        private ObservableCollection<ComboBoxItem> Joystick2Items { get; set; } = new ObservableCollection<ComboBoxItem>();
        private ObservableCollection<ComboBoxItem> Joystick3Items { get; set; } = new ObservableCollection<ComboBoxItem>();
        private ObservableCollection<ComboBoxItem> Joystick4Items { get; set; } = new ObservableCollection<ComboBoxItem>();

        private int joy1SelectedIndex = 0;
        private int Joy1SelectedIndex
        {
            get => joy1SelectedIndex;
            set {
                if (value != -1)
                {
                    this.SetProperty(ref joy1SelectedIndex, value);
                    if (value == 0 || value != 0 && value == Joy2SelectedIndex) Joy2SelectedIndex = 0;
                    if (value != 0 && value == Joy3SelectedIndex) Joy3SelectedIndex = 0;
                    if (value != 0 && value == Joy4SelectedIndex) Joy4SelectedIndex = 0;
                }
            }
        }
        private int joy2SelectedIndex = 0;
        private int Joy2SelectedIndex
        {
            get => joy2SelectedIndex;
            set
            {
                if (value != -1)
                {
                    this.SetProperty(ref joy2SelectedIndex, value);
                    if (value != 0 && value == Joy1SelectedIndex) Joy1SelectedIndex = 0;
                    if (value == 0 || value != 0 && value == Joy3SelectedIndex) Joy3SelectedIndex = 0;
                    if (value != 0 && value == Joy4SelectedIndex) Joy4SelectedIndex = 0;
                }
            }
        }
        private int joy3SelectedIndex = 0;
        private int Joy3SelectedIndex
        {
            get => joy3SelectedIndex;
            set
            {
                if (value != -1)
                {
                    this.SetProperty(ref joy3SelectedIndex, value);
                    if (value != 0 && value == Joy2SelectedIndex) Joy2SelectedIndex = 0;
                    if (value != 0 && value == Joy1SelectedIndex) Joy1SelectedIndex = 0;
                    if (value == 0 || value != 0 && value == Joy4SelectedIndex) Joy4SelectedIndex = 0;
                }
            }
        }
        private int joy4SelectedIndex = 0;
        private int Joy4SelectedIndex
        {
            get => joy4SelectedIndex;
            set
            {
                if (value != -1)
                {
                    this.SetProperty(ref joy4SelectedIndex, value);
                    if (value != 0 && value == Joy2SelectedIndex) Joy2SelectedIndex = 0;
                    if (value != 0 && value == Joy3SelectedIndex) Joy3SelectedIndex = 0;
                    if (value != 0 && value == Joy1SelectedIndex) Joy1SelectedIndex = 0;
                }
            }
        }

        /*MOD / FS2 */
        [ObservableProperty]
        private FsoBuildPickerViewModel? fsoPicker;
        [ObservableProperty]
        private string globalCmd = string.Empty;
        [ObservableProperty]
        private int fs2LangSelectedIndex = 0;
        [ObservableProperty]
        private uint multiPort = 7808;

        public GlobalSettingsViewModel()
        {
        }

        public void LoadData()
        {
            var flagData = GetFlagData();
            /* Knossos Settings */
            if (Knossos.globalSettings.basePath != null)
            {
                BasePath = Knossos.globalSettings.basePath;
            }
            EnableLogFile = Knossos.globalSettings.enableLogFile;
            LogLevel= Knossos.globalSettings.logLevel;
            Fs2RootPack = Knossos.retailFs2RootFound;
            NumberOfMods = Knossos.GetInstalledModList(null).Count.ToString();
            NumberOfBuilds = Knossos.GetInstalledBuildsList(null).Count.ToString();
            if(SysInfo.IsWindows)
            {
                DetectedOS = "Windows";
                WindowsOS = true;
            }
            else
            {
                if(SysInfo.IsLinux)
                {
                    DetectedOS = "Linux";
                }
                else
                {
                    if(SysInfo.IsMacOS)
                    {
                        DetectedOS = "OSX";
                    }
                }
            }

            CpuArch = SysInfo.CpuArch;
            IsAVX = SysInfo.CpuAVX;
            IsAVX2 = SysInfo.CpuAVX2;

            /* VIDEO SETTINGS */
            //RESOLUTION
            ResolutionItems.Clear();
            if (flagData != null && flagData.displays != null)
            {
                foreach(var display in flagData.displays)
                {
                    var item = new ComboBoxItem();
                    item.Content = display.name;
                    item.IsEnabled = false;
                    item.Tag = 0;
                    ResolutionItems.Add(item);
                    if(display.modes != null)
                    {
                        foreach(var mode in display.modes)
                        {
                            var itemMode = new ComboBoxItem();
                            itemMode.Content = mode.x+"x"+mode.y;
                            itemMode.Tag = display.index;
                            ResolutionItems.Add(itemMode);
                            if(mode.bits == 16)
                            {
                                Enable16BitColor = true;
                            }
                        }
                    }

                    if(Knossos.globalSettings.displayResolution == null)
                    {
                        Knossos.globalSettings.displayResolution = display.width + "x" + display.height;
                    }
                }
            }
            var resoItem = ResolutionItems.FirstOrDefault(i => i.Content?.ToString() == Knossos.globalSettings.displayResolution);
            if (resoItem != null)
            {
                var index = ResolutionItems.IndexOf(resoItem);
                if(index != -1)
                {
                    ResolutionSelectedIndex = index;
                }
            }
            else
            {
                ResolutionSelectedIndex = 0;
            }
            //COLOR DEPTH
            if(Knossos.globalSettings.displayColorDepth == 32)
            {
                BitsSelectedIndex = 0;
            }
            else
            {
                BitsSelectedIndex = 1;
            }
            //Texture Filter
            TextureSelectedIndex = Knossos.globalSettings.textureFilter;
            //Shadows
            EnableShadows = Knossos.globalSettings.enableShadows;
            ShadowQualitySelectedIndex = Knossos.globalSettings.shadowQuality;
            //AA
            EnableAA = Knossos.globalSettings.enableAA;
            AaSelectedIndex = Knossos.globalSettings.aaPreset;
            //SoftParticles
            EnableSoftParticles = Knossos.globalSettings.enableSoftParticles;
            //Window
            RunInWindow = Knossos.globalSettings.runInWindow;
            //Borderless Window
            BorderlessWindow = Knossos.globalSettings.borderlessWindow;
            //VSYNC
            NoVsync = Knossos.globalSettings.noVsync;
            //No Post Process
            NoProstProcess = Knossos.globalSettings.noProstProcess;
            //No FPS Cap
            NoFpsCapping = Knossos.globalSettings.noFpsCapping;
            //FPS
            ShowFps = Knossos.globalSettings.showFps;

            /* AUDIO SETTINGS */
            //Playback Devices
            PlaybackItems.Clear();
            if (flagData != null && flagData.openal != null && flagData.openal.playback_devices != null)
            {
                foreach (var playback in flagData.openal.playback_devices)
                {
                    var item = new ComboBoxItem();
                    item.Content = playback;
                    item.Tag = playback;
                    PlaybackItems.Add(item);

                    if (Knossos.globalSettings.playbackDevice == null)
                    {
                        Knossos.globalSettings.playbackDevice = flagData.openal.default_playback;
                    }
                }
            }
            var pbItem = PlaybackItems.FirstOrDefault(i => i.Tag?.ToString() == Knossos.globalSettings.playbackDevice);
            if (pbItem != null)
            {
                var index = PlaybackItems.IndexOf(pbItem);
                if (index != -1)
                {
                    PlaybackSelectedIndex = index;
                }
            }
            else
            {
                PlaybackSelectedIndex = 0;
            }
            //Capture Devices
            CaptureItems.Clear();
            if (flagData != null && flagData.openal != null && flagData.openal.capture_devices != null)
            {
                foreach (var capture in flagData.openal.capture_devices)
                {
                    var item = new ComboBoxItem();
                    item.Content = capture;
                    item.Tag = capture;
                    CaptureItems.Add(item);

                    if (Knossos.globalSettings.captureDevice == null)
                    {
                        Knossos.globalSettings.captureDevice = flagData.openal.default_capture;
                    }
                }
            }
            var ctItem = CaptureItems.FirstOrDefault(i => i.Tag?.ToString() == Knossos.globalSettings.captureDevice);
            if (ctItem != null)
            {
                var index = CaptureItems.IndexOf(ctItem);
                if (index != -1)
                {
                    CaptureSelectedIndex = index;
                }
            }
            else
            {
                CaptureSelectedIndex = 0;
            }
            //Sample Rate
            switch (Knossos.globalSettings.sampleRate)
            {
                case 44100: SampleRateSelectedIndex = 0; break;
                case 48000: SampleRateSelectedIndex = 1; break;
                case 96000: SampleRateSelectedIndex = 2; break;
                case 192000: SampleRateSelectedIndex = 3; break;
                default: SampleRateSelectedIndex = 0; break;
            }
            //Enable EFX
            EnableEFX = Knossos.globalSettings.enableEfx;
            //Disable Audio
            DisableAudio = Knossos.globalSettings.disableAudio;
            //Disable Music
            DisableAudio = Knossos.globalSettings.disableMusic;
            //TTS Settings
            EnableTTS = Knossos.globalSettings.enableTts;
            TtsBriefings = Knossos.globalSettings.ttsBriefings;
            TtsTechroom = Knossos.globalSettings.ttsTechroom;
            TtsIngame = Knossos.globalSettings.ttsIngame;
            TtsMulti = Knossos.globalSettings.ttsMulti;
            TtsVolume = Knossos.globalSettings.ttsVolume;
            VoiceItems.Clear();
            if (flagData != null && flagData.voices != null)
            {
                foreach (var voice in flagData.voices)
                {
                    var item = new ComboBoxItem();
                    item.Content = voice;
                    item.Tag = voice;
                    VoiceItems.Add(item);
                }
            }
            if (Knossos.globalSettings.ttsVoice == null)
            {
                Knossos.globalSettings.ttsVoice = 0;
            }
            else
            {
                if(Knossos.globalSettings.ttsVoice.Value + 1 <= VoiceItems.Count)
                {
                    VoiceSelectedIndex = Knossos.globalSettings.ttsVoice.Value;
                }
            }
            //Joysticks
            //The reason for this BS is that i cant re-use comboBox items in multiple controls
            Joystick1Items.Clear();
            Joystick2Items.Clear();
            Joystick3Items.Clear();
            Joystick4Items.Clear();
            Joy1SelectedIndex = 0;
            Joy2SelectedIndex = 0;
            Joy3SelectedIndex = 0;
            Joy4SelectedIndex = 0;
            var noJoyItem = new ComboBoxItem();
            noJoyItem.Content = "No Joystick";
            noJoyItem.Tag = null;
            Joystick1Items.Add(noJoyItem);
            var noJoy2Item = new ComboBoxItem();
            noJoy2Item.Content = "No Joystick";
            noJoy2Item.Tag = null;
            Joystick2Items.Add(noJoy2Item);
            var noJoy3Item = new ComboBoxItem();
            noJoy3Item.Content = "No Joystick";
            noJoy3Item.Tag = null;
            Joystick3Items.Add(noJoy3Item);
            var noJoy4Item = new ComboBoxItem();
            noJoy4Item.Content = "No Joystick";
            noJoy4Item.Tag = null;
            Joystick4Items.Add(noJoy4Item);

            if (flagData != null && flagData.joysticks != null)
            {
                foreach (var joy in flagData.joysticks)
                {
                    var item = new ComboBoxItem();
                    item.Content = joy.name + " - GUID: " + joy.guid + " - ID: " + joy.id;
                    item.Tag = joy;
                    var item2 = new ComboBoxItem();
                    item2.Content = joy.name + " - GUID: " + joy.guid + " - ID: " + joy.id; ;
                    item2.Tag = joy;
                    var item3 = new ComboBoxItem();
                    item3.Content = joy.name + " - GUID: " + joy.guid + " - ID: " + joy.id; ;
                    item3.Tag = joy;
                    var item4 = new ComboBoxItem();
                    item4.Content = joy.name + " - GUID: " + joy.guid + " - ID: " + joy.id; ;
                    item4.Tag = joy;
                    Joystick1Items.Add(item);
                    Joystick2Items.Add(item2);
                    Joystick3Items.Add(item3);
                    Joystick4Items.Add(item4);
                }

                // i hate this
                if(Knossos.globalSettings.joystick1 != null)
                {
                    bool found = false;
                    foreach(var item in Joystick1Items)
                    {
                        if(item.Tag != null)
                        {
                            var joystick = (Joystick)item.Tag;
                            if (joystick.guid == Knossos.globalSettings.joystick1.guid)
                            {
                                var index = Joystick1Items.IndexOf(item);
                                if (index != -1)
                                {
                                    Joy1SelectedIndex = index;
                                    found = true;
                                }
                            }
                        }
                    }

                    if(!found)
                    {
                        var missingItem = new ComboBoxItem();
                        missingItem.Content = "(Missing) " + Knossos.globalSettings.joystick1.name + " GUID: " + Knossos.globalSettings.joystick1.guid + " ID: " + Knossos.globalSettings.joystick1.id;
                        missingItem.Tag = Knossos.globalSettings.joystick1;
                        Joystick1Items.Add(missingItem);
                        var index = Joystick1Items.IndexOf(missingItem);
                        if (index != -1)
                        {
                            Joy1SelectedIndex = index;
                        }
                    }
                }
                if (Knossos.globalSettings.joystick2 != null)
                {
                    bool found = false;
                    foreach (var item in Joystick2Items)
                    {
                        if (item.Tag != null)
                        {
                            var joystick = (Joystick)item.Tag;
                            if (joystick.guid == Knossos.globalSettings.joystick2.guid)
                            {
                                var index = Joystick2Items.IndexOf(item);
                                if (index != -1)
                                {
                                    Joy2SelectedIndex = index;
                                    found = true;
                                }
                            }
                        }
                    }

                    if (!found)
                    {
                        var missingItem = new ComboBoxItem();
                        missingItem.Content = "(Missing) " + Knossos.globalSettings.joystick2.name + " GUID: " + Knossos.globalSettings.joystick2.guid + " ID: " + Knossos.globalSettings.joystick2.id;
                        missingItem.Tag = Knossos.globalSettings.joystick2;
                        Joystick2Items.Add(missingItem);
                        var index = Joystick2Items.IndexOf(missingItem);
                        if (index != -1)
                        {
                            Joy2SelectedIndex = index;
                        }
                    }
                }
                if (Knossos.globalSettings.joystick3 != null)
                {
                    bool found = false;
                    foreach (var item in Joystick3Items)
                    {
                        if (item.Tag != null)
                        {
                            var joystick = (Joystick)item.Tag;
                            if (joystick.guid == Knossos.globalSettings.joystick3.guid)
                            {
                                var index = Joystick3Items.IndexOf(item);
                                if (index != -1)
                                {
                                    Joy3SelectedIndex = index;
                                    found = true;
                                }
                            }
                        }
                    }

                    if (!found)
                    {
                        var missingItem = new ComboBoxItem();
                        missingItem.Content = "(Missing) " + Knossos.globalSettings.joystick3.name + " GUID: " + Knossos.globalSettings.joystick3.guid + " ID: " + Knossos.globalSettings.joystick3.id;
                        missingItem.Tag = Knossos.globalSettings.joystick3;
                        Joystick3Items.Add(missingItem);
                        var index = Joystick3Items.IndexOf(missingItem);
                        if (index != -1)
                        {
                            Joy3SelectedIndex = index;
                        }
                    }
                }
                if (Knossos.globalSettings.joystick4 != null)
                {
                    bool found = false;
                    foreach (var item in Joystick4Items)
                    {
                        if (item.Tag != null)
                        {
                            var joystick = (Joystick)item.Tag;
                            if (joystick.guid == Knossos.globalSettings.joystick4.guid)
                            {
                                var index = Joystick4Items.IndexOf(item);
                                if (index != -1)
                                {
                                    Joy4SelectedIndex = index;
                                    found = true;
                                }
                            }
                        }
                    }

                    if (!found)
                    {
                        var missingItem = new ComboBoxItem();
                        missingItem.Content = "(Missing) " + Knossos.globalSettings.joystick4.name + " GUID: " + Knossos.globalSettings.joystick4.guid + " ID: " + Knossos.globalSettings.joystick4.id;
                        missingItem.Tag = Knossos.globalSettings.joystick4;
                        Joystick4Items.Add(missingItem);
                        var index = Joystick4Items.IndexOf(missingItem);
                        if (index != -1)
                        {
                            Joy4SelectedIndex = index;
                        }
                    }
                }
            }

            /* MOD SETTINGS */
            //FSO VERSION
            if (Knossos.globalSettings.globalBuildId != null)
            {
                if (Knossos.globalSettings.globalBuildExec != null)
                {
                    FsoPicker = new FsoBuildPickerViewModel(new FsoBuild(Knossos.globalSettings.globalBuildExec));
                }
                else
                {
                    var matchingBuilds = Knossos.GetInstalledBuildsList(Knossos.globalSettings.globalBuildId);
                    if (matchingBuilds.Any())
                    {
                        var theBuild = matchingBuilds.First(build => build.version == Knossos.globalSettings.globalBuildVersion);
                        if (theBuild != null)
                        {
                            FsoPicker = new FsoBuildPickerViewModel(theBuild);
                        }
                        else
                        {
                            Log.Add(Log.LogSeverity.Warning, "GlobalSettingsViewModel.LoadData()", "Missing user-saved global build version: " + Knossos.globalSettings.globalBuildId + " and version: " + Knossos.globalSettings.globalBuildVersion);
                            FsoPicker = new FsoBuildPickerViewModel(null);
                        }
                    }
                    else
                    {
                        Log.Add(Log.LogSeverity.Warning, "GlobalSettingsViewModel.LoadData()", "Missing user-saved global build, requested build id: " + Knossos.globalSettings.globalBuildId);
                        FsoPicker = new FsoBuildPickerViewModel(null);
                    }
                }
            }
            else
            {
                FsoPicker = new FsoBuildPickerViewModel(null);
            }

            //GLOBAL CMD
            if(Knossos.globalSettings.globalCmdLine != null)
            {
                GlobalCmd = Knossos.globalSettings.globalCmdLine;
            }

            //FS2 Lang
            switch(Knossos.globalSettings.fs2Lang)
            {
                case "English": Fs2LangSelectedIndex = 0; break;
                case "German": Fs2LangSelectedIndex = 1; break;
                case "French": Fs2LangSelectedIndex = 2; break;
                case "Polish": Fs2LangSelectedIndex = 3; break;
                default: Fs2LangSelectedIndex = 0;  break;
            }

            //Multi Port
            MultiPort = Knossos.globalSettings.multiPort;
        }

        private FlagsJsonV1? GetFlagData()
        {
            FlagDataLoaded = false;
            if (Knossos.globalSettings.globalBuildId == null)
            { 
                var builds = Knossos.GetInstalledBuildsList();

                if (builds.Any())
                {
                    //First the stable ones
                    var stables = builds.Where(b => b.stability == FsoStability.Stable);
                    if (stables.Any())
                    {
                        foreach (var stable in stables)
                        {
                            var flags = stable.GetFlagsV1();
                            if (flags != null)
                            {
                                FlagDataLoaded = true;
                                SysInfo.SetFSODataFolderPath(flags.pref_path);
                                return flags;
                            }
                        }
                    }

                    //If we are still here try all others
                    var others = builds.Where(b => b.stability != FsoStability.Stable);
                    if (others.Any())
                    {
                        foreach (var other in others)
                        {
                            var flags = other.GetFlagsV1();
                            if (flags != null)
                            {
                                FlagDataLoaded = true;
                                SysInfo.SetFSODataFolderPath(flags.pref_path);
                                return flags;
                            }
                        }
                    }
                }
            }
            else
            {
                if (Knossos.globalSettings.globalBuildExec == null)
                {
                    var globalBuild = Knossos.GetInstalledBuildsList().FirstOrDefault(builds => builds.id == Knossos.globalSettings.globalBuildId && builds.version == Knossos.globalSettings.globalBuildVersion);
                    var flags = globalBuild?.GetFlagsV1();
                    if (flags != null)
                    {
                        FlagDataLoaded = true;
                        SysInfo.SetFSODataFolderPath(flags.pref_path);
                        return flags;
                    }
                }
                else
                {
                    var globalBuild = new FsoBuild(Knossos.globalSettings.globalBuildExec);
                    var flags = globalBuild?.GetFlagsV1();
                    if (flags != null)
                    {
                        FlagDataLoaded = true;
                        SysInfo.SetFSODataFolderPath(flags.pref_path);
                        return flags;
                    }
                }
            }

            Log.Add(Log.LogSeverity.Warning, "GlobalSettingsViewModel.GetFlagData()", "Unable to find a valid build to get flag data for global settings.");
            return null;
        }

        /* UI Buttons */
        private async void BrowseFolderCommand()
        {
            if (MainWindow.instance != null)
            {
                var dialog = new OpenFolderDialog();
                if(BasePath != string.Empty)
                {
                    dialog.Directory = BasePath;
                }

                var result = await dialog.ShowAsync(MainWindow.instance);

                if (result != null)
                {
                    Knossos.globalSettings.basePath = result;
                    Knossos.globalSettings.Save();
                    Knossos.LoadBasePath();
                    LoadData();
                }
            }
        }

        private void ResetCommand()
        {
            Knossos.globalSettings = new GlobalSettings();
            LoadData();
            SaveCommand();
        }

        private void SaveCommand()
        {
            /* Knossos Settings */
            if (BasePath != string.Empty)
            {
                Knossos.globalSettings.basePath = BasePath;
            }
            Knossos.globalSettings.enableLogFile = EnableLogFile;
            Knossos.globalSettings.logLevel = LogLevel;

            /* VIDEO */
            //Resolution
            if (ResolutionSelectedIndex + 1 <= ResolutionItems.Count)
            {
                Knossos.globalSettings.displayResolution = ResolutionItems[ResolutionSelectedIndex].Content?.ToString();
                var displayIndex = ResolutionItems[ResolutionSelectedIndex].Tag;
                if ( displayIndex != null)
                { 
                    Knossos.globalSettings.displayIndex = (int)displayIndex;
                }
            }
            //Color Depth
            if(BitsSelectedIndex == 0)
            {
                Knossos.globalSettings.displayColorDepth = 32;
            }
            else
            {
                Knossos.globalSettings.displayColorDepth = 16;
            }
            //Texture Filter
            Knossos.globalSettings.textureFilter = TextureSelectedIndex;
            //Shadows
            Knossos.globalSettings.enableShadows = EnableShadows;
            Knossos.globalSettings.shadowQuality = ShadowQualitySelectedIndex;
            //AA
            Knossos.globalSettings.enableAA = EnableAA;
            Knossos.globalSettings.aaPreset = AaSelectedIndex;
            //SoftParticles
            Knossos.globalSettings.enableSoftParticles = EnableSoftParticles;
            //Window
            Knossos.globalSettings.runInWindow = RunInWindow;
            //Borderless Window
            Knossos.globalSettings.borderlessWindow = BorderlessWindow;
            //VSYNC
            Knossos.globalSettings.noVsync = NoVsync;
            //No Post Process
            Knossos.globalSettings.noProstProcess = NoProstProcess;
            //No FPS Cap
            Knossos.globalSettings.noFpsCapping = NoFpsCapping;
            //FPS
            Knossos.globalSettings.showFps = ShowFps;

            /* AUDIO SETTINGS */
            //Playback
            if (PlaybackSelectedIndex + 1 <= PlaybackItems.Count)
            {
                Knossos.globalSettings.playbackDevice = PlaybackItems[PlaybackSelectedIndex].Tag?.ToString();
            }
            //Capture
            if (CaptureSelectedIndex + 1 <= CaptureItems.Count)
            {
                Knossos.globalSettings.captureDevice = CaptureItems[CaptureSelectedIndex].Tag?.ToString();
            }
            //Sample Rate
            switch (SampleRateSelectedIndex)
            {
                case 0: Knossos.globalSettings.sampleRate = 44100; break;
                case 1: Knossos.globalSettings.sampleRate = 48000; break;
                case 2: Knossos.globalSettings.sampleRate = 96000; break;
                case 3: Knossos.globalSettings.sampleRate = 192000; break;
            }
            //Enable EFX
            Knossos.globalSettings.enableEfx = EnableEFX;
            //Disable Audio
            Knossos.globalSettings.disableAudio = DisableAudio;
            //Disable Music
            Knossos.globalSettings.disableMusic = DisableAudio;
            //TTS Settings
            Knossos.globalSettings.enableTts = EnableTTS;
            Knossos.globalSettings.ttsBriefings = TtsBriefings;
            Knossos.globalSettings.ttsTechroom = TtsTechroom;
            Knossos.globalSettings.ttsIngame = TtsIngame;
            Knossos.globalSettings.ttsMulti = TtsMulti;
            Knossos.globalSettings.ttsVoice = VoiceSelectedIndex;
            Knossos.globalSettings.ttsVolume = TtsVolume;

            /* JOYSTICKS */
            if (Joy1SelectedIndex + 1 <= Joystick1Items.Count)
            {
                Knossos.globalSettings.joystick1 = (Joystick?)Joystick1Items[Joy1SelectedIndex].Tag;
            }
            if (Joy2SelectedIndex + 1 <= Joystick2Items.Count)
            {
                Knossos.globalSettings.joystick2 = (Joystick?)Joystick2Items[Joy2SelectedIndex].Tag;
            }
            if (Joy3SelectedIndex + 1 <= Joystick3Items.Count)
            {
                Knossos.globalSettings.joystick3 = (Joystick?)Joystick3Items[Joy3SelectedIndex].Tag;
            }
            if (Joy4SelectedIndex + 1 <= Joystick4Items.Count)
            {
                Knossos.globalSettings.joystick4 = (Joystick?)Joystick4Items[Joy4SelectedIndex].Tag;
            }

            /* MOD SETTINGS */
            //GLOBAL BUILD
            var globalBuild = FsoPicker?.GetSelectedFsoBuild();
            if (globalBuild != null)
            {
                Knossos.globalSettings.globalBuildId = globalBuild.id;
                Knossos.globalSettings.globalBuildVersion = globalBuild.version;
                Knossos.globalSettings.globalBuildExec = globalBuild.directExec;
            }
            else
            {
                Knossos.globalSettings.globalBuildId = null;
                Knossos.globalSettings.globalBuildVersion = null;
                Knossos.globalSettings.globalBuildExec = null;
            }
            //GLOBAL CMD
            if (GlobalCmd.Trim().Length > 0)
            {
                Knossos.globalSettings.globalCmdLine = GlobalCmd;
            }
            else
            {
                Knossos.globalSettings.globalCmdLine = null;
            }

            //FS2 Lang
            switch (Fs2LangSelectedIndex)
            {
                case 0: Knossos.globalSettings.fs2Lang = "English"; break;
                case 1: Knossos.globalSettings.fs2Lang = "German"; break;
                case 2: Knossos.globalSettings.fs2Lang = "French"; break;
                case 3: Knossos.globalSettings.fs2Lang = "Polish"; break;
            }

            //Multi port
            Knossos.globalSettings.multiPort = MultiPort;

            Knossos.globalSettings.Save();
        }

        private void TestVoiceCommand()
        {

        }

        private void GlobalCmdHelp()
        {
            Knossos.OpenBrowserURL("https://wiki.hard-light.net/index.php/Command-Line_Reference");
        }
        private void ReloadFlagData()
        {
            LoadData();
        }

        private async void OpenGetVoices()
        {
            if (MainWindow.instance != null)
            {
                var dialog = new AddSapiVoicesView();
                dialog.DataContext = new AddSapiVoicesViewModel();

                await dialog.ShowDialog<AddSapiVoicesView?>(MainWindow.instance);
            }
        }
    }
}
