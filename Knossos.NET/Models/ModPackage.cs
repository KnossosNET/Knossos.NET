using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Knossos.NET.Models
{
    public class ModPackage
    {
        public string name { get; set; } = string.Empty; // required
        public string? notes { get; set; } = string.Empty; // optional
        /*
            optional, default: "recommended"
            A feature can be:
            - "required" (always installed with the mod, in fact these are the base files of the mod),
            - "recommended" (automatically selected for installation, but the user can skip them),
            - "optional" (not automatically selected, but user can add them during the install process)
        */
        public string? status { get; set; } // "<required|recommended|optional>"
        public ModDependency[]? dependencies { get; set; } = new ModDependency[0];
        public string? environment { get; set; } // optional, boolean expression like "windows && X86_64 && (sse || sse2)"
        public string? folder { get; set; }
        [JsonPropertyName("is_vp")]
        public bool isVp { get; set; } // optional, whether Knossos should pack the files in a VP on upload, default: false
        public ModFile[]? files { get; set; } = new ModFile[0];
        public ModFilelist[]? filelist { get; set; } = new ModFilelist[0];
        public List<ModExecutable>? executables { get; set; } = new List<ModExecutable>();// optional
        [JsonPropertyName("check_notes")]
        public object? checkNotes { get; set; }

        /* Knet Only */
        /// <summary>
        /// used for pkg display in a checkbox.
        /// NOT Saved in the json
        /// </summary>
        [JsonIgnore]
        public bool isSelected { get; set; } = false;
        /// <summary>
        /// used for display (to enable/disabled chkbox) and to enable/disable the package in devmode
        /// Saved in the json
        /// </summary>
        public bool isEnabled { get; set; } = true;
        /// <summary>
        /// Used to display checkbox tooltip
        /// NOT saved in the Json
        /// </summary>
        [JsonIgnore]
        public string tooltip { get; set; } = string.Empty;
        /// <summary>
        /// Used to indicate a pkg is needed during mod install checkbox selection
        /// Used to change checkbox foreground color during mod install/modify display
        /// </summary>
        [JsonIgnore]
        public bool isRequired { get; set; } = false;
        [JsonIgnore]
        public int buildScore { get; set; } = 0;
    }
}
