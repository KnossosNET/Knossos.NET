
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using Avalonia.Threading;


namespace Knossos.NET.ViewModels
{
    /// <summary>
    /// Debug Filter View Model
    /// </summary>
    public partial class DebugFiltersViewModel : ViewModelBase 
    {
        public static DebugFiltersViewModel? Instance { get; set; }

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

            foreach(var category in Categories)
            {
                if (category.Typename == "General")
                {
                    foreach(var filter in category.Filters)
                    {
                        filter.Enabled = true;
                    }
                }
            }

            try {
                // Debug filter files are always in the data folder.
                var path = Path.Combine(KnUtils.GetFSODataFolderPath(), "data", "debug_filter.cfg");
                if (File.Exists(path)) 
                {
                    using (StreamReader reader = new StreamReader(path)) 
                    {
                        string? line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            bool enable = true;
                            string option = string.Empty;

                            // Treat it just like FSO does.  Use the first character to tell if it's enabled or disabled.
                            if (line.StartsWith("+"))
                            {
                                enable = true;
                                option = line.Substring(1).Trim();
                            } else if (line.StartsWith("-"))
                            {
                                enable = false;
                                option = line.Substring(1).Trim();
                            }

                            if (option != string.Empty)
                            {
                                bool found = false;
                                foreach (var category in Categories)
                                {
                                    foreach (var filter in category.Filters)
                                    {
                                        if (filter.Filter.Trim().ToLower() == option.ToLower())
                                        {
                                            filter.Enabled = enable;
                                            found = true;
                                            break;
                                        }
                                        
                                    }
                                    if (found) 
                                    {
                                        break;
                                    }
                                }

                                // if we didn't have a match in the standard categories
                                if (!found)
                                {
                                    foreach (var category in Categories)
                                    {
                                        if (category.Typename == "Custom")
                                        {
                                            found = true;
                                            category.AddCustom(option, enable);
                                            break;
                                        }
                                    }
                                }

                                // if we also didn't have a custom category setup.
                                if (!found)
                                {
                                    var customCategory = new DebugFilterCategory();
                                    customCategory.Typename = "Custom";
                                    customCategory.AddCustom(option, enable); 
                                    Categories.Insert(0, customCategory);
                                }
                            }
                        }
                    }
                }                
            }
            catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "DebugFiltersViewModel.LoadDebugRepo()", ex);
            }

            Instance = this;
            Initialized = true;
        }

        public DebugFiltersViewModel()
        {
            Task.Run(() => LoadDebugRepo());
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
                if (category.Typename == "Custom")
                {
                    category.AddCustom(CustomFilter, true);

                    // Empty the text box when done.
                    CustomFilter = string.Empty;
                    return;
                }
            }

            // if no custom category exists, add it and then add the new filter
            var customCategory = new DebugFilterCategory();
            customCategory.Typename = "Custom";
            customCategory.AddCustom(CustomFilter, true);
            Categories.Insert(0, customCategory);

            // Empty the text box when done.
            CustomFilter = string.Empty;

            SaveConfig();
        }

        public void SaveConfig(){
            try 
            {
                if(!Directory.Exists(Path.Combine(KnUtils.GetFSODataFolderPath(), "data")))
                {
                    Directory.CreateDirectory(Path.Combine(KnUtils.GetFSODataFolderPath(), "data"));
                }

                // much easier to start over with settings the user has chosen
                var path = Path.Combine(KnUtils.GetFSODataFolderPath(), "data", "debug_filter.cfg");
                if (File.Exists(path)) {
                    File.Delete(path);
                }

                using (StreamWriter sw = new StreamWriter(path, false, new UTF8Encoding(false)))
                {
                    foreach(var category in Categories)
                    {
                        // Every custom filter must be added or it will be lost.
                        if (category.Typename == "Custom") 
                        {
                            foreach(var filter in category.Filters)
                            {
                                if (filter.Enabled) {
                                    sw.WriteLine("+" + filter.Filter);
                                } else {
                                    sw.WriteLine("-" + filter.Filter);
                                }
                            }
                        } 
                        // General are on by default, so only list if they are disabled
                        else if (category.Typename == "General")
                        {
                            foreach(var filter in category.Filters)
                            {
                                if (!filter.Enabled) 
                                {
                                    sw.WriteLine("-" + filter.Filter);
                                }
                            }
                        }
                        else
                        {
                            foreach(var filter in category.Filters)
                            {
                                if (filter.Enabled)
                                {
                                    sw.WriteLine("+" + filter.Filter);
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "DebugFiltersViewModel.SaveConfig() is unable to save: ", ex);
            }
        
            
        }
    }
}