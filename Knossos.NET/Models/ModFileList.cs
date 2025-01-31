using System.Text.Json.Serialization;

namespace Knossos.NET.Models
{
    public class ModFilelist
    {
        public string? filename { get; set; }  // file path
        public string? archive { get; set; }
        [JsonPropertyName("orig_name")]
        public string? origName { get; set; }  // name in archive 
        public string[]? checksum { get; set; } // sha256, value
    }
}
