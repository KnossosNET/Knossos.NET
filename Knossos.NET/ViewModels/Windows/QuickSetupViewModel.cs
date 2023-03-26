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
    public partial class QuickSetupViewModel : ViewModelBase
    {
        private int pageNumber = 1;

        [ObservableProperty]
        private bool repoDownloaded = false;
        [ObservableProperty]
        private bool canGoBack = false;
        [ObservableProperty]
        private bool canContinue = true;

        [ObservableProperty]
        private string? libraryPath = null;

        [ObservableProperty]
        private bool page1 = true;
        [ObservableProperty]
        private bool page2 = false;
        [ObservableProperty]
        private bool page3 = false;
        [ObservableProperty]
        private bool page4 = false;
        [ObservableProperty]
        private bool page5 = false;
        [ObservableProperty]
        private bool page6 = false;

        public QuickSetupViewModel() 
        {
            TrackRepoStatus();
        }

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

        private void GoBackCommand()
        {
            pageNumber--;
            SetActivePage();
        }

        private void ContinueCommand()
        {
            pageNumber++;
            SetActivePage();
        }

        private void SetActivePage()
        {
            switch(pageNumber)
            {
                case 1: CanGoBack = false; CanContinue = true; Page1 = true; Page2 = false; break;
                case 2: CanGoBack = true; CanContinue = false; Page1 = false; Page2 = true; Page3 = false; EnterPage2(); break;
                case 3: CanGoBack = true; CanContinue = false; Page2 = false; Page3 = true; Page4 = false; EnterPage3(); break;
                case 4: CanGoBack = true; CanContinue = true; Page3 = false; Page4 = true; Page5 = false; break;
                case 5: CanGoBack = true; CanContinue = true; Page4 = false; Page5 = true; Page6 = false; break;
                case 6: CanGoBack = true; CanContinue = false; Page5 = false; Page6 = true; break;
            }
        }
    }
}
