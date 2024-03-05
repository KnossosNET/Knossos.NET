
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using Avalonia.Threading;


namespace Knossos.NET.ViewModels
{
    /// <summary>
    /// Debug Filter View Model
    /// </summary>
    public partial class DebugFiltersViewModel : ViewModelBase 
    {
        [ObservableProperty]
        internal ObservableCollection<DebugFilterCategory> categories = new ObservableCollection<DebugFilterCategory>();
        [ObservableProperty]
        private bool initialized = false;

        public async Task LoadDebugRepo()
        {
            if (Initialized)
                return;

            try
            {
                HttpResponseMessage response = await KnUtils.GetHttpClient().GetAsync(Knossos.debugFilterURL).ConfigureAwait(false);
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var debugRepo = JsonSerializer.Deserialize<DebugFilterCategory[]>(json)!;
                if(debugRepo != null)
                {
                    foreach(var category in debugRepo)
                    {
                        Categories.Insert(Categories.Count, category);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "DebugFilterViewModel.LoadDebugRepo()", ex);
                return;
            }

            // TODO! Add loading of saved settings here.

            Initialized = true;

        }
    }
}