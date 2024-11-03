using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;

namespace Knossos.NET.ViewModels
{
    /// <summary>
    /// Installed mod card container view, this is the "Home" tab
    /// </summary>
    public partial class ModListViewModel : ViewModelBase
    {
        /// <summary>
        /// Is this tab in the middle of sorting or filtering mod tiles
        /// </summary>
        [ObservableProperty]
        internal bool sorting = false;

        /// <summary>
        /// For the knossos Loading animation 
        /// </summary>
        [ObservableProperty]
        internal LoadingIconViewModel loadingAnimation;

        /// <summary>
        /// Current Sort Mode
        /// </summary>
        internal MainWindowViewModel.SortType sortType = MainWindowViewModel.SortType.unsorted;

        internal string search = string.Empty;
        internal string Search
        {
            get { return search; }
            set 
            {
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
            }
        }

        [ObservableProperty]
        internal ObservableCollection<ModCardViewModel> mods = new ObservableCollection<ModCardViewModel>();

        public ModListViewModel()
        {
            LoadingAnimation = new LoadingIconViewModel();

        }


        /// <summary>
        /// Clears all mods in view
        /// </summary>
        public void ClearView()
        {
            Mods.Clear();
        }

        /// <summary>
        /// Updates border color and status icons on all mod cards
        /// </summary>
        public void RunModStatusChecks()
        {
            Mods.ForEach(m => m.RefreshSpecialIcons());
            Mods.ForEach(m => m.CheckDependencyActiveVersion());
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
                    if (Mods[i].ActiveVersion != null)
                    {
                        if(CompareMods(Mods[i].ActiveVersion!,modJson) > 0)
                        {
                            break;
                        }
                    }
                }
                Mods.Insert(i, new ModCardViewModel(modJson));
            }
            else
            {
                modCard.AddModVersion(modJson);
            }
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
                    if (MainWindowViewModel.Instance != null && newSort != MainWindowViewModel.SortType.unsorted  && MainWindowViewModel.Instance.sharedSortType != newSort )
                    {
                        MainWindowViewModel.Instance.sharedSortType = newSort;
                    }
                    LoadingAnimation.Animate = 1;
                    Sorting = true;
                    Dispatcher.UIThread.Invoke( () =>
                    {
                        var tempList = Mods.ToList();
                        // Only sort and update to the new sort type if we have mods to sort!
                        if (tempList.Any()){
                            sortType = newSort;
                            tempList.Sort(CompareMods);
                            for (int i = 0; i < tempList.Count; i++)
                            {
                                Mods.Move(Mods.IndexOf(tempList[i]), i);
                            }
                        }
                        GC.Collect();
                        Sorting = false;
                        LoadingAnimation.Animate = 0;
                    },DispatcherPriority.Background);

                }
            }catch(Exception ex)
            {
                Sorting = false;
                LoadingAnimation.Animate = 0;                
                Log.Add(Log.LogSeverity.Error, "ModListViewModel.ChangeSort()", ex);
            }
        }

        private int CompareMods(ModCardViewModel x, ModCardViewModel y)
        {
            if (x.ActiveVersion != null && y.ActiveVersion != null)
                return CompareMods(x.ActiveVersion, y.ActiveVersion);
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
                Log.Add(Log.LogSeverity.Warning, "ModListViewModel.CompareMods()",ex.Message);
                return 0; 
            }
        }

        /// <summary>
        /// Calls a "UpdateIsAvailable(value)" on the mod card
        /// This would be a external way to telling the mod card that the mod has updates available
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        public void UpdateIsAvailable(string id,bool value)
        {
            var modCard = Mods.FirstOrDefault(m => m.ID == id);
            if (modCard != null)
            {
                modCard.UpdateIsAvailable(value);
            }
        }

        /// <summary>
        /// Remove a mod card from view
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
        /// Deletes a specific mod version from the UI modcard version list
        /// IF this is the last version of a mod the modcard will be deleted instead
        /// </summary>
        /// <param name="mod"></param>
        public void RemoveModVersion(Mod mod)
        {
            var modCard = Mods.FirstOrDefault(m => m.ID == mod.id);
            if (modCard != null)
            {
                if(modCard.GetNumberOfModVersions() > 1)
                {
                    modCard.DeleteModVersion(mod);
                }
                else
                {
                    Mods.Remove(modCard);
                }
            }
        }
    }
}
