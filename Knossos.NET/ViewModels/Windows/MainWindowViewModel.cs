using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;
using Knossos.NET.Views;

namespace Knossos.NET.ViewModels
{
    /// <summary>
    /// Wrapper Window view model for the MainView on desktop OS
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        internal string appTitle = "Knossos.NET v" + Knossos.AppVersion;
        [ObservableProperty]
        internal int minWindowWidth = 900;
        [ObservableProperty]
        internal int minWindowHeight = 500;

        public int? WindowWidth { get; private set; } = null;
        public int? WindowHeight { get; private set; } = null;

        [ObservableProperty]
        internal static MainViewModel? mainViewModel;

        public MainWindowViewModel()
        {
            if(mainViewModel == null)
                mainViewModel = new MainViewModel();

            if (CustomLauncher.IsCustomMode)
            {
                //Apply customization for Single TC Mode
                AppTitle = CustomLauncher.WindowTitle + " v" + Knossos.AppVersion;
                WindowHeight = CustomLauncher.WindowHeight;
                WindowWidth = CustomLauncher.WindowWidth;
                MinWindowWidth = CustomLauncher.MinWindowWidth.HasValue ? CustomLauncher.MinWindowWidth.Value : 900;
                MinWindowHeight = CustomLauncher.MinWindowHeight.HasValue ? CustomLauncher.MinWindowHeight.Value : 500;
            }
            MainWindow.instance?.SetSize(WindowWidth, WindowHeight);
        }
    }
}
