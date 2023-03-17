
using System.Text.Json;
using System;
using System.Text.Json.Serialization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Knossos.NET.Classes;

namespace Knossos.NET.Models
{
    /*
        Stores and load the global Knossos.NET configuration.
    */
    public class GlobalSettings
    {
        /* Knossos Settings */
        [JsonPropertyName("base_path")]
        public string? basePath { get; set; } = null;
        [JsonPropertyName("enable_log")]
        public bool enableLogFile { get; set; } = true;
        [JsonPropertyName("log_level")]
        public int logLevel { get; set; } = 1;

        /* Video Settings */
        [JsonPropertyName("display_resolution")]
        public string? displayResolution { get; set; } = null;
        [JsonPropertyName("display_index")]
        public int displayIndex { get; set; } = 0;
        [JsonPropertyName("display_color_depth")]
        public int displayColorDepth { get; set; } = 32;
        [JsonPropertyName("texture_filter")]
        public int textureFilter { get; set; } = 1;
        [JsonPropertyName("enable_shadows")]
        public bool enableShadows { get; set; } = true;
        [JsonPropertyName("shadow_quality")]
        public int shadowQuality { get; set; } = 3;
        [JsonPropertyName("enable_aa")]
        public bool enableAA { get; set; } = true;
        [JsonPropertyName("aa_preset")]
        public int aaPreset { get; set; } = 4;
        [JsonPropertyName("enable_soft_particles")]
        public bool enableSoftParticles { get; set; } = true;
        [JsonPropertyName("run_in_window")]
        public bool runInWindow { get; set; } = false;
        [JsonPropertyName("borderless_window")]
        public bool borderlessWindow { get; set; } = false;
        [JsonPropertyName("no_vsync")]
        public bool noVsync { get; set; } = false;
        [JsonPropertyName("no_post_process")]
        public bool noProstProcess { get; set; } = false;
        [JsonPropertyName("no_fps_capping")]
        public bool noFpsCapping { get; set; } = false;
        [JsonPropertyName("show_fps")]
        public bool showFps { get; set; } = false;

        /* AUDIO SETTINGS */
        [JsonPropertyName("playback_device")]
        public string? playbackDevice { get; set; } = null;
        [JsonPropertyName("capture_device")]
        public string? captureDevice { get; set; } = null;
        [JsonPropertyName("disable_audio")]
        public bool disableAudio { get; set; } = false;
        [JsonPropertyName("disable_music")]
        public bool disableMusic { get; set; } = false;
        [JsonPropertyName("sample_rate")]
        public int sampleRate { get; set; } = 44100;
        [JsonPropertyName("enable_efx")]
        public bool enableEfx { get; set; } = false;
        [JsonPropertyName("enable_tts")]
        public bool enableTts { get; set; } = true;
        [JsonPropertyName("tts_voice")]
        public int? ttsVoice { get; set; } = null;
        [JsonPropertyName("tts_techroom")]
        public bool ttsTechroom { get; set; } = true;
        [JsonPropertyName("tts_briefings")]
        public bool ttsBriefings { get; set; } = true;
        [JsonPropertyName("tts_ingame")]
        public bool ttsIngame { get; set; } = true;
        [JsonPropertyName("tts_multi")]
        public bool ttsMulti { get; set; } = true;
        [JsonPropertyName("tts_description")]
        public bool ttsDescription { get; set; } = true;
        [JsonPropertyName("tts_volume")]
        public int ttsVolume { get; set; } = 100;
        [JsonPropertyName("tts_voice_name")]
        public string? ttsVoiceName { get; set; } = null;

        /* JOYSTICKS */
        public Joystick? joystick1 { get; set; } = null;
        public Joystick? joystick2 { get; set; } = null;
        public Joystick? joystick3 { get; set; } = null;
        public Joystick? joystick4 { get; set; } = null;

        /*GLOBAL FSO*/
        [JsonPropertyName("global_cmdline")]
        public string? globalCmdLine { get; set; } = null;

        [JsonPropertyName("fs2_lang")]
        public string fs2Lang { get; set; } = "English";

        [JsonPropertyName("multiplayer_port")]
        public uint multiPort { get; set; } = 7808;

        public void Save() 
        {
            try
            {
                var encoderSettings = new TextEncoderSettings();
                encoderSettings.AllowRange(UnicodeRanges.All);

                var options = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.Create(encoderSettings),
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "settings.json", json, Encoding.UTF8);
                Log.Add(Log.LogSeverity.Information, "GlobalSettings.Save()", "Global settings has been saved.");
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "GlobalSettings.Save()", ex);
            }
        }

        public string GetSystemCMD(FsoBuild? build = null)
        { 
            var cmd = string.Empty;
            if(enableShadows && shadowQuality > 0)
            {
                cmd += "-enable_shadows";
                cmd += "-shadow_quality " + shadowQuality;
            }
            if(enableAA)
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
                        if (aaPreset <= 3)
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
            if(enableSoftParticles)
            {
                cmd += "-soft_particles";
            }
            if (runInWindow)
            {
                cmd += "-window";
            }
            if (borderlessWindow)
            {
                cmd += "-fullscreen_window";
            }
            if (noVsync)
            {
                cmd += "-no_vsync";
            }
            if(noProstProcess)
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
