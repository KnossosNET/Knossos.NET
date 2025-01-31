using System;
using System.Text.Json.Serialization;

namespace Knossos.NET.Models
{
    public class ModFile
    {
        public string? filename { get; set; }
        public string? dest { get; set; } // "<destination path>"
        public string[]? checksum { get; set; } // sha256, value
        public Int64 filesize { get; set; } // Size in bytes
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string[]? urls { get; set; } // The URLs are full URLs (they contain the filename).
    }
}
