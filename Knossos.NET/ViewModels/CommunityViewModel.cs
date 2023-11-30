using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.Views;

namespace Knossos.NET.ViewModels
{
    /// <summary>
    /// Develop Tab View Model
    /// </summary>
    public partial class CommunityViewModel : ViewModelBase
    {
        public static string discordText = string.Empty;
        public static string hlpText = string.Empty;
        public static string fredText = string.Empty;

        public static string githubText = string.Empty;

        public static string faqText = string.Empty;
    

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