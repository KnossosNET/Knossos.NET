using System.Text.Json;
using System.Text.Json.Serialization;

namespace Knossos.NET.Classes{


    public class QuestionCategory
    {
        [JsonPropertyName("Name")]
        public string name { get; set; } = string.Empty;
        public Question[]? packages { get; set; }
    }


    public class Question
    {        
        [JsonPropertyName("Question")]
        public string question { get; set; } = string.Empty;
        [JsonPropertyName("Answer")]
        public string answer { get; set; } = string.Empty;
    }
}
