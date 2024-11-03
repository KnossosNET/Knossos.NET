using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;

namespace Knossos.NET.ViewModels
{
    /// <summary>
    /// Nebula mod card container view, this is the "explore" tab
    /// </summary>
    public partial class NebulaModListViewModel : ViewModelBase
    {

        /// <summary>
        /// Current Sort Mode
        /// </summary>
        internal MainWindowViewModel.SortType sortType = MainWindowViewModel.SortType.unsorted;

        /// <summary>
        /// The user has opened this tab in this session?
        /// </summary>
        internal bool IsTabOpen = false;

        private bool _isLoading = true;
        internal bool isLoading{
            get { return _isLoading; }
            set { 
                _isLoading = value;
                ShowTiles = !sorting && !isLoading;
                if (ShowTiles)
                {
                    LoadingAnimation.Animate = 0;
                } 
                else  
                {
                    LoadingAnimation.Animate = 1;
                }        
            }
        }

        private bool _sorting = true;
        internal bool sorting {
            get { return _sorting; }
            set { 
                _sorting = value;
                ShowTiles = !sorting && !isLoading;

                if (ShowTiles)
                {
                    LoadingAnimation.Animate = 0;                
                } 
                else  
                {
                    LoadingAnimation.Animate = 1;
                }
            }
        }

        /// <summary>
        /// For the UI to detmerine whether to show mod tiles.  It needs to check more than one property, so this gets updated when sorting or isLoading do.
        /// </summary>
        [ObservableProperty]
        internal bool showTiles = false;

        [ObservableProperty]
        internal LoadingIconViewModel loadingAnimation = new LoadingIconViewModel();

        /// <summary>
        /// Search string
        /// </summary>
        internal string search = string.Empty;
        internal string Search
        {
            get { return search; }
            set 
            {
                sorting = true;
                LoadingAnimation.Animate = 1;

                if (value != Search){
                    this.SetProperty(ref search, value);
                    if (value.Trim() != string.Empty)
                    {
                        foreach(var mod in Mods)
                        {
                            if( mod.Name != null && mod.Name.ToLower().Contains(value.ToLower()))
                            {
                                mod.Visible = true;
                            }
                            else
                            {
                                mod.Visible = false;
                            }
                        }
                    }
                    else
                    {
                        Mods.ForEach(m => m.Visible = true);
                    }
                }
                sorting = false;
                LoadingAnimation.Animate = 1;
            }
        }

        [ObservableProperty]
        internal ObservableCollection<NebulaModCardViewModel> mods = new ObservableCollection<NebulaModCardViewModel>();

        public NebulaModListViewModel()
        {
        }

        /// <summary>
        /// Open the tab and slowly display modcards to avoid ui lock
        /// </summary>
        public async void OpenTab(string newSearch, MainWindowViewModel.SortType newSortType)
        {
            Search = newSearch;
            if (isLoading)
            {
                IsTabOpen = true;
                return;
            }

            if (!IsTabOpen)
            {
                IsTabOpen = true;
                // This should remain true until we get to Change Sort.  It is guaranteed to be finished then
                sorting = true;

                try
                {
                    await Task.Delay(200).ConfigureAwait(false);
                    List<NebulaModCardViewModel>? modsInView = null;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        isLoading = false;
                        modsInView = Mods.ToList();
                    });
                    if (modsInView != null)
                    {
                        foreach (var m in modsInView)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                if (Search.Trim() == string.Empty || m.Name != null && m.Name.ToLower().Contains(Search.ToLower()))
                                {
                                    m.Visible = true;
                                }
                            });
                            await Task.Delay(1).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Add(Log.LogSeverity.Error, "NebulaModListViewModel.OpenTab", ex);
                }

            }
            ChangeSort(newSortType);
        }

        /// <summary>
        /// Reload all mods from nebula
        /// </summary>
        public void ReloadRepoCommand()
        {
            Knossos.ResetBasePath();
        }

        /// <summary>
        /// Clears all mods in view
        /// </summary>
        public void ClearView()
        {
            Mods.Clear();
        }

        /// <summary>
        /// Adds a single mod into the view, it will be inserted in order depending on the current select sort mode
        /// </summary>
        /// <param name="modJson"></param>
        public void AddMod(Mod modJson)
        {
            var modCard = Mods.FirstOrDefault(m => m.ID == modJson.id);
            if (modCard == null)
            {
                int i;
                for (i = 0; i < Mods.Count; i++)
                {
                    if (Mods[i].modJson != null)
                    {
                        if(CompareMods(Mods[i].modJson!,modJson) > 0)
                        {
                            break;
                        }
                    }
                }
                var card = new NebulaModCardViewModel(modJson);
                if (!isLoading)
                {
                    if (Search.Trim() == string.Empty || card.Name != null && card.Name.ToLower().Contains(Search.ToLower()))
                    {
                        card.Visible = true;
                    }
                }
                Mods.Insert(i, card);
            }
            else
            {
                //Update? Should NOT be needed for Nebula mods
            }
        }

        /// <summary>
        /// Adds a new list of mods into the, in a more efficient way that loading one by one
        /// It replaces all mods, all old mods are deleted, intended only for the first big load of mods
        /// </summary>
        /// <param name="modList"></param>
        public async void AddMods(List<Mod> modList)
        {
            isLoading = true;
            await Task.Delay(20).ConfigureAwait(false);
            var newModCardList = new ObservableCollection<NebulaModCardViewModel>();
            foreach (Mod? mod in modList)
            {
                var modCard = newModCardList.FirstOrDefault(m => m.ID == mod.id);
                if (modCard == null)
                {
                    int i;
                    for (i = 0; i < newModCardList.Count; i++)
                    {
                        if (newModCardList[i].modJson != null)
                        {
                            if (CompareMods(newModCardList[i].modJson!, mod) > 0)
                            {
                                break;
                            }
                        }
                    }
                    var card = new NebulaModCardViewModel(mod);
                    newModCardList.Insert(i, card);
                }
                else
                {
                    //Update? Should NOT be needed for Nebula mods
                }
            }
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Mods = newModCardList;
                isLoading = false;
                if (IsTabOpen)
                {
                    IsTabOpen = false;
                    OpenTab(Search, sortType);
                }
            });
        }

        /// <summary>
        /// Change sorting mode and re-order the list of mods
        /// </summary>
        /// <param name="sort"></param>
        internal void ChangeSort(object sort)
        {
            try
            {
                MainWindowViewModel.SortType newSort;

                if (sort is MainWindowViewModel.SortType){
                    newSort = (MainWindowViewModel.SortType)sort;
                } else {
                    newSort = (MainWindowViewModel.SortType)Enum.Parse(typeof(MainWindowViewModel.SortType), (string)sort);
                }

                if (newSort != sortType)
                {
                    if (MainWindowViewModel.Instance != null && newSort != MainWindowViewModel.SortType.unsorted && MainWindowViewModel.Instance.sharedSortType != newSort)
                    {
                        MainWindowViewModel.Instance.sharedSortType = newSort;
                    }
                    if (sortType != MainWindowViewModel.SortType.unsorted)
                    {
                        sorting = true;
                    }

                    Dispatcher.UIThread.Invoke( () =>
                    {
                        sortType = newSort;
                        var tempList = Mods.ToList();
                        tempList.Sort(CompareMods);
                        isLoading = true;
                        for (int i = 0; i < tempList.Count; i++)
                        {
                            Mods.Move(Mods.IndexOf(tempList[i]), i);
                        }
                        isLoading = false;
                        GC.Collect();
                    },DispatcherPriority.Background);
                }
            }catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "ModListViewModel.ChangeSort()", ex);
            }

            // There is no reason to keep this on, whether in success or fail, and some of the functions that call this
            // set sorting to true.
            sorting = false;
        }

        private int CompareMods(NebulaModCardViewModel x, NebulaModCardViewModel y)
        {
            if (x.modJson != null && y.modJson != null)
                return CompareMods(x.modJson, y.modJson);
            else
                return 0;
        }

        private int CompareMods(Mod modA,Mod modB)
        {
            try
            {
                switch (sortType)
                {
                    case MainWindowViewModel.SortType.name:
                        return Mod.CompareTitles(modA.title, modB.title);
                    case MainWindowViewModel.SortType.release:
                        if (modA.firstRelease == modB.firstRelease)
                            return 0;
                        if (modA.firstRelease != null && modB.firstRelease != null)
                        {
                            if (DateTime.Parse(modA.firstRelease, CultureInfo.InvariantCulture) < DateTime.Parse(modB.firstRelease, CultureInfo.InvariantCulture))
                                return 1;
                            else
                                return -1;
                        }
                        else
                        {
                            if (modA.firstRelease == null)
                                return -1;
                            else
                                return 1;
                        }
                    case MainWindowViewModel.SortType.update:
                        if (modA.lastUpdate == modB.lastUpdate)
                            return 0;
                        if (modA.lastUpdate != null && modB.lastUpdate != null)
                        {
                            if (DateTime.Parse(modA.lastUpdate, CultureInfo.InvariantCulture) < DateTime.Parse(modB.lastUpdate, CultureInfo.InvariantCulture))
                                return 1;
                            else
                                return -1;
                        }
                        else
                        {
                            if (modA.lastUpdate == null)
                                return 1;
                            else
                                return -1;
                        }
                    default: return 0;
                }
            }catch(Exception ex)
            { 
                Log.Add(Log.LogSeverity.Warning, "NebulaModListViewModel.CompareMods()",ex.Message);
                return 0; 
            }
        }

        /// <summary>
        /// Changes a modcard to "installing" mode
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancelToken"></param>
        public void SetInstalling(string id, CancellationTokenSource cancelToken)
        {
            var modCard = Mods.FirstOrDefault(m => m.ID == id);
            if (modCard != null)
            {
                modCard.InstallingMod(cancelToken);
            }
        }

        /// <summary>
        /// Removes a mod id from view
        /// </summary>
        /// <param name="id"></param>
        public void RemoveMod(string id)
        {
            var modCard = Mods.FirstOrDefault(m => m.ID == id);
            if (modCard != null)
            {
                Mods.Remove(modCard);
            }
        }

        /// <summary>
        /// Cancels a mod install of a mod id in view, removing the "installing" mode 
        /// </summary>
        /// <param name="id"></param>
        public void CancelModInstall(string id)
        {
            var modCard = Mods.FirstOrDefault(m => m.ID == id);
            if (modCard != null)
            {
                modCard.CancelInstall();
            }
        }
    }
}
