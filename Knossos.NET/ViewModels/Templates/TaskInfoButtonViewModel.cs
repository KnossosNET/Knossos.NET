using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
    public partial class TaskInfoButtonViewModel : ViewModelBase
    {
        internal TaskViewModel? TaskViewModel { get; set; }
        [ObservableProperty]
        internal int taskNumber = 0;
        [ObservableProperty]
        internal int animate = 0;
        [ObservableProperty]
        internal string tooltip = "";
        [ObservableProperty]
        internal bool frame0 = true;
        [ObservableProperty]
        internal bool frame1 = false;

        public TaskInfoButtonViewModel() 
        {
            TaskNumber = 0;
        }

        public TaskInfoButtonViewModel(TaskViewModel taskViewModel)
        {
            TaskViewModel = taskViewModel;
            TaskNumber = taskViewModel.NumberOfTasks();
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 1500;
            timer.Elapsed += Update;
            timer.Start();
        }

        private void Update(object? _, System.Timers.ElapsedEventArgs __)
        {
            if(TaskViewModel != null)
            {
                int tasks = TaskViewModel.NumberOfTasks();
                TaskNumber = tasks;
                
                if (tasks > 0)
                {
                    Tooltip = "Open Task List\n\n" + TaskViewModel.GetRunningTaskString();
                    if (!TaskViewModel.IsSafeState())
                    {
                        Animate = 1;
                    }
                    else
                    {
                        Animate = 0;
                    }
                }
                else
                {
                    Tooltip = "Open Task List";
                    Animate = 0;
                }
            }
        }
    }
}
