using Avalonia.Threading;
using Knossos.NET.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace Knossos.NET.ViewModels
{
    public partial class TaskItemViewModel : ViewModelBase
    {
        public void DisplayUpdates(List<Mod> updatedMods)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    IsCompleted = true;
                    IsTextTask = true;
                    var newMods = updatedMods.Where(x => x.isNewMod && x.type == ModType.mod);
                    var newTCs = updatedMods.Where(x => x.isNewMod && x.type == ModType.tc);
                    var newEngine = updatedMods.Where(x => x.type == ModType.engine);
                    var updateMods = updatedMods.Where(x => !x.isNewMod && x.type != ModType.engine);

                    Name = "Repo Changes:";
                    if (newMods != null && newMods.Any())
                    {
                        Name += " New Mods: " + newMods.Count();
                        foreach (var nm in newMods)
                        {
                            var newTask = new TaskItemViewModel();
                            newTask.ShowMsg("Mod Released!   " + nm, null, Brushes.Green);
                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                TaskList.Add(newTask);
                            });
                        }
                    }
                    if (newTCs != null && newTCs.Any())
                    {
                        Name += " TCs: " + newTCs.Count();
                        foreach (var nTc in newTCs)
                        {
                            var newTask = new TaskItemViewModel();
                            newTask.ShowMsg("Total Conversion Released!  " + nTc, null, Brushes.Green);
                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                TaskList.Add(newTask);
                            });
                        }
                    }
                    if (newEngine != null && newEngine.Any())
                    {
                        Name += " Engine Builds: " + newEngine.Count();
                        foreach (var ne in newEngine)
                        {

                            var newTask = new TaskItemViewModel();
                            newTask.ShowMsg("Engine Build Released!  " + ne, null, Brushes.Yellow);
                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                TaskList.Add(newTask);
                            });
                        }
                    }
                    if (updateMods != null && updateMods.Any())
                    {
                        Name += " Mod Updates: " + updateMods.Count();
                        foreach (var nm in updateMods)
                        {
                            var newTask = new TaskItemViewModel();
                            newTask.ShowMsg("Mod Update Released!  " + nm, null, Brushes.LightBlue);
                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                TaskList.Add(newTask);
                            });
                        }
                    }
                    Dispatcher.UIThread.InvokeAsync(() => TaskRoot.Add(this));
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.DisplayUpdates()", ex);
            }
        }
    }
}
