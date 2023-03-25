using Avalonia.Threading;
using Knossos.NET.Models;
using Knossos.NET.Views;
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
        public Queue<TaskItemViewModel> taskQueue { get; set; } = new Queue<TaskItemViewModel>();

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

        public void CancelAllInstallTaskWithID(string id, string? version)
        {
            foreach (var task in TaskList)
            {
                if(!task.IsCompleted && task.installID == id && task.installVersion == version)
                {
                    task.CancelTaskCommand();
                }
            }
        }

        public bool IsSafeState()
        {
            foreach (var task in TaskList)
            {
                if (!task.IsCancelled && !task.IsCompleted)
                {
                    return false;
                }
            }
            return true;
        }

        public async Task<bool?> AddFileDownloadTask(string url, string dest, string msg, bool showStopButton, string? tooltip = null)
        {
            var newTask = new TaskItemViewModel();
            TaskList.Add(newTask);
            return await newTask.DownloadFile(url, dest, msg, showStopButton, tooltip);
        }

        public void AddMessageTask(string msg, string? tooltip = null)
        {
            try
            {
                var newTask = new TaskItemViewModel();
                TaskList.Add(newTask);
                newTask.ShowMsg(msg, tooltip);
            }
            catch { }
        }

        public void CleanCommand()
        {
            for (int i = TaskList.Count - 1; i >= 0; i--)
            {
                if (TaskList[i].CancelButtonVisible == false)
                {
                    TaskList.RemoveAt(i);
                }
            }
        }

        public async Task<FsoBuild?> InstallBuild(FsoBuild build, FsoBuildItemViewModel sender, Mod? modJson=null)
        {
            if(Knossos.GetKnossosLibraryPath() == null)
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await MessageBox.Show(MainWindow.instance!, "Knossos library path is not set! Before installing mods go to settings and select a library folder.", "Error", MessageBox.MessageBoxButtons.OK);
                });
                return null;
            }
            var newTask = new TaskItemViewModel();
            TaskList.Add(newTask);
            taskQueue.Enqueue(newTask);
            return await newTask.InstallBuild(build, sender,sender.cancellationTokenSource,modJson);
        }

        public async void InstallMod(Mod mod, List<ModPackage>? reinstallPkgs = null)
        {
            if (Knossos.GetKnossosLibraryPath() == null)
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await MessageBox.Show(MainWindow.instance!, "Knossos library path is not set! Before installing mods go to settings and select a library folder.", "Error", MessageBox.MessageBoxButtons.OK);
                });
                return;
            }

            if (mod.type == "engine")
            {
                //If this is a engine build call the UI element to do the build install process instead
                FsoBuildsViewModel.Instance?.RelayInstallBuild(mod);
            }
            else
            {
                var cancelSource = new CancellationTokenSource();
                var newTask = new TaskItemViewModel();
                TaskList.Add(newTask);
                taskQueue.Enqueue(newTask);
                await newTask.InstallMod(mod, cancelSource, reinstallPkgs);
                cancelSource.Dispose();
            }
        }

        public async void VerifyMod(Mod mod)
        {
            var cancelSource = new CancellationTokenSource();
            var newTask = new TaskItemViewModel();
            TaskList.Add(newTask);
            taskQueue.Enqueue(newTask);
            await newTask.VerifyMod(mod, cancelSource);
            cancelSource.Dispose();
        }
    }
}
