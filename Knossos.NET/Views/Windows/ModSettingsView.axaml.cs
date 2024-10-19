using Avalonia.Controls;
using Avalonia.Threading;
using Knossos.NET.ViewModels;
using System.Threading.Tasks;

namespace Knossos.NET.Views
{
    public partial class ModSettingsView : Window
    {
        private bool canClose = false;

        public ModSettingsView()
        {
            InitializeComponent();
        }

        protected override async void OnClosing(WindowClosingEventArgs e)
        {
            //Intercept closing, do stuff, then re-call close
            if (DataContext != null)
            {
                var vm = DataContext as ModSettingsViewModel;
                if (vm != null)
                {
                    if (!canClose)
                    {
                        e.Cancel = true;

                        await Dispatcher.UIThread.InvokeAsync(() => {
                            vm.SaveSettingsCommand();
                            canClose = true;
                        });

                        if (canClose)
                            this.Close();
                    }
                }
            }
            base.OnClosing(e);
        }
    }
}
