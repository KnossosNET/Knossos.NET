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
        internal bool animating = false;
        [ObservableProperty]
        internal string tooltip = "";

        public TaskInfoButtonViewModel() 
        {
            TaskNumber = 0;
        }

        public TaskInfoButtonViewModel(TaskViewModel taskViewModel)
        {
            TaskViewModel = taskViewModel;
            TaskNumber = taskViewModel.NumberOfTasks();
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 2000;
            timer.Elapsed += Update;
            timer.Start();
        }

        internal void OpenTaskView()
        {
            TaskViewModel?.ShowCommand();
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
                        Animating = true;
                    }
                    else
                    {
                        Animating = false;
                    }
                }
                else
                {
                    Animating = false;
                    Tooltip = "Open Task List";
                }
            }
        }
    }
}
