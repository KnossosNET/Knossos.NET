using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
    public partial class DevModDescriptionEditorViewModel : ViewModelBase
    {
        private DevModEditorViewModel? editor;

        public DevModDescriptionEditorViewModel()
        {
        }

        public DevModDescriptionEditorViewModel(DevModEditorViewModel? editor)
        {
            this.editor = editor;
        }
    }
}
