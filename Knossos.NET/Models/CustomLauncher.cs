using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Knossos.NET.ViewModels.MainWindowViewModel;

namespace Knossos.NET.Models
{
    /// <summary>
    /// Class to handle the configuration options and optional save file of the SingleTC mode
    /// </summary>
    public static class CustomLauncher
    {
        private static bool _customFileLoaded = false;

        /// <summary>
        /// If left empty Knet will try to pick up the "custom_launcher.json" file.
        /// Change it to a mod id to hardcode SingleTC ON using the default settings set here.
        /// </summary>
        public static string? ModID { get; private set; } = null;

        /// <summary>
        /// If enabled, the FSO data folder will changed to use the ModID instead "FreeSpaceOpen"
        /// This gives this TC its own settings and pilot saving location.
        /// </summary>
        public static bool UseCustomFSODataFolder { get; private set; } = true;

        /// <summary>
        /// This allows Knet to search for launcher updates (or not) at the start.
        /// Disabling it will completely disable launcher updates.
        /// If you are forking this and want to provide your own repo to check for updates
        /// change "GitHubUpdateRepoURL" in Knossos.cs
        /// </summary>
        public static bool AllowLauncherUpdates { get; private set; } = true;

        /// <summary>
        /// Custom title for the launcher window. It is recommended to add the mod name to it
        /// Launcher version is auto-added at the end
        /// </summary>
        public static string WindowTitle { get; private set; } = "Knet Launcher";

        /// <summary>
        /// Starting width size of the launcher window
        /// null for auto
        /// </summary>
        public static int? WindowWidth { get; private set; } = 960;

        /// <summary>
        /// Starting height size of the launcher window
        /// null for auto
        /// </summary>
        public static int? WindowHeight { get; private set; } = 540;

        /// <summary>
        /// Add the regular FSO engine view to the menu
        /// </summary>
        public static bool MenuDisplayEngineEntry { get; private set; } = true;

        /// <summary>
        /// Add the regular Knet debug view to the menu
        /// </summary>
        public static bool MenuDisplayDebugEntry { get; private set; } = true;

        /// <summary>
        /// Add the regular Knet Nebula Login view to the menu
        /// This is intended to allow nebula user creation and login
        /// To download private versions of the TC, normally you do not want this
        /// buts its there as an option
        /// </summary>
        public static bool MenuDisplayNebulaLoginEntry { get; private set; } = false;

        /// <summary>
        /// Display the regular Knossos settings menu item
        /// If you do this you may want to add "-no_ingame_options" to the custom cmdline
        /// </summary>
        public static bool MenuDisplayGlobalSettingsEntry { get; private set; } = false;

        /// <summary>
        /// Yet another cmdline option, pass it as a string array. 
        /// It has the lowest priority, same options can be overriden by mod cmdline.
        /// </summary>
        public static string[]? CustomCmdlineArray { get; private set; }

        /// <summary>
        /// Disabling this disconnects the launcher from Nebula completely. Meaning.
        /// It can not install, update or modify installations. And it cant do api calls and will not get repo_minimal.json
        /// It is only good to provide static game files not meant to be changed or be updated by a 3rd party app or service
        /// </summary>
        public static bool UseNebulaServices { get; private set; } = true;

        /// <summary>
        /// Whatever to write or not the Knossos.log file to the datafolder, disabling it will prevent this file from be written.
        /// But the output to the debug console will still be there if you use it.
        /// </summary>
        public static bool WriteLogFile { get; private set; } = true;

        /// <summary>
        /// Call this AFTER checking if we are in portable mode or not.
        /// The first time it runs it will try to load the "custom_launcher.json" if ModID is null
        /// </summary>
        public static bool IsCustomMode
        {
            get
            {
                if (ModID == null && !_customFileLoaded)
                {
                    ReadCustomFile();
                }
                return ModID != null;
            }
        }

        /// <summary>
        /// Try read "custom_launcher.json"
        /// Possible paths:
        /// Portable mode ON:
        /// "./kn_portable/KnossosNET/custom_launcher.json"
        /// Portable mode OFF
        /// "./custom_launcher.json"
        /// (same path as the launcher executable)
        /// Normal data folder is not used in this case to avoid conflict with multiple custom launchers
        /// </summary>
        private static void ReadCustomFile()
        {
            try
            {
                _customFileLoaded = true;
                var filePath = Knossos.inPortableMode ? Path.Combine(KnUtils.GetKnossosDataFolderPath(), "custom_launcher.json") : 
                    Path.Combine(KnUtils.KnetFolderPath!, "custom_launcher.json");
                
                if (File.Exists(filePath))
                {
                    Log.Add(Log.LogSeverity.Information, "CustomLauncher.ReadCustomFile()", "Loading custom launcher data...");
                    using FileStream jsonFile = File.OpenRead(filePath);
                    var customData = JsonSerializer.Deserialize<CustomFileData>(jsonFile)!;

                    if(customData.ModID != null)
                        ModID = customData.ModID;

                    if(customData.UseCustomFSODataFolder.HasValue)
                        UseCustomFSODataFolder = customData.UseCustomFSODataFolder.Value;

                    if (customData.AllowLauncherUpdates.HasValue)
                        AllowLauncherUpdates = customData.AllowLauncherUpdates.Value;

                    if (customData.WindowTitle != null)
                        WindowTitle = customData.WindowTitle;

                    if (customData.WindowWidth != null)
                        WindowWidth = customData.WindowWidth;

                    if (customData.WindowHeight != null)
                        WindowHeight = customData.WindowHeight;

                    if (customData.MenuDisplayEngineEntry.HasValue)
                        MenuDisplayEngineEntry = customData.MenuDisplayEngineEntry.Value;

                    if (customData.MenuDisplayDebugEntry.HasValue)
                        MenuDisplayDebugEntry = customData.MenuDisplayDebugEntry.Value;

                    if (customData.MenuDisplayNebulaLoginEntry.HasValue)
                        MenuDisplayNebulaLoginEntry = customData.MenuDisplayNebulaLoginEntry.Value;

                    if (customData.MenuDisplayGlobalSettingsEntry.HasValue)
                        MenuDisplayGlobalSettingsEntry = customData.MenuDisplayGlobalSettingsEntry.Value;

                    CustomCmdlineArray = customData.CustomCmdlineArray;

                    if (customData.UseNebulaServices.HasValue)
                        UseNebulaServices = customData.UseNebulaServices.Value;

                    if (customData.WriteLogFile.HasValue)
                        WriteLogFile = customData.WriteLogFile.Value;

                    jsonFile.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "CustomLauncher.ReadCustomFile()", ex);
            }
        }

        struct CustomFileData
        {
            public string? ModID { get; set; }
            public bool? UseCustomFSODataFolder { get; set; }
            public bool? AllowLauncherUpdates { get; set; }
            public string? WindowTitle { get; set; }
            public int? WindowWidth { get; set; }
            public int? WindowHeight { get; set; }
            public bool? MenuDisplayEngineEntry { get; set; }
            public bool? MenuDisplayDebugEntry { get; set; }
            public bool? MenuDisplayNebulaLoginEntry { get; set; }
            public bool? MenuDisplayGlobalSettingsEntry { get; set; }
            public string[]? CustomCmdlineArray { get; set; }
            public bool? UseNebulaServices { get; set; }
            public bool? WriteLogFile { get; set; }
        }
    }
}
