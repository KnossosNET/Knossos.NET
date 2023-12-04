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
    /// Develop Tab View Model
    /// </summary>
    public partial class CommunityViewModel : ViewModelBase
    {
        [ObservableProperty]
        internal ObservableCollection<QuestionCategory> categories = new ObservableCollection<QuestionCategory>();


        public CommunityViewModel()
        {
            LoadFAQRepo();
        }

        private async Task LoadFAQRepo()
        {
            try
            {
                HttpResponseMessage response = await KnUtils.GetHttpClient().GetAsync(Knossos.FAQURL).ConfigureAwait(false);
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var faqRepo = JsonSerializer.Deserialize<QuestionCategory[]>(json)!;
                if(faqRepo != null)
                {
                    foreach(var category in faqRepo)
                    {
                        Categories.Insert(Categories.Count, category);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "CommunityViewModel.LoadFAQRepo()", ex);
            }

        }

        internal void JoinHLPDiscord()
        {
            KnUtils.OpenBrowserURL(@"https://discord.gg/cyhMBhMHzK");
        }

        internal void VisitHLPWiki()
        {
            KnUtils.OpenBrowserURL(@"https://wiki.hard-light.net/index.php/Main_Page");
        }

        internal void OpenFredTutorial()
        {
            KnUtils.OpenBrowserURL(@"http://diaspora.fs2downloads.com/FREDDocs/index.html");
        }

        internal void VisitScriptingRepo()
        {
            KnUtils.OpenBrowserURL(@"https://github.com/FSO-Scripters/fso-scripts");
        }

        internal void VisitKnossosNETIssues(){
            KnUtils.OpenBrowserURL(@"https://github.com/KnossosNET/Knossos.NET/issues");
        }

        internal void VisitFSOIssues(){
            KnUtils.OpenBrowserURL(@"https://github.com/scp-fs2open/fs2open.github.com/issues");
        }
    }
}