using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Knossos.NET.Classes{


    public partial class QuestionCategory  : ObservableObject
    {
        public string Name { get; set; } = string.Empty;

        public partial class FAQQuestion
        {        
            [JsonPropertyName("Question")]
            public string question { get; set; } = string.Empty;
            public string Answer { get; set; } = string.Empty;
        }

        [ObservableProperty]
        public ObservableCollection<FAQQuestion> questions = new ObservableCollection<FAQQuestion>();
    }


}
