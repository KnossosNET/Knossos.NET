
using System.Text.Json;
using System;
using System.Text.Json.Serialization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Knossos.NET.Classes;
using IniParser;
using IniParser.Model;
using HarfBuzzSharp;
using Avalonia.Threading;
using Knossos.NET.Views;
using Knossos.NET.ViewModels;
using System.Threading.Tasks;

namespace Knossos.NET.Models
{
    /// <summary>
    /// Disabled = No option to compress is ever show
    /// Manual = User can select to compress mods manually during install and in mod settings
    /// Always = Always compress all mods during install, no matter what.
    /// ModSupport = Compress only if the mod depends on a FSO verson that is higher or equal than the minimal required (23.2.0)
    /// </summary>
    public enum CompressionSettings
    {
        Disabled,
        Manual,
        Always,
        ModSupport
    }

    /// <summary>
    /// Stores and load the global Knossos.NET configuration
    /// </summary>
    public class GlobalSettings
    {
        struct Resolution
        {
            public uint width { get; set; }
            public uint height { get; set; }
        }

        /* Knossos Settings */
        [JsonPropertyName("base_path")]
        public string? basePath { get; set; } = null;
        [JsonPropertyName("enable_log")]
        public bool enableLogFile { get; set; } = true;
        [JsonPropertyName("log_level")]
        public int logLevel { get; set; } = 1;
        [JsonPropertyName("global_cmdline")]
        public string? globalCmdLine { get; set; } = null;
        [JsonPropertyName("force_sse2")]
        public bool forceSSE2 { get; set; } = false;
        [JsonPropertyName("max_concurrent_subtasks")]
        public int maxConcurrentSubtasks { get; set; } = 3;
        [JsonPropertyName("max_download_speed")]
        public long maxDownloadSpeed { get; set; } = 0;
        [JsonPropertyName("mirror_blacklist")]
        public string[]? mirrorBlacklist { get; set; } = null;
        [JsonPropertyName("mod_compression")]
        public CompressionSettings modCompression { get; set; } = CompressionSettings.Manual;
        [JsonPropertyName("compression_max_parallelism")]
        public int compressionMaxParallelism { get; set; } = 4;
        [JsonPropertyName("auto_update")]
        public bool autoUpdate { get; set; } = false;
        [JsonPropertyName("check_updates")]
        public bool checkUpdate { get; set; } = true;

        /* FSO Settings that use the fs2_open.ini are json ignored */

        /* Video Settings */
        [JsonIgnore]
        public string? displayResolution { get; set; } = null;
        [JsonIgnore]
        public int displayIndex { get; set; } = 0;
        [JsonIgnore]
        public int displayColorDepth { get; set; } = 32;
        [JsonIgnore]
        public int textureFilter { get; set; } = 1;
        [JsonIgnore]
        public int shadowQuality { get; set; } = 0;
        [JsonIgnore]
        public int aaPreset { get; set; } = 4;
        [JsonIgnore]
        public int msaaPreset { get; set; } = 0;
        [JsonIgnore]
        public bool enableSoftParticles { get; set; } = true;
        [JsonIgnore]
        public bool enableDeferredLighting { get; set; } = true;
        [JsonIgnore]
        public int windowMode { get; set; } = 2;
        [JsonIgnore]
        public bool vsync { get; set; } = true;
        [JsonIgnore]
        public bool postProcess { get; set; } = true;
        [JsonPropertyName("no_fps_capping")]
        public bool noFpsCapping { get; set; } = false;
        [JsonPropertyName("show_fps")]
        public bool showFps { get; set; } = false;

        /* AUDIO SETTINGS */
        [JsonIgnore]
        public string? playbackDevice { get; set; } = null;
        [JsonIgnore]
        public string? captureDevice { get; set; } = null;
        [JsonPropertyName("disable_audio")]
        public bool disableAudio { get; set; } = false;
        [JsonPropertyName("disable_music")]
        public bool disableMusic { get; set; } = false;
        [JsonIgnore]
        public int sampleRate { get; set; } = 44100;
        [JsonIgnore]
        public bool enableEfx { get; set; } = false;
        [JsonPropertyName("enable_tts")]
        public bool enableTts { get; set; } = true;
        [JsonIgnore]
        public int? ttsVoice { get; set; } = null;
        public string? ttsVoiceName { get; set; } = null;
        [JsonIgnore]
        public bool ttsTechroom { get; set; } = true;
        [JsonIgnore]
        public bool ttsBriefings { get; set; } = true;
        [JsonIgnore]
        public bool ttsIngame { get; set; } = true;
        [JsonIgnore]
        public bool ttsMulti { get; set; } = true;
        [JsonPropertyName("tts_description")]
        public bool ttsDescription { get; set; } = true;
        [JsonIgnore]
        public int ttsVolume { get; set; } = 100;

        /* INPUT */
        [JsonIgnore]
        public Joystick? joystick1 { get; set; } = null;
        [JsonIgnore]
        public Joystick? joystick2 { get; set; } = null;
        [JsonIgnore]
        public Joystick? joystick3 { get; set; } = null;
        [JsonIgnore]
        public Joystick? joystick4 { get; set; } = null;
        [JsonIgnore]
        public uint joystickDeadZone { get; set; } = 10;
        [JsonIgnore]
        public uint mouseSensitivity { get; set; } = 5;
        [JsonIgnore]
        public uint joystickSensitivity { get; set; } = 9;

        /* MISC */
        [JsonIgnore]
        public string fs2Lang { get; set; } = "English";
        [JsonIgnore]
        public uint multiPort { get; set; } = 7808;
        [JsonIgnore]
        public string pxoLogin { get; set; } = "";
        [JsonIgnore]
        public string pxoPassword { get; set; } = "";

        [JsonPropertyName("hide_build_rc")]
        public bool hideBuildRC { get; set; } = true;
        [JsonPropertyName("hide_build_custom")]
        public bool hideBuildCustom { get; set; } = false;
        [JsonPropertyName("hide_build_nightly")]
        public bool hideBuildNightly { get; set; } = true;

        [JsonIgnore]
        private FileSystemWatcher? iniWatcher = null;

        /// <summary>
        /// When the User is on the settings tab we must watch the fs2_open.ini for external changes
        /// This is the initial call that must be called once, then we start or stop raising of events
        /// </summary>
        private void StartWatchingDirectory()
        {
            iniWatcher = new FileSystemWatcher(KnUtils.GetFSODataFolderPath());
            iniWatcher.NotifyFilter = NotifyFilters.LastWrite;
            iniWatcher.Changed += OnIniChanged;
            iniWatcher.Filter = "fs2_open.ini";
        }

        /// <summary>
        /// If the fs2_open.ini is changed externally, reload the data
        /// </summary>
        private void OnIniChanged(object sender, FileSystemEventArgs e)
        {
            iniWatcher!.EnableRaisingEvents = false;
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Log.Add(Log.LogSeverity.Information, "GlobalSettings.OnIniChanged()", "fs2_open.ini was changed externally, loading data.");
                Load();
                MainWindowViewModel.Instance?.GlobalSettingsLoadData();
            });
            Task.Delay(1000);
            iniWatcher!.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Start watching for changes on the ini file
        /// </summary>
        public void EnableIniWatch()
        {
            if(iniWatcher != null) 
                iniWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Stop watching for changes on the ini file
        /// </summary>
        public void DisableIniWatch()
        {
            if(iniWatcher != null)
                iniWatcher.EnableRaisingEvents = false;
        }

        /// <summary>
        /// Load setting data that saves on the fs2_open.ini
        /// On the ini we save all data that is used by both FSO and KNET
        /// </summary>
        private void ReadFS2IniValues()
        {
            try
            {
                if (!File.Exists(KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "fs2_open.ini"))
                {
                    return;
                }
                var parser = new FileIniDataParser();
                var data = parser.ReadFile(KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "fs2_open.ini");
                data.Configuration.AssigmentSpacer = string.Empty;

                if(iniWatcher == null)
                {
                    StartWatchingDirectory();
                }


                //LEGACY ENTRIES, mostly read only by fso
                /* Default Section */
                var resoItem = data["Default"]["VideocardFs2open"];
                if (!string.IsNullOrEmpty(resoItem))
                {
                    displayResolution = resoItem.Split('(', ')')[1];
                    if (resoItem.Contains("16 bit"))
                    {
                        displayColorDepth = 16;
                    }
                }

                if(!string.IsNullOrEmpty(data["Default"]["TextureFilter"]))
                {
                    textureFilter = int.Parse(data["Default"]["TextureFilter"]);
                }

                //Joysticks!
                if (!string.IsNullOrEmpty(data["Default"]["CurrentJoystickGUID"]) || !string.IsNullOrEmpty(data["Default"]["Joy0GUID"]))
                {
                    joystick1 = new Joystick();
                    joystick1.name = string.Empty;
                    if (!string.IsNullOrEmpty(data["Default"]["Joy0GUID"]) && !string.IsNullOrEmpty(data["Default"]["Joy0ID"]))
                    {
                        joystick1.guid = data["Default"]["Joy0GUID"];
                        joystick1.id = int.Parse(data["Default"]["Joy0ID"]);
                    }
                    else if (!string.IsNullOrEmpty(data["Default"]["CurrentJoystick"]) && !string.IsNullOrEmpty(data["Default"]["CurrentJoystickGUID"]))
                    {
                        joystick1.guid = data["Default"]["CurrentJoystickGUID"];
                        joystick1.id = int.Parse(data["Default"]["CurrentJoystick"]);
                    }
                }
                if (!string.IsNullOrEmpty(data["Default"]["Joy1GUID"]) && !string.IsNullOrEmpty(data["Default"]["Joy1ID"]))
                {
                    joystick2 = new Joystick();
                    joystick2.name = string.Empty;
                    joystick2.guid = data["Default"]["Joy1GUID"];
                    joystick2.id = int.Parse(data["Default"]["Joy1ID"]);
                }
                if (!string.IsNullOrEmpty(data["Default"]["Joy2GUID"]) && !string.IsNullOrEmpty(data["Default"]["Joy2ID"]))
                {
                    joystick3 = new Joystick();
                    joystick3.name = string.Empty;
                    joystick3.guid = data["Default"]["Joy2GUID"];
                    joystick3.id = int.Parse(data["Default"]["Joy2ID"]);
                }
                if (!string.IsNullOrEmpty(data["Default"]["Joy3GUID"]) && !string.IsNullOrEmpty(data["Default"]["Joy3ID"]))
                {
                    joystick4 = new Joystick();
                    joystick4.name = string.Empty;
                    joystick4.guid = data["Default"]["Joy3GUID"];
                    joystick4.id = int.Parse(data["Default"]["Joy3ID"]);
                }

                if (!string.IsNullOrEmpty(data["Default"]["SpeechVolume"]))
                {
                    ttsVolume = int.Parse(data["Default"]["SpeechVolume"]);
                }

                if (!string.IsNullOrEmpty(data["Default"]["SpeechVoice"]))
                {
                    ttsVoice = int.Parse(data["Default"]["SpeechVoice"]);
                }

                if (!string.IsNullOrEmpty(data["Default"]["SpeechTechroom"]))
                {
                    if (int.Parse(data["Default"]["SpeechTechroom"]) == 1)
                    {
                        ttsTechroom = true;
                    }
                    else
                    {
                        ttsTechroom = false;
                    }
                }

                if (!string.IsNullOrEmpty(data["Default"]["SpeechBriefings"]))
                {
                    if (int.Parse(data["Default"]["SpeechBriefings"]) == 1)
                    {
                        ttsBriefings = true;
                    }
                    else
                    {
                        ttsBriefings = false;
                    }
                }

                if (!string.IsNullOrEmpty(data["Default"]["SpeechIngame"]))
                {
                    if (int.Parse(data["Default"]["SpeechIngame"]) == 1)
                    {
                        ttsIngame = true;
                    }
                    else
                    {
                        ttsIngame = false;
                    }
                }

                if (!string.IsNullOrEmpty(data["Default"]["SpeechMulti"]))
                {
                    if (int.Parse(data["Default"]["SpeechMulti"]) == 1)
                    {
                        ttsMulti = true;
                    }
                    else
                    {
                        ttsMulti = false;
                    }
                }

                if (!string.IsNullOrEmpty(data["Default"]["Language"]))
                {
                    fs2Lang = data["Default"]["Language"];
                }

                if (!string.IsNullOrEmpty(data["Default"]["ForcePort"]))
                {
                    multiPort = uint.Parse(data["Default"]["ForcePort"]);
                }

                /* Video Section */
                if (!string.IsNullOrEmpty(data["Video"]["Display"]))
                {
                    displayIndex = int.Parse(data["Video"]["Display"]);
                }
                else
                {
                    displayIndex = 0;
                }

                /* Sound Section */
                captureDevice = data["Sound"]["CaptureDevice"];
                playbackDevice = data["Sound"]["PlaybackDevice"];
                if(!string.IsNullOrEmpty(data["Sound"]["SampleRate"]))
                {
                    sampleRate = int.Parse(data["Sound"]["SampleRate"]);
                }
                if (!string.IsNullOrEmpty(data["Sound"]["EnableEFX"]))
                {
                    if(int.Parse(data["Sound"]["EnableEFX"])==1)
                    {
                        enableEfx = true;
                    }
                    else
                    {
                        enableEfx = false;
                    }
                }

                //SCPUI ENTRIES
                /* Graphics Section */
                var scpUiReso = data["Graphics"]["Resolution"];
                if (!string.IsNullOrEmpty(scpUiReso))
                {
                    var rj=JsonSerializer.Deserialize<Resolution>(scpUiReso)!;
                    displayResolution = rj.width + "x" + rj.height;
                }
                var winMode = data["Graphics"]["WindowMode"];
                if(!string.IsNullOrEmpty(winMode))
                {   
                    windowMode = int.Parse(winMode);
                }
                if (data["Graphics"]["Display"] != null)
                {
                    displayIndex = int.Parse(data["Graphics"]["Display"]);
                }
                else
                {
                    displayIndex = 0;
                }

                if (!string.IsNullOrEmpty(data["Graphics"]["TextureFilter"]))
                {
                    textureFilter = int.Parse(data["Graphics"]["TextureFilter"]);
                }

                if(!string.IsNullOrEmpty(data["Graphics"]["Shadows"]))
                {
                    shadowQuality = int.Parse(data["Graphics"]["Shadows"]);
                }

                if (!string.IsNullOrEmpty(data["Graphics"]["AAMode"]))
                {
                    aaPreset = int.Parse(data["Graphics"]["AAMode"]);
                }

                if (!string.IsNullOrEmpty(data["Graphics"]["MSAASamples"]))
                {
                    // recall MSAASamples is in intervals of 4 (0, 4, 8) so convert to preset level
                    msaaPreset = int.Parse(data["Graphics"]["MSAASamples"]) / 4;
                }

                if (!string.IsNullOrEmpty(data["Graphics"]["SoftParticles"]))
                {
                    enableSoftParticles = bool.Parse(data["Graphics"]["SoftParticles"]);
                }

                if (!string.IsNullOrEmpty(data["Graphics"]["DeferredLighting"]))
                {
                    enableDeferredLighting = bool.Parse(data["Graphics"]["DeferredLighting"]);
                }

                if (!string.IsNullOrEmpty(data["Graphics"]["VSync"]))
                {
                    vsync = bool.Parse(data["Graphics"]["VSync"]);
                }

                if (!string.IsNullOrEmpty(data["Graphics"]["PostProcessing"]))
                {
                    postProcess = bool.Parse(data["Graphics"]["PostProcessing"]);
                }

                /* Input Section */
                if (!string.IsNullOrEmpty(data["Input"]["Joystick"]))
                {
                    joystick1 = JsonSerializer.Deserialize<Joystick>(data["Input"]["Joystick"])!;
                }
                if (!string.IsNullOrEmpty(data["Input"]["Joystick1"]))
                {
                    joystick2 = JsonSerializer.Deserialize<Joystick>(data["Input"]["Joystick1"])!;
                }
                if (!string.IsNullOrEmpty(data["Input"]["Joystick2"]))
                {
                    joystick3 = JsonSerializer.Deserialize<Joystick>(data["Input"]["Joystick2"])!;
                }
                if (!string.IsNullOrEmpty(data["Input"]["Joystick3"]))
                {
                    joystick4 = JsonSerializer.Deserialize<Joystick>(data["Input"]["Joystick3"])!;
                }
                if (!string.IsNullOrEmpty(data["Input"]["JoystickDeadZone"]))
                {
                    joystickDeadZone = uint.Parse(data["Input"]["JoystickDeadZone"]);
                }

                if (!string.IsNullOrEmpty(data["Input"]["JoystickSensitivity"]))
                {
                    joystickSensitivity = uint.Parse(data["Input"]["JoystickSensitivity"]);
                }

                if (!string.IsNullOrEmpty(data["Input"]["MouseSensitivity"]))
                {
                    mouseSensitivity = uint.Parse(data["Input"]["MouseSensitivity"]);
                }

                //PXO
                if (!string.IsNullOrEmpty(data["PXO"]["Login"]))
                {
                    pxoLogin = data["PXO"]["Login"];
                }
                if (!string.IsNullOrEmpty(data["PXO"]["Password"]))
                {
                    pxoPassword = data["PXO"]["Password"];
                }
            }
            catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Information, "GlobalSettings.ReadFS2IniValues()", ex);
            }
        }

        /// <summary>
        /// Load all Knet setting data, both from settings.json and from the fs2_open.ini
        /// Data that is used only by KNET is saved on settings.json
        /// Data that is used by FSO anf KNET is saved on the fs2_open.ini
        /// Any new variable that is added to the json must be added here or it would not be loaded
        /// </summary>
        public void Load()
        {
            try
            {
                if (File.Exists(KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "settings.json"))
                {
                    using FileStream jsonFile = File.OpenRead(KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "settings.json");
                    var tempSettings = JsonSerializer.Deserialize<GlobalSettings>(jsonFile)!;
                    jsonFile.Close();
                    if (tempSettings != null)
                    {
                        basePath = tempSettings.basePath;
                        enableLogFile = tempSettings.enableLogFile;
                        logLevel = tempSettings.logLevel;
                        globalCmdLine = tempSettings.globalCmdLine;
                        ttsDescription = tempSettings.ttsDescription;
                        enableTts = tempSettings.enableTts;
                        noFpsCapping = tempSettings.noFpsCapping;
                        showFps = tempSettings.showFps;
                        disableAudio= tempSettings.disableAudio;
                        disableMusic= tempSettings.disableMusic;
                        hideBuildCustom = tempSettings.hideBuildCustom;
                        hideBuildNightly = tempSettings.hideBuildNightly;
                        hideBuildRC = tempSettings.hideBuildRC;
                        forceSSE2 = tempSettings.forceSSE2;
                        maxConcurrentSubtasks = tempSettings.maxConcurrentSubtasks;
                        maxDownloadSpeed = tempSettings.maxDownloadSpeed;
                        mirrorBlacklist = tempSettings.mirrorBlacklist;
                        modCompression = tempSettings.modCompression;
                        compressionMaxParallelism = tempSettings.compressionMaxParallelism;
                        autoUpdate = tempSettings.autoUpdate;
                        checkUpdate = tempSettings.checkUpdate;
                        ttsVoiceName = tempSettings.ttsVoiceName;

                        ReadFS2IniValues();
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

        /// <summary>
        /// Save setting data to the fs2_open.ini
        /// Stops the ini-watcher if it was enabled and re-enables it to avoid triggering a read
        /// </summary>
        public void WriteFS2IniValues()
        {
            try
            {
                var parser = new FileIniDataParser();
                if(!File.Exists(KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "fs2_open.ini"))
                {
                    Directory.CreateDirectory(KnUtils.GetFSODataFolderPath());
                    File.Create(KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "fs2_open.ini").Close();
                }

                var data = parser.ReadFile(KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "fs2_open.ini");
                data.Configuration.AssigmentSpacer = string.Empty;

                /* Default Section */
                if(displayResolution != null)
                { 
                    data["Default"]["VideocardFs2open"]  = "OGL -(" + displayResolution + ")x" + displayColorDepth + " bit";
                }
                data["Default"]["TextureFilter"] = textureFilter.ToString();
                data["Default"]["Language"] = fs2Lang;
                data["Default"]["ForcePort"] = multiPort.ToString();
                data["Default"]["ConnectionSpeed"] = "Fast";
                data["Default"]["NetworkConnection"] = "LAN";
                if (enableTts)
                {
                    data["Default"]["SpeechTechroom"] = Convert.ToInt32(ttsTechroom).ToString();
                    data["Default"]["SpeechBriefings"] = Convert.ToInt32(ttsBriefings).ToString();
                    data["Default"]["SpeechIngame"] = Convert.ToInt32(ttsIngame).ToString();
                    data["Default"]["SpeechMulti"] = Convert.ToInt32(ttsMulti).ToString();
                }
                else
                {
                    data["Default"]["SpeechTechroom"] = "0";
                    data["Default"]["SpeechBriefings"] = "0";
                    data["Default"]["SpeechIngame"] = "0";
                    data["Default"]["SpeechMulti"] = "0";
                }

                data["Default"]["SpeechVoice"] = ttsVoice.ToString();
                data["Default"]["SpeechVolume"] = ttsVolume.ToString();

                if (joystick1 != null)
                {
                    data["Default"]["CurrentJoystickGUID"] = joystick1.guid;
                    data["Default"]["CurrentJoystick"] = joystick1.id.ToString();
                    data["Default"]["Joy0GUID"] = joystick1.guid;
                    data["Default"]["Joy0ID"] = joystick1.id.ToString();
                }
                else
                {
                    data["Default"].RemoveKey("CurrentJoystickGUID");
                    data["Default"].RemoveKey("CurrentJoystick");
                    data["Default"].RemoveKey("Joy0GUID");
                    data["Default"].RemoveKey("Joy0ID");
                }
                if (joystick2 != null)
                {
                    data["Default"]["Joy1GUID"] = joystick2.guid;
                    data["Default"]["Joy1ID"] = joystick2.id.ToString();
                }
                else
                {
                    data["Default"].RemoveKey("Joy1GUID");
                    data["Default"].RemoveKey("Joy1ID");
                }
                if (joystick3 != null)
                {
                    data["Default"]["Joy2GUID"] = joystick3.guid;
                    data["Default"]["Joy2ID"] = joystick3.id.ToString();
                }
                else
                {
                    data["Default"].RemoveKey("Joy2GUID");
                    data["Default"].RemoveKey("Joy2ID");
                }
                if (joystick4 != null)
                {
                    data["Default"]["Joy3GUID"] = joystick4.guid;
                    data["Default"]["Joy3ID"] = joystick4.id.ToString();
                }
                else
                {
                    data["Default"].RemoveKey("Joy3GUID");
                    data["Default"].RemoveKey("Joy3ID");
                }
                data["Default"]["TextureFilter"] = textureFilter.ToString();

                /* Video Section */
                data["Video"]["Display"] = displayIndex.ToString();

                /* Sound Section */
                data["Sound"]["SampleRate"] = sampleRate.ToString();
                data["Sound"]["EnableEFX"] = Convert.ToInt32(enableEfx).ToString();
                data["Sound"]["PlaybackDevice"] = playbackDevice;
                data["Sound"]["CaptureDevice"] = captureDevice;

                /* Graphics Section */
                if (displayResolution != null)
                {
                    data["Graphics"]["Resolution"] = "{\"width\":" + displayResolution.Split("x")[0] +",\"height\":"+ displayResolution.Split("x")[1] + "}";
                }
                data["Graphics"]["Shadows"] = shadowQuality.ToString();
                data["Graphics"]["AAMode"] = aaPreset.ToString();
                data["Graphics"]["MSAASamples"] = (msaaPreset * 4).ToString(); // recall MSAASamples is in intervals of 4 (0, 4, 8) so convert to preset level
                data["Graphics"]["WindowMode"] = windowMode.ToString();
                data["Graphics"]["Display"] = displayIndex.ToString();
                data["Graphics"]["TextureFilter"] = textureFilter.ToString();
                data["Graphics"]["SoftParticles"] = enableSoftParticles.ToString().ToLower();
                data["Graphics"]["DeferredLighting"] = enableDeferredLighting.ToString().ToLower();
                data["Graphics"]["VSync"] = vsync.ToString().ToLower();
                data["Graphics"]["PostProcessing"] = postProcess.ToString().ToLower();

                /* Input Section */
                if (joystick1 != null)
                {
                    data["Input"]["Joystick"] = "{\"guid\":\"" + joystick1.guid + "\",\"id\":" + joystick1.id + "}";
                }
                else
                {
                    data["Input"].RemoveKey("Joystick");
                }
                if (joystick2 != null)
                {
                    data["Input"]["Joystick1"] = "{\"guid\":\"" + joystick2.guid + "\",\"id\":" + joystick2.id + "}";
                }
                else
                {
                    data["Input"].RemoveKey("Joystick1");
                }
                if (joystick3 != null)
                {
                    data["Input"]["Joystick2"] = "{\"guid\":\"" + joystick3.guid + "\",\"id\":" + joystick3.id + "}";
                }
                else
                {
                    data["Input"].RemoveKey("Joystick2");
                }
                if (joystick4 != null)
                {
                    data["Input"]["Joystick3"] = "{\"guid\":\"" + joystick4.guid + "\",\"id\":" + joystick4.id + "}";
                }
                else
                {
                    data["Input"].RemoveKey("Joystick3");
                }

                data["Input"]["JoystickDeadZone"] = joystickDeadZone.ToString();
                data["Input"]["JoystickSensitivity"] = joystickSensitivity.ToString();
                data["Input"]["MouseSensitivity"] = mouseSensitivity.ToString();

                data["PXO"]["Login"] = pxoLogin;
                data["PXO"]["Password"] = pxoPassword;

                bool wasWatchingIni = false;
                if (iniWatcher != null)
                {
                    wasWatchingIni = iniWatcher.EnableRaisingEvents;
                    iniWatcher.EnableRaisingEvents = false;
                }
                parser.WriteFile(KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "fs2_open.ini", data, new UTF8Encoding(false));
                if(iniWatcher!= null && wasWatchingIni)
                    iniWatcher.EnableRaisingEvents = true;
                Log.Add(Log.LogSeverity.Information, "GlobalSettings.WriteFS2IniValues","Writen ini: "+ KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "fs2_open.ini");
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "GlobalSettings.WriteFS2IniValues()", ex);
            }
        }

        /// <summary>
        /// Write values to settings.json and fs2_open.ini
        /// Writing the ini is optional, it should not be done for Knossos-only settings.
        /// </summary>
        /// <param name="writeIni"></param>
        public void Save(bool writeIni = true) 
        {
            if (writeIni)
            {
                WriteFS2IniValues();
            }
            try
            {
                var options = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "settings.json", json, new UTF8Encoding(false));
                Log.Add(Log.LogSeverity.Information, "GlobalSettings.Save()", "Global settings has been saved.");
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "GlobalSettings.Save()", ex);
            }
        }

        /// <summary>
        /// Convert user-selected settings into FSO cmdline arguments
        /// </summary>
        /// <param name="build"></param>
        /// <returns>cmdline string</returns>
        public string GetSystemCMD(FsoBuild? build = null)
        { 
            var cmd = string.Empty;
            if(shadowQuality > 0)
            {
                cmd += "-enable_shadows";
                cmd += "-shadow_quality " + shadowQuality;
            }
            if(aaPreset > 0)
            {
                if (build == null || SemanticVersion.Compare(build.version, "21.0.0") >= 0)
                {
                    cmd += "-aa";
                    cmd += "-aa_preset " + aaPreset;
                }
                else
                {
                    if (SemanticVersion.Compare(build.version, "3.8.0") >= 0)
                    {
                        if (aaPreset <= 2)
                        {
                            cmd += "-fxaa";
                            switch (aaPreset)
                            {
                                case 0: cmd += "-fxaa_preset 1"; break;
                                case 1: cmd += "-fxaa_preset 5"; break;
                                case 2: cmd += "-fxaa_preset 7"; break;
                            }
                        }
                        else
                        {
                            cmd += "-smaa";
                            switch (aaPreset)
                            {
                                case 3: cmd += "-smaa_preset 0"; break;
                                case 4: cmd += "-smaa_preset 1"; break;
                                case 5: cmd += "-smaa_preset 2"; break;
                                case 6: cmd += "-smaa_preset 3"; break;
                            }
                        }
                    }
                }
            }
            if (msaaPreset > 0)
            {
                switch (msaaPreset)
                {
                    case 1: cmd += "-msaa 4"; break;
                    case 2: cmd += "-msaa 8"; break;
                }
            }
            if (enableSoftParticles)
            {
                cmd += "-soft_particles";
            }
            if (!enableDeferredLighting)
            {
                cmd += "-no_deferred";
            }
            switch (windowMode)
            {
                case 0: cmd += "-window"; break;
                case 1: cmd += "-fullscreen_window"; break;
                case 2: break; //fullscreen
            }
            if (!vsync)
            {
                cmd += "-no_vsync";
            }
            if(!postProcess)
            {
                cmd += "-no_post_process";
            }
            if (noFpsCapping)
            {
                cmd += "-no_fps_capping";
            }
            if(showFps)
            {
                cmd += "-fps";
            }
            if(disableAudio)
            {
                cmd += "-nosound";
            }
            if(disableMusic)
            {
                cmd += "-nomusic";
            }
            return cmd;
        }
    }
}
