using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Knossos.NET.ViewModels;

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

            public void SaveConfig(){
                if (DebugFiltersViewModel.Instance != null){
                    DebugFiltersViewModel.Instance.SaveConfig();
                }
            }

        }

        [ObservableProperty]
        internal string typename = string.Empty;
        [ObservableProperty]
        internal ObservableCollection<DebugFilter> filters = new ObservableCollection<DebugFilter>();

        public void AddCustom(string customFilter, bool enabled){
            var newFilter = new DebugFilter();
            
            // Strings longer than 31 trigger an assert in FSO.
            // We could remove the assert, but we still need to support older builds.
            if (customFilter.Length > 31)
            {
                customFilter = customFilter.Trim().Substring(0, 31);
            }

            newFilter.Filter = customFilter;
            newFilter.Enabled = enabled;

            Filters.Add(newFilter);
        }
    }
}
