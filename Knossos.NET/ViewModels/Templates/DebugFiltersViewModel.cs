
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
        [ObservableProperty]
        private string customFilter = string.Empty;

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

            foreach(var category in Categories){
                if (category.Typename == "General"){
                    foreach(var filter in category.Filters){
                        filter.Enabled = true;
                    }
                }
            }

            // TODO! Add loading of saved settings here.

            Initialized = true;
        }

        public DebugFiltersViewModel()
        {
            LoadDebugRepo();
        }

        public void AddFilter()
        {
            // If we pressed by accident, ignore
            if (CustomFilter == string.Empty)
            {
                return;
            }

            // Look for a custom category that already exists
            foreach(var category in Categories) {
                // Add new filter to it, if so
                if (category.Typename == "Custom"){
                    category.AddCustom(CustomFilter);

                    // Empty the text box when done.
                    CustomFilter = string.Empty;
                    return;
                }
            }

            // if no custom category exists, add it and then add the new filter
            var customCategory = new DebugFilterCategory();
            customCategory.Typename = "Custom";
            customCategory.AddCustom(CustomFilter);
            Categories.Insert(0, customCategory);

            // Empty the text box when done.
            CustomFilter = string.Empty;
        }
    }

}