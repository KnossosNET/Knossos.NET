using Avalonia.Controls;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.Views;
using Knossos.NET.Views.Windows;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
    public partial class FsoBuildsViewModel : ViewModelBase
    {
        public static FsoBuildsViewModel? Instance; 

        private ObservableCollection<FsoBuildItemViewModel> StableItems { get; set; } = new ObservableCollection<FsoBuildItemViewModel>();
        private ObservableCollection<FsoBuildItemViewModel> RcItems { get; set; } = new ObservableCollection<FsoBuildItemViewModel>();
        private ObservableCollection<FsoBuildItemViewModel> NightlyItems { get; set; } = new ObservableCollection<FsoBuildItemViewModel>();
        private ObservableCollection<FsoBuildItemViewModel> CustomItems { get; set; } = new ObservableCollection<FsoBuildItemViewModel>();

        public FsoBuildsViewModel()
        {
            Instance = this;
        }

        public void ClearView()
        {
            StableItems.Clear();
            RcItems.Clear();
            NightlyItems.Clear();
            CustomItems.Clear();
        }

        public void RelayInstallBuild(Mod mod)
        {
            var buildStability = FsoBuild.GetFsoStability(mod.stability, mod.id);
            ObservableCollection<FsoBuildItemViewModel>? list = null;
            switch (buildStability)
            {
                case FsoStability.Stable: list = StableItems; break;
                case FsoStability.RC: list = RcItems; break;
                case FsoStability.Nightly: list = NightlyItems; break;
                case FsoStability.Custom: list = CustomItems; break;
            }

            if(list != null)
            {
                var uiItem = list.FirstOrDefault(b => b.CompareIdAndVersionToMod(mod) == true);
                if (uiItem != null)
                {
                    uiItem.DownloadBuildExternal(mod);
                }
                else
                {
                    Log.Add(Log.LogSeverity.Error, "FsoBuildsViewModel.RelayInstallBuild()", "Unable to find the build UI item with these parameters. Build install cancelled. Version: " + mod.version + " ID: " + mod.id);
                }
            }
            else
            {
                Log.Add(Log.LogSeverity.Error, "FsoBuildsViewModel.RelayInstallBuild()", "Unable to select the correct build list with these parameters. Build install cancelled. Stability: "+mod.stability + " ID: "+mod.id);
            }
        }

        public void AddBuildToUi(FsoBuild build)
        {
            switch(build.stability)
            {
                case FsoStability.Stable:
                    var previous = StableItems.FirstOrDefault(b=> string.Compare(b.Date,build.date) < 0);
                    if (previous != null)
                    {
                        int index = StableItems.IndexOf(previous);
                        if (index == -1)
                            index = 0;
                        StableItems.Insert(index, new FsoBuildItemViewModel(build, this));
                    }
                    else
                    {
                        StableItems.Add(new FsoBuildItemViewModel(build, this));
                    }
                    break;
                case FsoStability.RC:
                    var previousRc = RcItems.FirstOrDefault(b => string.Compare(b.Date, build.date) < 0);
                    if (previousRc != null)
                    {
                        int index = RcItems.IndexOf(previousRc);
                        if (index == -1)
                            index = 0;
                        RcItems.Insert(index, new FsoBuildItemViewModel(build, this));
                    }
                    else
                    {
                        RcItems.Add(new FsoBuildItemViewModel(build, this));
                    }
                    break;
                case FsoStability.Nightly:
                    var previousNL = NightlyItems.FirstOrDefault(b => string.Compare(b.Date, build.date) < 0);
                    if (previousNL != null)
                    {
                        int index = NightlyItems.IndexOf(previousNL);
                        if (index == -1)
                            index = 0;
                        NightlyItems.Insert(index, new FsoBuildItemViewModel(build, this));
                    }
                    else
                    {
                        NightlyItems.Add(new FsoBuildItemViewModel(build, this));
                    }
                    break;
                case FsoStability.Custom:
                    var previousCu = CustomItems.FirstOrDefault(b => string.Compare(b.Date, build.date) < 0);
                    if (previousCu != null)
                    {
                        int index = CustomItems.IndexOf(previousCu);
                        if (index == -1)
                            index = 0;
                        CustomItems.Insert(index, new FsoBuildItemViewModel(build, this));
                    }
                    else
                    {
                        CustomItems.Add(new FsoBuildItemViewModel(build, this));
                    }
                    break;
            }
        }

        public void DeleteBuild(FsoBuild build, FsoBuildItemViewModel item)
        {
            try
            {
                Directory.Delete(build.folderPath,true);
                if(!StableItems.Remove(item))
                {
                    if (!RcItems.Remove(item))
                    {
                        if (!NightlyItems.Remove(item))
                        {
                            CustomItems.Remove(item);
                        }
                    }
                }
                Knossos.RemoveBuild(build);
            }
            catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "FsoBuildsViewModel.DeleteBuild()",ex);
            }
        }

        public async void CommandAddUserBuild()
        {
            if (MainWindow.instance != null)
            {
                var dialog = new AddUserBuildView();
                dialog.DataContext = new AddUserBuildViewModel();

                await dialog.ShowDialog<AddUserBuildView?>(MainWindow.instance);
            }
        }
    }
}
