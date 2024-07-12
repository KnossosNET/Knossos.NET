using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
    public partial class LoadingIconViewModel : ViewModelBase
    {
        [ObservableProperty]
        internal bool frame0 = true;
        [ObservableProperty]
        internal bool frame1 = false;

        public LoadingIconViewModel() 
        {
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 500;
            timer.Elapsed += Animate;
            timer.Start();
        }

        private void Animate(object? _, System.Timers.ElapsedEventArgs __)
        {
            if(Frame0)
            {
                Frame0 = false;
                Frame1 = true;
            }
            else
            {
                Frame1 = false;
                Frame0 = true;
            }
        }
    }
}
