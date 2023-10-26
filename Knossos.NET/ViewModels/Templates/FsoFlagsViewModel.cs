using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Classes;
using Knossos.NET.Views;
using System.Collections.ObjectModel;
using System.Linq;

namespace Knossos.NET.ViewModels
{
    public partial class FsoFlagsViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string globalCmd = string.Empty;

        [ObservableProperty]
        public string cmdLine = string.Empty;

        private ObservableCollection<FlagCategoryItem> Flags { get; set; } = new ObservableCollection<FlagCategoryItem>();

        public FsoFlagsViewModel()
        {
        }

        public FsoFlagsViewModel(FlagsJsonV1 flagsV1, string? modCmd)
        {
            if(modCmd != null)
            {
                CmdLine = modCmd;
            }

            globalCmd += Knossos.globalSettings.GetSystemCMD();
            
            if(Knossos.globalSettings.globalCmdLine != null)
            {
                globalCmd = Knossos.globalSettings.globalCmdLine;
            }

            if (flagsV1.flags != null)
            {
                foreach(Flag flag in flagsV1.flags)
                {
                    var catExist = Flags.FirstOrDefault(f => f.Name == flag.type);

                    if(catExist != null)
                    {
                        catExist.FlagItemList.Add(new FlagItem(flag.name,flag.description,flag.web_url, CmdLine.Contains(flag.name) || GlobalCmd.Contains(flag.name), this));
                    }
                    else
                    {
                        var newCat = new FlagCategoryItem(flag.type);
                        newCat.FlagItemList.Add(new FlagItem(flag.name, flag.description, flag.web_url, CmdLine.Contains(flag.name) || GlobalCmd.Contains(flag.name), this));
                        Flags.Add(newCat);
                    }
                }
            }
        }

        public string GetCmdLine()
        {
            return CmdLine;
        }
    }

    public class FlagCategoryItem
    {
        public string Name { get; set; } = string.Empty;
        public ObservableCollection<FlagItem> FlagItemList { get; set; } = new ObservableCollection<FlagItem>();

        public FlagCategoryItem(string name) 
        { 
            this.Name = name;
        }
    }

    public class FlagItem
    {
        public string Cmd { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Web_url { get; set; } = string.Empty;
        private bool Enabled { get; set; } = false;

        private FsoFlagsViewModel view;

        public FlagItem(string flag, string description, string web_url, bool enabled, FsoFlagsViewModel view)
        {
            this.Cmd = flag;
            this.Description = description;
            this.Web_url = web_url;
            this.Enabled = enabled;
            this.view = view;
        }

        internal void ToggleFlag()
        {
            if(!Enabled)
            {
                if(view.CmdLine.Contains(" " + this.Cmd))
                {
                    view.CmdLine = view.CmdLine.Replace(" " + this.Cmd, "");
                }
                else
                {
                    view.CmdLine = view.CmdLine.Replace(this.Cmd, "");
                }
            }
            else
            {
                if(view.CmdLine.Length > 0)
                {
                    view.CmdLine += " " + Cmd;
                }
                else
                {
                    view.CmdLine += Cmd;
                }
            }
        }

        public void OpenFlagInfoURL()
        {
            KnUtils.OpenBrowserURL(this.Web_url);
        }
    }
}
