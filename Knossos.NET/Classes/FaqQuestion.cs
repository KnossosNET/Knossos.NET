using System.Text.Json;
using System.Text.Json.Serialization;

namespace Knossos.NET.Classes{


    public class QuestionCategory
    {
        public string Name { get; set; } = string.Empty;
        public Question[]? Questions { get; set; }
    }


    public class Question
    {        
        [JsonPropertyName("Question")]
        public string question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }
}
