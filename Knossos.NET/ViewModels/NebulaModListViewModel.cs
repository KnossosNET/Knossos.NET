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
        enum SortType
        {
            name,
            release,
            update
        }

        /// <summary>
        /// Current Sort Mode
        /// </summary>
        private SortType sortType = SortType.name;

        /// <summary>
        /// The user has opened this tab in this session?
        /// </summary>
        internal bool IsTabOpen = false;

        [ObservableProperty]
        internal bool isLoading = true;

        /// <summary>
        /// Search string
        /// </summary>
        internal string search = string.Empty;
        internal string Search
        {
            get { return search; }
            set 
            {
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

        [ObservableProperty]
        internal ObservableCollection<NebulaModCardViewModel> mods = new ObservableCollection<NebulaModCardViewModel>();

        public NebulaModListViewModel()
        {
        }

        /// <summary>
        /// Open the tab and slowly display modcards to avoid ui lock
        /// </summary>
        public async void OpenTab()
        {
            if (IsLoading)
            {
                IsTabOpen = true;
                return;
            }

            if (!IsTabOpen)
            {
                IsTabOpen = true;
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    try
                    {
                        await Task.Delay(100);
                        IsLoading = false;
                        foreach (NebulaModCardViewModel m in Mods.ToList())
                        {
                            m.Visible = true;
                            await Task.Delay(1);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Add(Log.LogSeverity.Error, "NebulaModListViewModel.OpenTab", ex);
                    }
                });
            }
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
                if (IsLoading)
                    card.Visible = false;
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
            IsLoading = true;
            await Task.Delay(20);
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
                    card.Visible = false;
                    newModCardList.Insert(i, card);
                }
                else
                {
                    //Update? Should NOT be needed for Nebula mods
                }
            }
            Mods = newModCardList;
            IsLoading = false;
            if (IsTabOpen)
            {
                IsTabOpen = false;
                OpenTab();
            }
        }

        /// <summary>
        /// Change sorting mode and re-order the list of mods
        /// </summary>
        /// <param name="sort"></param>
        internal async void ChangeSort(object sort)
        {
            try
            {
                SortType newSort = (SortType)Enum.Parse(typeof(SortType), (string)sort);
                if (newSort != sortType)
                {
                    await Dispatcher.UIThread.InvokeAsync( () =>
                    {
                        sortType = newSort;
                        var tempList = Mods.ToList();
                        tempList.Sort(CompareMods);
                        IsLoading = true;
                        for (int i = 0; i < tempList.Count; i++)
                        {
                            Mods.Move(Mods.IndexOf(tempList[i]), i);
                        }
                        IsLoading = false;
                        GC.Collect();
                    },DispatcherPriority.Background);
                }
            }catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "ModListViewModel.ChangeSort()", ex);
            }
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
                    case SortType.name:
                        return String.Compare(modA.title, modB.title);
                    case SortType.release:
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
                    case SortType.update:
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
