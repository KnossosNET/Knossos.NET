using Avalonia.Controls;
using Avalonia.Threading;
using Knossos.NET.ViewModels;

namespace Knossos.NET.Views
{
    public partial class MainWindow : Window
    {
        public static MainWindow? instance;
        private static bool canClose = false;

        public MainWindow()
        {
            instance = this;
            InitializeComponent();
        }

        protected override async void OnClosing(WindowClosingEventArgs e)
        {
            //Intercept closing, do stuff, then re-call close
            if (!canClose)
            {
                e.Cancel = true;

                await Dispatcher.UIThread.InvokeAsync(() => {
                    Knossos.Tts(string.Empty);
                    MainWindowViewModel.Instance?.GlobalSettingsView.CommitPendingChanges();
                    Knossos.globalSettings.SaveSettingsOnAppClose();
                    canClose = true;
                });

                if (canClose) 
                    this.Close(); 
            }
            base.OnClosing(e);
        }
    }
}
