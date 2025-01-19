﻿using System;
using System.IO;
using System.Text.Json;

namespace Knossos.NET.Models
{
    /// <summary>
    /// Data struct for the custom mode dynamic link button
    /// </summary>
    public struct LinkButton
    {
        public string ToolTip { get; set; }
        public string IconPath { get; set; }
        public string LinkURL { get; set; }
    }

    public struct CustomMenuButton
    {
        public string Name { get; set; }
        public string ToolTip { get; set; }
        public string IconPath { get; set; }
        public string Type { get; set; } //HtmlContent
        public string LinkURL { get; set; }
    }

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
        /// This is also the min width
        /// null for auto
        /// </summary>
        public static int? WindowWidth { get; private set; } = 1024;

        /// <summary>
        /// Starting height size of the launcher window
        /// This is also the min height
        /// null for auto
        /// </summary>
        public static int? WindowHeight { get; private set; } = 540;

        /// <summary>
        /// The first time the user opens the launcher, the main menu should be expanded or collapsed?
        /// After that it will use the saved state
        /// </summary>
        public static bool MenuOpenFirstTime { get; private set; } = false;

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
        /// Display the regular Knossos community menu item
        /// </summary>
        public static bool MenuDisplayCommunityEntry { get; private set; } = false;

        /// <summary>
        /// Display the regular Knossos settings menu item
        /// If you do this you may want to add "-no_ingame_options" to the custom cmdline
        /// </summary>
        public static bool MenuDisplayGlobalSettingsEntry { get; private set; } = false;

        /// <summary>
        /// Add custom buttons to the menu
        /// </summary>
        public static CustomMenuButton[]? CustomMenuButtons { get; private set; }

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
        /// Path to the background image for the home view
        /// It is recommended this image to be about 200px less in width than the starting WindowWidth
        /// Supports local image in the Knet data folder, a local full path, harcoded image or remote https:// URL
        /// Supports APNGs, GIF, PNG and JPG
        /// Examples:
        /// Harcoded Image:
        /// "avares://Knossos.NET/Assets/fs2_res/kn_screen_0.jpg"
        /// Data Folder image (same path to were the repo_minimal.json is downloaded for the current running mode):
        /// "GgqNPDqW0AAMR80.png"
        /// Remote Image (will be cached locally):
        /// "https://video-meta.humix.com/poster/h2YKfXkqITvJ/pKvBUijWRO2_IChwVu.jpg"
        /// </summary>
        public static string? HomeBackgroundImage { get; private set; } = "avares://Knossos.NET/Assets/general/custom_home_background.jpg";

        /// <summary>
        /// Set a path to the welcome HTML message on home screen
        /// Uses the same path rules as HomeBackgroundImage
        /// null to disable or put a path to a empty file if you want to display it at some point
        /// </summary>
        public static string? HomeWelcomeHtml { get; private set; } = null;

        /// <summary>
        /// Thickness string to use as margin for the WelcomeHTML display
        /// left, up, right, down
        /// </summary>
        public static string? HomeWelcomeMargin { get; private set; } = "50,50,50,0";

        /// <summary>
        /// Optional Link buttons that are displayed in the home screen that
        /// if clicked opens a external web link in user browser
        /// Icon path follows the same rules as HomeBackgroundImage, so URL, embedded and local images are supported.
        /// </summary>
        public static LinkButton[]? HomeLinkButtons { get; private set; }

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

                    if (customData.MenuOpenFirstTime.HasValue)
                        MenuOpenFirstTime = customData.MenuOpenFirstTime.Value;

                    if (customData.MenuDisplayEngineEntry.HasValue)
                        MenuDisplayEngineEntry = customData.MenuDisplayEngineEntry.Value;

                    if (customData.MenuDisplayDebugEntry.HasValue)
                        MenuDisplayDebugEntry = customData.MenuDisplayDebugEntry.Value;

                    if (customData.MenuDisplayNebulaLoginEntry.HasValue)
                        MenuDisplayNebulaLoginEntry = customData.MenuDisplayNebulaLoginEntry.Value;

                    if (customData.MenuDisplayGlobalSettingsEntry.HasValue)
                        MenuDisplayGlobalSettingsEntry = customData.MenuDisplayGlobalSettingsEntry.Value;

                    if (customData.MenuDisplayCommunityEntry.HasValue)
                        MenuDisplayCommunityEntry = customData.MenuDisplayCommunityEntry.Value;

                    CustomMenuButtons = customData.CustomMenuButtons;

                    CustomCmdlineArray = customData.CustomCmdlineArray;

                    if (customData.UseNebulaServices.HasValue)
                        UseNebulaServices = customData.UseNebulaServices.Value;

                    if (customData.WriteLogFile.HasValue)
                        WriteLogFile = customData.WriteLogFile.Value;

                    if (customData.HomeBackgroundImage != null)
                        HomeBackgroundImage = customData.HomeBackgroundImage;

                    if (customData.HomeWelcomeHtml != null)
                        HomeWelcomeHtml = customData.HomeWelcomeHtml;

                    if (customData.HomeWelcomeMargin != null)
                        HomeWelcomeMargin = customData.HomeWelcomeMargin;

                    HomeLinkButtons = customData.HomeLinkButtons;

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
            public bool? MenuOpenFirstTime { get; set; }
            public bool? MenuDisplayEngineEntry { get; set; }
            public bool? MenuDisplayDebugEntry { get; set; }
            public bool? MenuDisplayNebulaLoginEntry { get; set; }
            public bool? MenuDisplayGlobalSettingsEntry { get; set; }
            public bool? MenuDisplayCommunityEntry { get; set; }
            public string[]? CustomCmdlineArray { get; set; }
            public bool? UseNebulaServices { get; set; }
            public bool? WriteLogFile { get; set; }
            public string? HomeBackgroundImage { get; set; }
            public string? HomeWelcomeHtml { get; set; }
            public string? HomeWelcomeMargin { get; set; }
            public LinkButton[]? HomeLinkButtons { get; set; }
            public CustomMenuButton[]? CustomMenuButtons { get; set; }
        }
    }
}