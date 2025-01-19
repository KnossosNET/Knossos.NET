using CommunityToolkit.Mvvm.ComponentModel;

namespace Knossos.NET.ViewModels
{
    public partial class HtmlContentViewModel : ViewModelBase
    {
        [ObservableProperty]
        internal string? htmlData = null;

        private string? savedHtmlData = null;

        public HtmlContentViewModel() 
        { 
        }

        public HtmlContentViewModel(string htmlData)
        {
            savedHtmlData = htmlData;
        }

        /// <summary>
        /// Loads the HTML content to the view
        /// </summary>
        public void Navigate()
        {
            if(savedHtmlData != null)
                HtmlData = savedHtmlData;
        }
    }
}
