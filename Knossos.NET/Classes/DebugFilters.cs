using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Knossos.NET.Classes{

    public partial class DebugFilterCategory  : ObservableObject
    {
        public partial class DebugFilter : ObservableObject
        {        
            [JsonPropertyName("Filter"), ObservableProperty]
            internal string filter = string.Empty;
            [JsonPropertyName("Description"), ObservableProperty]
            internal string description = string.Empty;

            [ObservableProperty]
            internal bool enabled = false;
        }

        [ObservableProperty]
        internal string typename = string.Empty;
        [ObservableProperty]
        internal ObservableCollection<DebugFilter> filters = new ObservableCollection<DebugFilter>();

        // TODO!  Create a save function
    }
}
