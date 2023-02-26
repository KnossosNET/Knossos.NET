
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace Knossos.NET.Models
{
    /*
        Model for the mod_settings.json file saved at the root of the mod folder to store custom user settings for the mod.
        This works as a "extension" of the mod.json
        All default values must be null for non-change.
    */
    public class ModSettings
    {
        [JsonPropertyName("custom_dependencies")]
        public List<ModDependency>? customDependencies { get; set; } = null;

        [JsonPropertyName("custom_mod_flags")]
        public List<string>? customModFlags { get; set; } = null;

        [JsonPropertyName("custom_build_id")]
        public string? customBuildId { get; set; } = null;

        [JsonPropertyName("custom_build_version")]
        public string? customBuildVersion { get; set; } = null;

        [JsonPropertyName("custom_build_exec")]
        public string? customBuildExec { get; set; } = null;

        [JsonPropertyName("custom_cmdline")]
        public string? customCmdLine { get; set; } = null;

        [JsonIgnore]
        private string? filePath = null;

        public ModSettings()
        {

        }

        public void Load(string fullPath)
        {
            try
            {
                this.filePath = fullPath + @"\mod_settings.json";
                if(File.Exists(filePath))
                {
                    string jsonString = File.ReadAllText(filePath);
                    var tempSettings = JsonSerializer.Deserialize<ModSettings>(jsonString);
                    if (tempSettings != null)
                    {
                        customDependencies = tempSettings.customDependencies;
                        customModFlags = tempSettings.customModFlags;
                        customBuildId = tempSettings.customBuildId;
                        customBuildVersion= tempSettings.customBuildVersion;
                        customCmdLine = tempSettings.customCmdLine;
                        customBuildExec = tempSettings.customBuildExec;
                        Log.Add(Log.LogSeverity.Information, "ModSettings.Load()", "Mod seetings has been loaded from " + filePath);
                    }
                    
                }
                else
                {
                    Log.Add(Log.LogSeverity.Information, "ModSettings.Load()", "File mod_settings.json " + filePath + " does not exist.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "ModSettings.Load()", ex);
            }
        }

        public void Save()
        {
            try
            {
                if(filePath!= null)
                {
                    var encoderSettings = new TextEncoderSettings();
                    encoderSettings.AllowRange(UnicodeRanges.All);

                    var options = new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.Create(encoderSettings),
                        WriteIndented = true
                    };

                    var json = JsonSerializer.Serialize(this, options);
                    File.WriteAllText(filePath, json, Encoding.UTF8);
                    Log.Add(Log.LogSeverity.Information, "ModSettings.Save()", "Mod seetings has been saved to "+filePath);
                }
                else
                {
                    Log.Add(Log.LogSeverity.Error, "ModSettings.Save()", "A mod tried to save mod_settings.json to a null filePath, this happens if you try to Save() whiout calling Load() first.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "ModSettings.Save()", ex);
            }
        }
    }
}
