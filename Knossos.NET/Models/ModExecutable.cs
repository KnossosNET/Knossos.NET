using System.Text.Json.Serialization;

namespace Knossos.NET.Models
{
    public class ModExecutable
    {
        public string? file { get; set; } // required, path to the executable (*.exe file on Windows), relative to the mod folder
        public string? label { get; set; } // <Fred FastDebug|FRED2|QTFred|QTFred FastDebug|FastDebug|null> optional, should be null for release builds and contain the name for others
        public ModProperties? properties { get; set; }

        [JsonIgnore]
        public int score { get; set; } = 0;
    }
}
