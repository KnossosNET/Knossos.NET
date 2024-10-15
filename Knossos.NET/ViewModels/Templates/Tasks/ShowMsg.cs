using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using Avalonia.Media;

namespace Knossos.NET.ViewModels
{
    public partial class TaskItemViewModel : ViewModelBase
    {
        public void ShowMsg(string msg, string? tooltip, IBrush? textColor = null)
        {
            try
            {
                if (!TaskIsSet)
                {
                    TaskIsSet = true;
                    IsCompleted = true;
                    IsTextTask = true;
                    Name = msg;
                    if (tooltip != null)
                    {
                        Tooltip = tooltip.Trim();
                        TooltipVisible = true;
                    }
                    if (textColor != null)
                    {
                        TextColor = textColor;
                    }
                }
                else
                {
                    throw new Exception("The task is already set, it cant be changed or re-assigned.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "TaskItemViewModel.ShowMsg()", ex);
            }
        }
    }
}
