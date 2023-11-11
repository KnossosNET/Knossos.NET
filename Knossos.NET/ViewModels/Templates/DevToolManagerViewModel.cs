using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Classes;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media;
using System.Net.Http;
using Avalonia.Threading;

namespace Knossos.NET.ViewModels
{
    public partial class DevToolManagerViewModel : ViewModelBase
    {
        /**/
        public partial class ToolItem : ObservableObject
        {
            [ObservableProperty]
            internal IBrush favoriteColor = Brushes.White;
            [ObservableProperty]
            internal bool disableButtons = false;
            [ObservableProperty]
            internal bool isInstalled = false;
            [ObservableProperty]
            internal bool updateAvailable = false;
            [ObservableProperty]
            internal string updateTooltip = "";
            [ObservableProperty]
            internal string toolTip = "";
            [ObservableProperty]
            public Tool tool;
            public Tool? repo;

            public ToolItem(Tool tool) 
            { 
                this.tool = tool;
                ToolTip = tool.description + "\n\n\nLast Updated: " + tool.lastUpdate;
                IsInstalled = tool.isInstalled;
                if (Tool.isFavorite)
                    FavoriteColor = Brushes.Yellow;
            }

            internal void ToggleFavorite()
            {
                Tool.isFavorite = !Tool.isFavorite;
                if (Tool.isFavorite)
                    FavoriteColor = Brushes.Yellow;
                else
                    FavoriteColor = Brushes.White;
                if (Tool.isInstalled)
                {
                    Tool.SaveJson();
                }
            }

            public void AddRepoVersion(Tool repo)
            {
                if(SemanticVersion.Compare(repo.version,Tool.version) > 0)
                {
                    this.repo = repo;
                    UpdateAvailable = true;
                    UpdateTooltip = "New Version Available: " + repo.version + "\nUpdate Date: " + repo.lastUpdate;
                }
            }

            internal void Update()
            {
                DisableButtons = true;
                if(repo != null)
                    TaskViewModel.Instance!.DownloadTool(repo, Tool, Callback);
            }

            internal void Delete()
            {
                try
                {
                    Tool.Delete();
                    if (UpdateAvailable)
                    {
                        UpdateAvailable = false;
                        UpdateTooltip = "";
                        Tool = repo!;
                        repo = null;
                        IsInstalled = Tool.isInstalled;
                    }
                    IsInstalled = false;
                }
                catch { }
            }

            internal void Install()
            {
                DisableButtons = true;
                TaskViewModel.Instance!.DownloadTool(Tool,null,Callback);
            }

            internal void Open()
            {
                Tool.Open();
            }

            internal void Callback(bool _)
            {
                DisableButtons = false;
                if (UpdateAvailable)
                {
                    UpdateAvailable = false;
                    UpdateTooltip = "";
                    Tool = repo!;
                    repo = null;
                }
                IsInstalled = Tool.isInstalled;
            }
        }
        /**/

        [ObservableProperty]
        internal ObservableCollection<ToolItem> tools = new ObservableCollection<ToolItem>();

        private bool toolsRepoLoaded = false;

        public DevToolManagerViewModel()
        {
        }

        public async void LoadTools()
        {
            if (!toolsRepoLoaded)
            {
                foreach (var installedTool in Knossos.GetTools())
                {
                    InsertInOrder(installedTool);
                }
                await LoadToolRepo().ConfigureAwait(false);
                toolsRepoLoaded = true;
            }
        }

        private async Task LoadToolRepo()
        {
            try
            {
                HttpResponseMessage response = await KnUtils.GetHttpClient().GetAsync(Knossos.ToolRepoURL).ConfigureAwait(false);
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var toolsRepo = JsonSerializer.Deserialize<Tool[]>(json)!;
                if(toolsRepo != null && toolsRepo.Any())
                {
                    foreach(var tool in toolsRepo)
                    {
                        if(tool.IsValidPlatform())
                        {
                            Dispatcher.UIThread.Invoke(() => { 
                                var installed = Tools.FirstOrDefault(x=>x.Tool.name == tool.name);
                                if (installed != null)
                                {
                                    installed.AddRepoVersion(tool);
                                }
                                else
                                {
                                    InsertInOrder(tool);
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "DevToolManagerViewModel.LoadToolRepo()", ex);
            }
        }

        private void InsertInOrder(Tool tool)
        {
            int i;
            for (i = 0; i < Tools.Count; i++)
            {
                if(Tools[i].Tool.isFavorite == tool.isFavorite && String.Compare(Tools[i].Tool.name, tool.name) > 0)
                {
                    break;
                }
                if (!Tools[i].Tool.isFavorite && tool.isFavorite)
                {
                    break;
                }
            }
            Tools.Insert(i, new ToolItem(tool));
        }
    }
}
