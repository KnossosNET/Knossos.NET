using Avalonia.Controls;
using System.ComponentModel;
using System.Threading.Tasks;

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

                await Task.Run(() => {
                    Knossos.Tts(string.Empty);
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
