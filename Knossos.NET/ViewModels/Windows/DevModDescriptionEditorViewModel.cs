using Avalonia.Controls;
using BBcodes;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
    public partial class DevModDescriptionEditorViewModel : ViewModelBase
    {
        private DevModDetailsViewModel? modeDetails;
        public TextBox? inputControl;
        private string description = string.Empty;
        private string Description
        {
            get
            {
                return description;
            }
            set 
            {
                if(value != description)
                {
                    SetProperty(ref description, value);
                    try
                    {
                        var html = BBCode.ConvertToHtml(Description, BBCode.BasicRules);
                        DescriptionHtml = "<body style=\"overflow: hidden;\"><span style=\"white-space: pre-line;color:white;overflow: hidden;\">" + html + "</span></body>";
                    }
                    catch (Exception ex) 
                    {
                        Log.Add(Log.LogSeverity.Error, "DevModDescriptionEditorViewModel.Description", ex);
                    }
                }
            }
        }
        [ObservableProperty]
        private string descriptionHtml = string.Empty;

        public DevModDescriptionEditorViewModel()
        {
        }

        internal void ToolBar (object command)
        {
            var caret = inputControl != null ? inputControl.CaretIndex : 0;
            Description = Description.Insert(caret, (string)command);
        }

        internal void Closing()
        {
            if(modeDetails != null)
            {
                modeDetails.UpdateDescription(Description);
            }
        }

        public DevModDescriptionEditorViewModel(DevModDetailsViewModel devModDetailsViewModel, string description)
        {
            this.modeDetails = devModDetailsViewModel;
            Description = description;
        }
    }
}
