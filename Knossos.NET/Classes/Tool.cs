using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace Knossos.NET.Classes
{
    public class Tool
    {
        public string name { get; set; } = string.Empty;
        public string version { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        [JsonPropertyName("last_update")]
        public string lastUpdate { get; set; } = string.Empty;
        public string cmdline { get; set; } = string.Empty;
        public ToolPackage[]? packages { get; set; }
        [JsonPropertyName("favorite")]
        public bool isFavorite { get; set; } = false;
        [JsonIgnore]
        private string folderpath { get; set; } = string.Empty;
        [JsonIgnore]
        private bool scoresSet { get; set; } = false;
        [JsonIgnore]
        public bool isInstalled
        {
            get
            {
                if (folderpath != string.Empty)
                    return true;
                return false;
            }
        }

        public Tool()
        {
        }

        public Tool(string folderpath)
        {
            try
            {
                using(var jsonFile = File.OpenRead(folderpath + Path.DirectorySeparatorChar + "tool.json"))
                {
                    var temptool = JsonSerializer.Deserialize<Tool>(jsonFile)!;
                    if (temptool != null) 
                    { 
                        name = temptool.name;
                        version = temptool.version;
                        description = temptool.description;
                        lastUpdate = temptool.lastUpdate;
                        cmdline = temptool.cmdline;
                        packages = temptool.packages;
                        isFavorite = temptool.isFavorite;
                    }
                }
                this.folderpath = folderpath;
            }
            catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Tool.Constructor()", ex);
            }
        }

        /// <summary>
        /// Open a tool
        /// </summary>
        /// <param name="workingFolder"></param>
        /// <returns>true or false depending if the process start was successfull</returns>
        public bool Open(string? workingFolder = null)
        {
            var best = GetBestPackage();
            if (folderpath != string.Empty && best != null)
            {
                try
                {
                    if (!KnUtils.IsWindows)
                        KnUtils.Chmod(Path.Combine(folderpath, best.executablePath));
                    using (var process = new Process())
                    {
                        process.StartInfo.FileName = Path.Combine(folderpath, best.executablePath);
                        if(cmdline != string.Empty)
                            process.StartInfo.Arguments = cmdline;
                        if(workingFolder != null)
                            process.StartInfo.WorkingDirectory = workingFolder;
                        process.StartInfo.UseShellExecute = true;
                        process.Start();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Add(Log.LogSeverity.Error, "Tool.Open()", ex);
                }
            }
            else
            {
                Log.Add(Log.LogSeverity.Error, "Tool.Open()", "Invalid platform or folderpath is empty. Tool: " + name);
            }
            return false;
        }

        /// <summary>
        /// Deletes a tool, it deletes the physical file and removes it from the knossos installed tool list
        /// It is availble to re download after this
        /// </summary>
        public void Delete()
        {
            if (folderpath != string.Empty)
            {
                try
                {
                    Directory.Delete(folderpath, true);
                    Knossos.RemoveTool(this);
                    folderpath = string.Empty;
                }
                catch(Exception ex)
                {
                    Log.Add(Log.LogSeverity.Error, "Tool.Delete()", ex);
                }
            }
        }

        /// <summary>
        /// Saves tool Json
        /// If path is passed it is updated internally before saving the json
        /// That it is used when we are installing a tool, since tools that arent installed does not have a folderpath
        /// </summary>
        /// <param name="path"></param>
        public void SaveJson(string? path = null)
        {
            try
            {
                if (path != null)
                    folderpath = path;
                if (folderpath != null)
                {
                    var options = new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        WriteIndented = true
                    };
                    var json = JsonSerializer.Serialize(this, options);
                    File.WriteAllText(folderpath + Path.DirectorySeparatorChar + "tool.json", json, new UTF8Encoding(false));
                    Log.Add(Log.LogSeverity.Information, "Tool.SaveJson", "tool.json has been saved to " + folderpath + Path.DirectorySeparatorChar + "tool.json");
                }
                else
                {
                    Log.Add(Log.LogSeverity.Error, "Tool.SaveJson", "A tool " + name + " tried to save tool.json to a null folderpath.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Tool.SaveJson", ex);
            }
        }

        /// <summary>
        /// Get string for the download URL of the best package for this system
        /// </summary>
        /// <returns>url for download or null if not valid package is found</returns>
        public string? GetDownloadURL()
        {
            if(IsValidPlatform())
            {
                return GetBestPackage()?.downloadUrl;
            }
            return null;
        }

        /// <summary>
        /// Gets the best package for this system OS and cpu arch
        /// </summary>
        /// <returns>Toolpackage or null if none was found</returns>
        public ToolPackage? GetBestPackage()
        {
            try
            {
                SetPkgScores();
                if (packages != null && packages.Any())
                {
                    return packages.MaxBy(x => x.score);
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Determine if this tool can be installed and run on this system
        /// </summary>
        /// <returns>true or false</returns>
        public bool IsValidPlatform()
        {
            try
            {
                SetPkgScores();
                if (packages != null && packages.Any())
                {
                    return packages.FirstOrDefault(x => x.score > 0) != null;
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Sets the packages scores by OS and cpu arch
        /// </summary>
        private void SetPkgScores()
        {
            if (scoresSet)
                return;
            try
            {
                if (packages != null && packages.Any())
                {
                    foreach (var pkg in packages)
                    {
                        pkg.score = 0;
                        pkg.os = pkg.os.ToLower().Replace(" ", "");
                        var archs = pkg.cpuArch.ToLower().Replace(" ", "").Split(",");
                        if (pkg.os == "windows" && KnUtils.IsWindows || pkg.os == "linux" && KnUtils.IsLinux || pkg.os == "macosx" && KnUtils.IsMacOS)
                        {
                            foreach(var arch in archs)
                            {
                                if(arch == "x64" && KnUtils.CpuArch == "X64" || arch == "x86" && KnUtils.CpuArch == "X86" || arch == "arm64" && KnUtils.CpuArch == "Arm64")
                                {
                                    pkg.score = 100;
                                    continue;
                                }
                                if(KnUtils.IsWindows || KnUtils.IsMacOS)
                                {
                                    if (arch == "x86" && KnUtils.CpuArch == "X64")
                                    {
                                        pkg.score = 50;
                                        continue;
                                    }
                                    if (arch == "x86" && KnUtils.CpuArch == "Arm64")
                                    {
                                        pkg.score = 20;
                                        continue;
                                    }
                                    if (arch == "x64" && KnUtils.CpuArch == "Arm64")
                                    {
                                        pkg.score = 70;
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                    scoresSet = true;
                }
            }
            catch { }
        }
    }

    public class ToolPackage
    {
        [JsonPropertyName("download_url")]
        public string downloadUrl { get; set; } = string.Empty;
        public string os { get; set; } = string.Empty;
        [JsonPropertyName("cpu_arch")]
        public string cpuArch { get; set; } = string.Empty;
        [JsonPropertyName("executable_path")]
        public string executablePath { get; set; } = string.Empty;
        [JsonIgnore]
        public int score { get; set; } = 0;
    }
}
