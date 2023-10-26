using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;
using Knossos.NET.Views;
using Knossos.NET.Views.Windows;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Knossos.NET.ViewModels
{
    public partial class FsoBuildsViewModel : ViewModelBase
    {
        public static FsoBuildsViewModel? Instance;

        [ObservableProperty]
        private ObservableCollection<FsoBuildItemViewModel> stableItems = new ObservableCollection<FsoBuildItemViewModel>();
        [ObservableProperty]
        private ObservableCollection<FsoBuildItemViewModel> rcItems = new ObservableCollection<FsoBuildItemViewModel>();
        [ObservableProperty]
        private ObservableCollection<FsoBuildItemViewModel> nightlyItems = new ObservableCollection<FsoBuildItemViewModel>();
        [ObservableProperty]
        private ObservableCollection<FsoBuildItemViewModel> customItems = new ObservableCollection<FsoBuildItemViewModel>();

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

        public void UpdateBuildUI(FsoBuild build)
        {
            switch (build.stability)
            {
                case FsoStability.Stable:
                    var previous = StableItems.FirstOrDefault(b => b.build != null && b.build.id == build.id && b.build.version == build.version);
                    if(previous != null)
                        StableItems.Remove(previous);
                    break;
                case FsoStability.RC:
                    var previousRc = RcItems.FirstOrDefault(b => b.build != null && b.build.id == build.id && b.build.version == build.version);
                    if(previousRc != null)
                        RcItems.Remove(previousRc);
                    break;
                case FsoStability.Nightly:
                    var previousNL = NightlyItems.FirstOrDefault(b => b.build != null && b.build.id == build.id && b.build.version == build.version);
                    if(previousNL != null)
                        NightlyItems.Remove(previousNL);
                    break;
                case FsoStability.Custom:
                    var previousCu = CustomItems.FirstOrDefault(b => b.build != null && b.build.id == build.id && b.build.version == build.version);
                    if(previousCu != null)
                        CustomItems.Remove(previousCu);
                    break;
            }
            AddBuildToUi(build);
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

        public void BulkLoadNebulaBuilds(List<Mod> modsJson)
        {
            var newStable = new ObservableCollection<FsoBuildItemViewModel>();
            var newRc = new ObservableCollection<FsoBuildItemViewModel>();
            var newNightly = new ObservableCollection<FsoBuildItemViewModel>();
            var newCustom = new ObservableCollection<FsoBuildItemViewModel>();
            StableItems.ForEach(s => { if (s.IsInstalled) newStable.Add(s); });
            RcItems.ForEach(s => { if (s.IsInstalled) newRc.Add(s); });
            NightlyItems.ForEach(s => { if (s.IsInstalled) newNightly.Add(s); });
            CustomItems.ForEach(s => { if (s.IsInstalled) newCustom.Add(s); });

            foreach (var mod in modsJson)
            {
                var build = new FsoBuild(mod);
                switch (build.stability)
                {
                    case FsoStability.Stable:
                        var previous = newStable.FirstOrDefault(b => string.Compare(b.Date, build.date) < 0);
                        if (previous != null)
                        {
                            int index = newStable.IndexOf(previous);
                            if (index == -1)
                                index = 0;
                            newStable.Insert(index, new FsoBuildItemViewModel(build, this));
                        }
                        else
                        {
                            newStable.Add(new FsoBuildItemViewModel(build, this));
                        }
                        break;
                    case FsoStability.RC:
                        var previousRc = newRc.FirstOrDefault(b => string.Compare(b.Date, build.date) < 0);
                        if (previousRc != null)
                        {
                            int index = newRc.IndexOf(previousRc);
                            if (index == -1)
                                index = 0;
                            newRc.Insert(index, new FsoBuildItemViewModel(build, this));
                        }
                        else
                        {
                            newRc.Add(new FsoBuildItemViewModel(build, this));
                        }
                        break;
                    case FsoStability.Nightly:
                        var previousNL = newNightly.FirstOrDefault(b => string.Compare(b.Date, build.date) < 0);
                        if (previousNL != null)
                        {
                            int index = newNightly.IndexOf(previousNL);
                            if (index == -1)
                                index = 0;
                            newNightly.Insert(index, new FsoBuildItemViewModel(build, this));
                        }
                        else
                        {
                            newNightly.Add(new FsoBuildItemViewModel(build, this));
                        }
                        break;
                    case FsoStability.Custom:
                        var previousCu = newCustom.FirstOrDefault(b => string.Compare(b.Date, build.date) < 0);
                        if (previousCu != null)
                        {
                            int index = newCustom.IndexOf(previousCu);
                            if (index == -1)
                                index = 0;
                            newCustom.Insert(index, new FsoBuildItemViewModel(build, this));
                        }
                        else
                        {
                            newCustom.Add(new FsoBuildItemViewModel(build, this));
                        }
                        break;
                }
            }

            StableItems = newStable;
            RcItems = newRc;
            NightlyItems = newNightly;
            CustomItems = newCustom;
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
                MainWindowViewModel.Instance?.RunModStatusChecks();
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
