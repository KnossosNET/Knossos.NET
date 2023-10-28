using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;
using Knossos.NET.Views;

namespace Knossos.NET.ViewModels
{
    public partial class ReportModViewModel : ViewModelBase
    {
        [ObservableProperty]
        internal string modName = string.Empty;

        [ObservableProperty]
        internal string modVersion = string.Empty;

        [ObservableProperty]
        internal string reasonString = string.Empty;

        private Mod? mod;

        public ReportModViewModel()
        {
        }

        public ReportModViewModel(Mod mod)
        {
            this.mod = mod;
            modName = mod.title;
            modVersion = mod.version;
        }

        internal async void Submit(object window)
        {
            if(ReasonString.Trim() != string.Empty && mod != null)
            {
                var reply = await Nebula.ReportMod(mod, ReasonString);
                if(reply)
                {
                    await MessageBox.Show(MainWindow.instance!, "Your report has been submitted to fsnebula, we will act as soon as possible.", "Mod Report OK", MessageBox.MessageBoxButtons.OK);
                    var w = (Window)window;
                    w.Close();
                }
                else
                {
                    await MessageBox.Show(MainWindow.instance!, "An error has ocurred while reporting a mod.", "Mod Report Error", MessageBox.MessageBoxButtons.OK);
                }
            }
            else
            {
                await MessageBox.Show(MainWindow.instance!, "Please provide a reason for your report.", "Reason is empty", MessageBox.MessageBoxButtons.OK);
            }
        }

        internal void Cancel(object window)
        {
            var w = (Window)window;
            w.Close();
        }
    }
}
