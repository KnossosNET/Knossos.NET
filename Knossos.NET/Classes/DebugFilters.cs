using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Knossos.NET.Classes{


    public partial class DebugFilterCategory  : ObservableObject
    {
        public string typeName { get; set; } = string.Empty;

        public partial class DebugFilter
        {        
            [JsonPropertyName("Filter")]
            public string filterName { get; set; } = string.Empty;
            public string description { get; set; } = string.Empty;

            public bool enabled = false;
        }

        [ObservableProperty]
        public ObservableCollection<DebugFilter> questions = new ObservableCollection<DebugFilter>();

        // TODO!  Create a save function
    }
}
