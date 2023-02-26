using Avalonia.Controls;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.Views;
using Knossos.NET.Views.Windows;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
    public partial class FsoBuildsViewModel : ViewModelBase
    {
        private ObservableCollection<FsoBuildItemViewModel> StableItems { get; set; } = new ObservableCollection<FsoBuildItemViewModel>();
        private ObservableCollection<FsoBuildItemViewModel> RcItems { get; set; } = new ObservableCollection<FsoBuildItemViewModel>();
        private ObservableCollection<FsoBuildItemViewModel> NightlyItems { get; set; } = new ObservableCollection<FsoBuildItemViewModel>();
        private ObservableCollection<FsoBuildItemViewModel> CustomItems { get; set; } = new ObservableCollection<FsoBuildItemViewModel>();

        public FsoBuildsViewModel()
        {
           
        }

        public void ClearView()
        {
            StableItems.Clear();
            RcItems.Clear();
            NightlyItems.Clear();
            CustomItems.Clear();
        }

        public void LoadAllBuilds()
        {
            StableItems.Clear();
            RcItems.Clear();
            NightlyItems.Clear();
            var allBuilds=Knossos.GetInstalledBuildsList("FSO");
            allBuilds.Sort(FsoBuild.CompareDatesAsTimestamp);

            foreach (var build in allBuilds)
            {
                if (build.stability == FsoStability.Stable)
                    StableItems.Add(new FsoBuildItemViewModel(build,this));
                if (build.stability == FsoStability.RC)
                    RcItems.Add(new FsoBuildItemViewModel(build, this));
                if (build.stability == FsoStability.Nightly)
                    NightlyItems.Add(new FsoBuildItemViewModel(build, this));
            }
            LoadCustomBuilds();
        }



        private void LoadCustomBuilds()
        {
            CustomItems.Clear();
            var builds = Knossos.GetInstalledBuildsList(null, Models.FsoStability.Custom);
            builds.Sort(FsoBuild.CompareDatesAsTimestamp);
            foreach (var build in builds)
            {
                CustomItems.Add(new FsoBuildItemViewModel(build, this));
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
                LoadCustomBuilds();
            }
        }
    }
}
