using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
    public partial class DevModEditorViewModel : ViewModelBase
    {
        public DevModEditorViewModel() 
        { 

        }

        [ObservableProperty]
        public string name = string.Empty;

        public void LoadMod(Mod mod)
        {
            Name = mod.ToString();
        }
    }
}
