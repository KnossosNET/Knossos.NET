using Knossos.NET.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Knossos.NET.Models
{
    /*
        This class writes and reads all values in the fs2_open.ini file.
        https://wiki.hard-light.net/index.php/Launcher_engine_interaction
    */
    public class Fs2OpenIni
    {
        /**** [Default] ****/
            // "[API]-([width]x[height])x[bits] bit. [API] must be exactly 4 characters long.
            // The only valid option here is "OGL " for using the OpenGL API.
            public string? VideocardFs2open; 

            //This should be the level of anti-aliasing desired for OpenGL rendering. This should be the multiple of 2 for how many samples should be taken (e.g. a value of 3 means 8x MSAA). If set to 0 anti-aliasing will be disabled.
            //Note: As of FSO 3.8.0 this option has no effect!
            public int? OGL_AntiAliasSamples;

            //Sets how textures should be filtered.Valid values are 0 for bilinear filtering and 1 for trilinear filtering.
            public int? TextureFilter;

            //Specifies the level of anisotropic texture filtering if supported by the OpenGL implementation.
            //has no effect by 19.0.0
            public int? OGL_AnisotropicFilter;

            //Specifies the GUID of the primary joystick. The case of alphabetic characters will be normalized so it doesn't matter if this string is upper or lower case. This GUID can be determined by using SDL2 or with the JSON output
            //has no effect since 22.0.0
            public string? CurrentJoystickGUID;

            //Specified the index of the primary joystick. This will only be used if there are multiple joysticks with the same GUID. This is the index used to open the joystick in SDL2 or the index into the joysticks array in the JSON output.
            //has no effect since 22.0.0
            public string? CurrentJoystick;

            //As of FSO 22.0, used for defining your first of multiple joysticks. Get this ID from the ID listed in the exported flags.txt. 
            public int? Joy0ID;
            //As of FSO 22.0, used for defining your first of multiple joysticks. Get this GUID from the GUID listed in the exported flags.txt, matching the entry used for Joy0ID. 
            public string? Joy0GUID;
            public int? Joy1ID;
            public string? Joy1GUID;
            public int? Joy2ID;
            public string? Joy2GUID;
            public int? Joy3ID;
            public string? Joy3GUID;

            //The name of the last pilot used.Used by the engine to determine which pilot should be at the top of the list and pre-loaded.
            public string? LastPlayer;

            //The gamma (brightness) value to be used to display the game.
            //Looks like legacy to me
            public int? GammaD3D;

            //Used to determine if force feedback should be enabled if the joystick supports it. If using the JSON output then it's possible to determine if a joystick supports this by looking at the is_haptic field of the joystick object.
            public int? EnableJoystickFF;

            //This can be used to enable or disable a directional hit effect if Force Feedback is enabled.
            public int? EnableHitEffect;

            //Sets the language of the game. Possible values are "English", "German", "French", and "Polish".
            public string? Language;

            //If set to 1, forces the game to start in full screen mode.
            //Looks like legacy
            public int? ForceFullscreen;

            //The number of screenshots taken by the player.
            public int? ScreenshotNum;

            //Used to set the framerate cap for the game. If set to zero, framerate cap is disabled.
            public int? MaxFPS;

            //Used to force a specific port for multiplayer connections
            public uint? ForcePort;

            //Used for setting how fast the internet connection is. Possible values are "Slow", "56K", "ISDN", "Cable", and "Fast". Defaults to "Fast" for non-windows OS's.
            public string? ConnectionSpeed;

            //Enables or disables text-to-speech in the techroom. Valid values are 1 and 0. Defaults to 0.
            public int? SpeechTechroom;

            //Enables or disables text-to-speech in the briefings. Valid values are 1 and 0. Defaults to 0.
            public int? SpeechBriefings;

            //Enables or disables text-to-speech in game. Valid values are 1 and 0. Defaults to 0.
            public int? SpeechIngame;

            //Enables or disables text-to-speech in the multiplayer. Valid values are 1 and 0. Defaults to 0.
            public int? SpeechMulti;

            //The volume scale of the text-to-speech sound. Should be in the range of 0 to 100.
            public int? SpeechVolume;

            //Sets which voice to use.See JSON output for a cross-platform way of determining which voices are available.
            public int? SpeechVoice;

            //Whether or not to use Banners for multiplayer. Valid values are 1 and 0. Defaults to 1.
            public int? PXOBanners;

            //Use Low Memory mode (Allows 2d animations to drop frames)
            public int? LowMem;

            //Set which processor should be assigned to the game. Defaults to 2. This setting applies only on Windows.
            public int? ProcessorAffinity;

            //The index of the next file to be dds exported from FSO.
            public int? ImageExportNum;

            //LAN
            public string? NetworkConnection;

            //Unsupported [Default] keys
            public List<string> UnsupportedDefault = new List<string>();

        /**** Video ****/
            //Used to choose on which display FSO should be displayed on. The JSON output contains a list of display detected on the system and their index. Alternatively, SDL2 can be used to enumerate this information.
            public int? Display;

            //Unsupported [Video] keys
            public List<string> UnsupportedVideo = new List<string>();


        /**** ForceFeedback ****/
            //A percentage scale of how strong the effect should be. 0 means completely disabled, 100 means full strength.
            public int? Strength;
            //Unsupported [ForceFeedback] keys
            public List<string> UnsupportedForceFeedback = new List<string>();


        /**** Sound ****/
            //Specifies the quality of the sound output. The value should be in [1, 3] where 3 is the highest quality and 1 is the lowest.
            public int? Quality;

            //Explicitly specifies the sample rate the OpenAL context should use.
            public int? SampleRate;

            //Enables or disables usage of OpenAL EFX.
            public int? EnableEFX;

            //Sets the playback device OpenAL should use. The JSON output contains a list of available playback devices.
            public string? PlaybackDevice;

            //Sets the capture device OpenAL should use. The JSON output contains a list of available capture devices.
            public string? CaptureDevice;

            //Unsupported [Sound] keys
            public List<string> UnsupportedSound = new List<string>();


        /**** PXO ****/
            //The login number for the player's account
            public string? Login;

            //The password the player uses for logging into the PXO service (not the account's password)
            public string? Password;

            //The Squad Name the player uses.
            public string? SquadName;

            //Unsupported [PXO] keys
            public List<string> UnsupportedPXO = new List<string>();

        /**** Unsupported Section ****/
        public List<string> UnsupportedSection = new List<string>();

        public Fs2OpenIni()
        {
            if (File.Exists(SysInfo.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "fs2_open.ini"))
            {
                var section = string.Empty;
                using (var fileStream = File.OpenRead(SysInfo.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "fs2_open.ini"))
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    string? line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if(line.Length > 0)
                        {
                            if (line.Contains("[") && line.Contains("]"))
                            {
                                section = line;
                                if (!section.Contains("Default") && !section.Contains("Video") && !section.Contains("ForceFeedback") && !section.Contains("Sound") & !section.Contains("PXO"))
                                {
                                    UnsupportedSection.Add(line);
                                    Log.Add(Log.LogSeverity.Warning, "Fs2OpenIni.Constructor()", "Unsupported section detected on fs2_open.ini: " + line);
                                }
                            }
                            else
                            {
                                if (line.Contains("="))
                                {
                                    if (section.Contains("Default") || section.Contains("Video") || section.Contains("ForceFeedback") || section.Contains("Sound") || section.Contains("PXO"))
                                    {
                                        ParseKey(line, section);
                                        Log.Add(Log.LogSeverity.Information, "Fs2OpenIni.Constructor()", "Parsing key value from fs2_open.ini: " + line + " Section: " + section);
                                    }
                                    else
                                    {
                                        UnsupportedSection.Add(line);
                                        Log.Add(Log.LogSeverity.Warning, "Fs2OpenIni.Constructor()", "Unsupported key for unsupported section detected on fs2_open.ini: " + line + " Section: "+section);
                                    }
                                }
                                else
                                {
                                    Log.Add(Log.LogSeverity.Error, "Fs2OpenIni.Constructor()", "Possible incorrect line detected on fs2_open.ini: " + line);
                                }
                            }
                        }
                    }
                }
            }
        }

        public bool WriteIniFile(FsoBuild build)
        {
            SetValuesFromConfig(build);
            try
            {
                using (StreamWriter inifile = new StreamWriter(SysInfo.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "fs2_open.ini"))
                {
                    inifile.WriteLine("[Default]");
                    if(VideocardFs2open != null)
                    {
                        inifile.WriteLine("VideocardFs2open="+VideocardFs2open);
                    }
                    if (OGL_AntiAliasSamples != null)
                    {
                        inifile.WriteLine("OGL_AntiAliasSamples="+OGL_AntiAliasSamples);
                    }
                    if (TextureFilter != null)
                    {
                        inifile.WriteLine("TextureFilter="+TextureFilter);
                    }
                    if (OGL_AnisotropicFilter != null)
                    {
                        inifile.WriteLine("OGL_AnisotropicFilter="+OGL_AnisotropicFilter);
                    }
                    if (CurrentJoystickGUID != null)
                    {
                        inifile.WriteLine("CurrentJoystickGUID="+CurrentJoystickGUID);
                    }
                    if (CurrentJoystick != null)
                    {
                        inifile.WriteLine("CurrentJoystick="+CurrentJoystick);
                    }
                    if (Joy0ID != null)
                    {
                        inifile.WriteLine("Joy0ID="+Joy0ID);
                    }
                    if (Joy0GUID != null)
                    {
                        inifile.WriteLine("Joy0GUID="+Joy0GUID);
                    }
                    if (Joy1ID != null)
                    {
                        inifile.WriteLine("Joy1ID="+Joy1ID);
                    }
                    if (Joy1GUID != null)
                    {
                        inifile.WriteLine("Joy1GUID="+Joy1GUID);
                    }
                    if (Joy2ID != null)
                    {
                        inifile.WriteLine("Joy2ID="+Joy2ID);
                    }
                    if (Joy2GUID != null)
                    {
                        inifile.WriteLine("Joy2GUID="+Joy2GUID);
                    }
                    if (Joy3ID != null)
                    {
                        inifile.WriteLine("Joy3ID="+Joy3ID);
                    }
                    if (Joy3GUID != null)
                    {
                        inifile.WriteLine("Joy3GUID="+Joy3GUID);
                    }
                    if (LastPlayer != null)
                    {
                        inifile.WriteLine("LastPlayer="+LastPlayer);
                    }
                    if (GammaD3D != null)
                    {
                        inifile.WriteLine("GammaD3D="+GammaD3D);
                    }
                    if (EnableJoystickFF != null)
                    {
                        inifile.WriteLine("EnableJoystickFF="+EnableJoystickFF);
                    }
                    if (EnableHitEffect != null)
                    {
                        inifile.WriteLine("EnableHitEffect="+EnableHitEffect);
                    }
                    if (Language != null)
                    {
                        inifile.WriteLine("Language="+Language);
                    }
                    if (ForceFullscreen != null)
                    {
                        inifile.WriteLine("ForceFullscreen="+ForceFullscreen);
                    }
                    if (ScreenshotNum != null)
                    {
                        inifile.WriteLine("ScreenshotNum="+ScreenshotNum);
                    }
                    if (MaxFPS != null)
                    {
                        inifile.WriteLine("MaxFPS="+MaxFPS);
                    }
                    if (ForcePort != null)
                    {
                        inifile.WriteLine("ForcePort="+ForcePort);
                    }
                    if (ConnectionSpeed != null)
                    {
                        inifile.WriteLine("ConnectionSpeed="+ConnectionSpeed);
                    }
                    if (SpeechTechroom != null)
                    {
                        inifile.WriteLine("SpeechTechroom="+SpeechTechroom);
                    }
                    if (SpeechBriefings != null)
                    {
                        inifile.WriteLine("SpeechBriefings="+SpeechBriefings);
                    }
                    if (SpeechIngame != null)
                    {
                        inifile.WriteLine("SpeechIngame="+SpeechIngame);
                    }
                    if (SpeechMulti != null)
                    {
                        inifile.WriteLine("SpeechMulti="+SpeechMulti);
                    }
                    if (SpeechVolume != null)
                    {
                        inifile.WriteLine("SpeechVolume="+SpeechVolume);
                    }
                    if (SpeechVoice != null)
                    {
                        inifile.WriteLine("SpeechVoice="+SpeechVoice);
                    }
                    if (PXOBanners != null)
                    {
                        inifile.WriteLine("PXOBanners="+PXOBanners);
                    }
                    if (LowMem != null)
                    {
                        inifile.WriteLine("LowMem="+LowMem);
                    }
                    if (ProcessorAffinity != null)
                    {
                        inifile.WriteLine("ProcessorAffinity="+ProcessorAffinity);
                    }
                    if (ImageExportNum != null)
                    {
                        inifile.WriteLine("ImageExportNum="+ImageExportNum);
                    }
                    if (NetworkConnection != null)
                    {
                        inifile.WriteLine("NetworkConnection="+NetworkConnection);
                    }

                    foreach(var unsupported in UnsupportedDefault)
                    {
                        inifile.WriteLine(unsupported);
                    }

                    inifile.WriteLine("");

                    //Video
                    inifile.WriteLine("[Video]");
                    if (Display != null)
                    {
                        inifile.WriteLine("Display="+Display);
                    }

                    foreach (var unsupported in UnsupportedVideo)
                    {
                        inifile.WriteLine(unsupported);
                    }

                    inifile.WriteLine("");

                    //ForceFeedback
                    inifile.WriteLine("[ForceFeedback]");
                    if (Strength != null)
                    {
                        inifile.WriteLine("Strength="+Strength);
                    }

                    foreach (var unsupported in UnsupportedForceFeedback)
                    {
                        inifile.WriteLine(unsupported);
                    }

                    inifile.WriteLine("");

                    //Sound
                    inifile.WriteLine("[Sound]");
                    if (Quality != null)
                    {
                        inifile.WriteLine("Quality="+Quality);
                    }
                    if (SampleRate != null)
                    {
                        inifile.WriteLine("SampleRate="+SampleRate);
                    }
                    if (EnableEFX != null)
                    {
                        inifile.WriteLine("EnableEFX="+EnableEFX);
                    }
                    if (PlaybackDevice != null)
                    {
                        inifile.WriteLine("PlaybackDevice="+PlaybackDevice);
                    }
                    if (CaptureDevice != null)
                    {
                        inifile.WriteLine("CaptureDevice="+CaptureDevice);
                    }

                    foreach (var unsupported in UnsupportedSound)
                    {
                        inifile.WriteLine(unsupported);
                    }

                    inifile.WriteLine("");

                    //PXO
                    inifile.WriteLine("[PXO]");
                    if (Login != null)
                    {
                        inifile.WriteLine("Login="+Login);
                    }
                    if (Password != null)
                    {
                        inifile.WriteLine("Password="+Password);
                    }
                    if (SquadName != null)
                    {
                        inifile.WriteLine("SquadName="+SquadName);
                    }

                    foreach (var unsupported in UnsupportedPXO)
                    {
                        inifile.WriteLine(unsupported);
                    }
                }

            }catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Fs2OpenIni.WriteIniFile()",ex);
                Log.Add(Log.LogSeverity.Error, "Fs2OpenIni.WriteIniFile()", "Error writing the fs2_open.ini file!");
                return false;
            }
            Log.Add(Log.LogSeverity.Information, "Fs2OpenIni.WriteIniFile()", "Writing fs2_opem.ini complete!");
            return true;
        }

        private void SetValuesFromConfig(FsoBuild build)
        {
            var buildVersion = new SemanticVersion(build.version);
            VideocardFs2open = "OGL -(" + Knossos.globalSettings.displayResolution + ")x" + Knossos.globalSettings.displayColorDepth + " bit";
            TextureFilter = Knossos.globalSettings.textureFilter;
            Language = Knossos.globalSettings.fs2Lang;

            //LastPlayer
            //EnableJoystickFF
            //EnableHitEffect
            //ScreenshotNum 
            //MaxFPS
            ForcePort = Knossos.globalSettings.multiPort;

            if(ConnectionSpeed == null)
            {
                ConnectionSpeed = "Fast";
            }
            if(NetworkConnection == null)
            {
                NetworkConnection = "LAN";
            }

            if (Knossos.globalSettings.enableTts)
            {
                SpeechTechroom = Convert.ToInt32(Knossos.globalSettings.ttsTechroom);
                SpeechBriefings = Convert.ToInt32(Knossos.globalSettings.ttsBriefings);
                SpeechIngame = Convert.ToInt32(Knossos.globalSettings.ttsIngame);
                SpeechMulti = Convert.ToInt32(Knossos.globalSettings.ttsMulti);
            }
            else
            {
                SpeechTechroom = 0;
                SpeechBriefings = 0;
                SpeechIngame = 0;
                SpeechMulti = 0;
            }

            SpeechVoice = Knossos.globalSettings.ttsVoice;
            SpeechVolume = Knossos.globalSettings.ttsVolume;

            //PXOBanners
            //LowMem
            //ProcessorAffinity 
            //ImageExportNum

            if (Knossos.globalSettings.displayIndex > 0)
            {
                Display = Knossos.globalSettings.displayIndex;
            }
            else
            {
                Display = null;
            }

            //Strength
            //Quality 

            SampleRate = Knossos.globalSettings.sampleRate;
            EnableEFX = Convert.ToInt32(Knossos.globalSettings.enableEfx);
            PlaybackDevice = Knossos.globalSettings.playbackDevice;
            CaptureDevice = Knossos.globalSettings.captureDevice;

            //Login 
            //Password 
            //SquadName 

            //<22.0.0
            if (SemanticVersion.Compare(buildVersion,new SemanticVersion("22.0.0")) < 0)
            {
                if(Knossos.globalSettings.joystick1 != null)
                {
                    CurrentJoystickGUID = Knossos.globalSettings.joystick1.guid;
                    CurrentJoystick = Knossos.globalSettings.joystick1.id.ToString();
                }
            }
            else
            {
                CurrentJoystickGUID = null;
                CurrentJoystick = null;
                if (Knossos.globalSettings.joystick1 != null)
                {
                    Joy0GUID = Knossos.globalSettings.joystick1.guid;
                    Joy0ID = Knossos.globalSettings.joystick1.id;
                }
                if (Knossos.globalSettings.joystick2 != null)
                {
                    Joy1GUID = Knossos.globalSettings.joystick2.guid;
                    Joy1ID = Knossos.globalSettings.joystick2.id;
                }
                if (Knossos.globalSettings.joystick3 != null)
                {
                    Joy2GUID = Knossos.globalSettings.joystick3.guid;
                    Joy2ID = Knossos.globalSettings.joystick3.id;
                }
                if (Knossos.globalSettings.joystick4 != null)
                {
                    Joy3GUID = Knossos.globalSettings.joystick4.guid;
                    Joy3ID = Knossos.globalSettings.joystick4.id;
                }

            }

            //<3.8.0 (Really legacy options)
            if (SemanticVersion.Compare(buildVersion, new SemanticVersion("3.8.0")) < 0)
            {
                if (Knossos.globalSettings.enableAA)
                {
                    if(Knossos.globalSettings.aaPreset < 3)
                    {
                        OGL_AntiAliasSamples = 1;
                        OGL_AnisotropicFilter = 4;
                    }
                    else
                    {
                        if (Knossos.globalSettings.aaPreset < 6)
                        {
                            OGL_AntiAliasSamples = 2;
                            OGL_AnisotropicFilter = 8;
                        }
                        else
                        {
                            OGL_AntiAliasSamples = 3;
                            OGL_AnisotropicFilter = 16;
                        }
                    }

                }
                else
                {
                    OGL_AntiAliasSamples = 0;
                    OGL_AnisotropicFilter = 0;
                }
            }
        }

        private void ParseKey (string line, string section)
        {
            try
            {
                var key = line.Split('=')[0].Trim();
                var value = line.Split('=')[1].Trim();

                switch (key)
                {
                    //Default
                    case "VideocardFs2open": VideocardFs2open = value; break;
                    case "OGL_AntiAliasSamples": OGL_AntiAliasSamples = int.Parse(value); break;
                    case "TextureFilter": TextureFilter = int.Parse(value); break;
                    case "OGL_AnisotropicFilter": OGL_AnisotropicFilter = int.Parse(value); break;
                    case "CurrentJoystickGUID": CurrentJoystickGUID = value; break;
                    case "CurrentJoystick": CurrentJoystick = value; break;
                    case "Joy0ID": Joy0ID = int.Parse(value); break;
                    case "Joy0GUID": Joy0GUID = value; break;
                    case "Joy1ID": Joy1ID = int.Parse(value); break;
                    case "Joy1GUID": Joy1GUID = value; break;
                    case "Joy2ID": Joy2ID = int.Parse(value); break;
                    case "Joy2GUID": Joy2GUID = value; break;
                    case "Joy3ID": Joy3ID = int.Parse(value); break;
                    case "Joy3GUID": Joy3GUID = value; break;
                    case "LastPlayer": LastPlayer = value; break;
                    case "GammaD3D": GammaD3D = int.Parse(value); break;
                    case "EnableJoystickFF": EnableJoystickFF = int.Parse(value); break;
                    case "EnableHitEffect": EnableHitEffect = int.Parse(value); break;
                    case "Language": Language = value; break;
                    case "ForceFullscreen": ForceFullscreen = int.Parse(value); break;
                    case "ScreenshotNum": ScreenshotNum = int.Parse(value); break;
                    case "MaxFPS": MaxFPS = int.Parse(value); break;
                    case "ForcePort": ForcePort = uint.Parse(value); break;
                    case "ConnectionSpeed": ConnectionSpeed = value; break;
                    case "SpeechTechroom": SpeechTechroom = int.Parse(value); break;
                    case "SpeechBriefings": SpeechBriefings = int.Parse(value); break;
                    case "SpeechIngame": SpeechIngame = int.Parse(value); break;
                    case "SpeechMulti": SpeechMulti = int.Parse(value); break;
                    case "SpeechVolume": SpeechVolume = int.Parse(value); break;
                    case "SpeechVoice": SpeechVoice = int.Parse(value); break;
                    case "PXOBanners": PXOBanners = int.Parse(value); break;
                    case "LowMem": LowMem = int.Parse(value); break;
                    case "ProcessorAffinity": ProcessorAffinity = int.Parse(value); break;
                    case "ImageExportNum": ImageExportNum = int.Parse(value); break;
                    case "NetworkConnection": NetworkConnection = value; break;
                    //Video
                    case "Display": Display = int.Parse(value); break;
                    //ForceFeedback
                    case "Strength": Strength = int.Parse(value); break;
                    //Sound
                    case "Quality": Quality = int.Parse(value); break;
                    case "SampleRate": SampleRate = int.Parse(value); break;
                    case "EnableEFX": EnableEFX = int.Parse(value); break;
                    case "PlaybackDevice": PlaybackDevice = value; break;
                    case "CaptureDevice": CaptureDevice = value; break;
                    //PXO
                    case "Login": Login = value; break;
                    case "Password": Password = value; break;
                    case "SquadName": SquadName = value; break;

                    default:
                        Log.Add(Log.LogSeverity.Warning, "Fs2OpenIni.Constructor()", "Parsing unsuported key value from fs2_open.ini: " + line + " Section: " + section);
                        if (section.Contains("Default"))
                        {
                            UnsupportedDefault.Add(line); break;
                        }
                        if (section.Contains("Video"))
                        {
                            UnsupportedVideo.Add(line); break;
                        }
                        if (section.Contains("Sound"))
                        {
                            UnsupportedSound.Add(line); break;
                        }
                        if (section.Contains("ForceFeedback"))
                        {
                            UnsupportedForceFeedback.Add(line); break;
                        }
                        if (section.Contains("PXO"))
                        {
                            UnsupportedPXO.Add(line); 
                        }
                        break;
                }
            }
            catch(Exception e) 
            {
                Log.Add(Log.LogSeverity.Error, "Fs2OpenIni.ParseKey()", e);
                Log.Add(Log.LogSeverity.Error, "Fs2OpenIni.ParseKey()", "Input line: "+line);
            }
        }
    }
}
