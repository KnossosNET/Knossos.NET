using Knossos.NET.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
    public partial class TaskViewModel : ViewModelBase
    {
        public static TaskViewModel? Instance { get; private set; }
        private ObservableCollection<TaskItemViewModel> TaskList { get; set; } = new ObservableCollection<TaskItemViewModel>();
        public Queue<TaskItemViewModel> installQueue { get; set; } = new Queue<TaskItemViewModel>();

        public TaskViewModel() 
        {
            Instance = this;
        }

        public void CancelAllRunningTasks()
        {
            foreach (var task in TaskList)
            {
                task.CancelTaskCommand();
            }
        }

        public async Task<bool?> AddFileDownloadTask(string url, string dest, string msg, bool showStopButton, string? tooltip = null)
        {
            var newTask = new TaskItemViewModel();
            TaskList.Add(newTask);
            return await newTask.DownloadFile(url,dest,msg,showStopButton,tooltip);
        }

        public void AddMessageTask(string msg, string? tooltip = null)
        {
            var newTask = new TaskItemViewModel();
            TaskList.Add(newTask);
            newTask.ShowMsg(msg,tooltip);
        }
        public async Task<FsoBuild?> InstallBuild(FsoBuild build, FsoBuildItemViewModel sender)
        {
            var newTask = new TaskItemViewModel();
            TaskList.Add(newTask);
            installQueue.Enqueue(newTask);
            return await newTask.InstallBuild(build, sender,sender.cancellationTokenSource);
        }

        public async void InstallMod(Mod mod)
        {
            var cancelSource = new CancellationTokenSource();
            var newTask = new TaskItemViewModel();
            TaskList.Add(newTask);
            installQueue.Enqueue(newTask);
            await newTask.InstallMod(mod, cancelSource);
            cancelSource.Dispose();
        }
    }
}
