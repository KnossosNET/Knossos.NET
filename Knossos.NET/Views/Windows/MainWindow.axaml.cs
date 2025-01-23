using Avalonia;
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

        /// <summary>
        /// For custom mode, for setting the task buttom at the last place in the menu
        /// we need to change the margins so it is displayed properly.
        /// </summary>
        public void FixMarginButtomTasks()
        {
            var tasks = this.FindControl<TaskInfoButtonView>("TaskButtom");
            if (tasks != null)
            {
                tasks.Margin = new Thickness(9, -45, 0, 0);
            }
            var list = this.FindControl<ListBox>("ButtomList");
            if (list != null)
            {
                list.Margin = new Thickness(2, 0, -100, 0);
            }
        }

        protected override async void OnClosing(WindowClosingEventArgs e)
        {
            //Intercept closing, do stuff, then re-call close
            if (!canClose)
            {
                e.Cancel = true;

                await Dispatcher.UIThread.InvokeAsync(() => {
                    Knossos.Tts(string.Empty);
                    MainWindowViewModel.Instance?.GlobalSettingsView?.CommitPendingChanges();
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
