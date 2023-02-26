using Avalonia.Controls;
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
        }
    }
}
