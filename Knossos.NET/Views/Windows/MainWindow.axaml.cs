using Avalonia.Controls;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Knossos.NET.Views
{
    public partial class MainWindow : Window
    {
        public static MainWindow? instance;
        public MainWindow()
        {
            instance = this;
            InitializeComponent();
            this.Closing += MainWindow_StopTTS;
        }

        private void MainWindow_StopTTS(object? sender, CancelEventArgs e)
        {
            Knossos.Tts(string.Empty);
        }
    }
}
