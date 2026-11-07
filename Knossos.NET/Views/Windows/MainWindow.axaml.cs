using Avalonia;
using Avalonia.Controls;
using Knossos.NET.Models;
using Knossos.NET.ViewModels;
using System;

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

        /// <summary>
        /// Apply the starting size, fitting normal mode to the screen while preserving custom dimensions.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetSize(double? width, double? height)
        {
            if (!CustomLauncher.IsCustomMode)
            {
                var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
                if (screen != null)
                {
                    // WorkingArea is in physical pixels; Width and Height are in logical units.
                    var availableSize = screen.WorkingArea.Size.ToSize(screen.Scaling);
                    if (width.HasValue)
                        width = Math.Min(width.Value, availableSize.Width * 0.9);
                    if (height.HasValue)
                        height = Math.Min(height.Value, availableSize.Height * 0.9);
                }
            }

            if(width.HasValue)
                this.Width = width.Value;
            if(height.HasValue)
                this.Height = height.Value;
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            // Everything that has to happen before the window goes away is synchronous and
            // already runs on the UI thread, so do it here and let the close proceed.
            // Do NOT cancel the close and re-call Close() later: on Linux the X11 session
            // manager asks the app to close during logout/reboot, and a cancelled first
            // close is reported back as "the app refused", which cancels the whole logout.
            Knossos.Tts(string.Empty);
            MainWindowViewModel.Instance?.GlobalSettingsView?.CommitPendingChanges();
            Knossos.globalSettings.SaveSettingsOnAppClose();
            base.OnClosing(e);
        }
    }
}
