using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
    /// <summary>
    /// Simple Quick Setup Guide
    /// Basic view model for the Quick Setup view
    /// </summary>
    public partial class QuickSetupViewModel : ViewModelBase
    {
        private int pageNumber = 1;

        [ObservableProperty]
        internal bool repoDownloaded = false;
        [ObservableProperty]
        internal bool canGoBack = false;
        [ObservableProperty]
        internal bool canContinue = true;
        [ObservableProperty]
        internal bool lastPage = false;

        [ObservableProperty]
        internal string? libraryPath = null;

        [ObservableProperty]
        internal bool page1 = true;
        [ObservableProperty]
        internal bool page2 = false;
        [ObservableProperty]
        internal bool page3 = false;
        [ObservableProperty]
        internal bool page4 = false;
        [ObservableProperty]
        internal bool page5 = false;
        [ObservableProperty]
        internal bool page6 = false;

        private Window? dialog;
        public QuickSetupViewModel(Window dialog) 
        {
            this.dialog = dialog;
            TrackRepoStatus();
        }

        /// <summary>
        /// Wait until repo_minimal.json has been parsed
        /// </summary>
        private void TrackRepoStatus()
        {
            Task.Run(async () =>
            {
                do
                {
                    await Task.Delay(1000);
                } while (!Nebula.repoLoaded);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    RepoDownloaded = Nebula.repoLoaded;
                });
            });
        }

        /// <summary>
        /// Wait until the Knossos library path is set
        /// </summary>
        private void EnterPage2()
        {
            Task.Run(() => 
            {
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    LibraryPath = Knossos.globalSettings.basePath;

                    if (Page2)
                    {
                        if (LibraryPath == null)
                        {
                            await Task.Delay(1000);
                            EnterPage2();
                        }
                        else
                        {
                            CanContinue = true;
                        }
                    }
                });
            });
        }

        /// <summary>
        /// Wait until FSO flags has been loaded
        /// </summary>
        private void EnterPage3()
        {
            Task.Run(() =>
            {
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (Page3)
                    {
                        if (!Knossos.flagDataLoaded)
                        {
                            await Task.Delay(1000);
                            EnterPage3();
                        }
                        else
                        {
                            CanContinue = true;
                        }
                    }
                });
            });
        }

        internal void GoBackCommand()
        {
            pageNumber--;
            SetActivePage();
        }

        internal void ContinueCommand()
        {
            pageNumber++;
            SetActivePage();
        }

        internal void Finish()
        {
            if(dialog != null)
                dialog.Close();
        }

        private void SetActivePage()
        {
            switch(pageNumber)
            {
                case 1: CanGoBack = false; CanContinue = true; Page1 = true; Page2 = false; LastPage = false;  break;
                case 2: CanGoBack = true; CanContinue = false; Page1 = false; Page2 = true; Page3 = false; EnterPage2(); LastPage = false;  break;
                case 3: CanGoBack = true; CanContinue = false; Page2 = false; Page3 = true; Page4 = false; EnterPage3(); LastPage = false;  break;
                case 4: CanGoBack = true; CanContinue = true; Page3 = false; Page4 = true; Page5 = false; LastPage = false; break;
                case 5: CanGoBack = true; CanContinue = true; Page4 = false; Page5 = true; Page6 = false; LastPage = false; break;
                case 6: CanGoBack = true; CanContinue = false; Page5 = false; Page6 = true; LastPage = true; break;
            }
        }
    }
}
