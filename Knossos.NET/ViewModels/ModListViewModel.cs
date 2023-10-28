using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;

namespace Knossos.NET.ViewModels
{
    public partial class ModListViewModel : ViewModelBase
    {
        enum SortType
        {
            name,
            release,
            update
        }

        internal bool IsNebulaView { get; set; } = false;

        internal string search  = string.Empty;

        private SortType sortType = SortType.name;

        internal bool IsTabOpen = false;

        [ObservableProperty]
        internal bool isLoading = true;

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
        internal ObservableCollection<ModCardViewModel> mods = new ObservableCollection<ModCardViewModel>();

        public ModListViewModel()
        {
        }

        public ModListViewModel(bool isNebulaView)
        {
            IsNebulaView = isNebulaView;
            if (!isNebulaView)
            {
                IsLoading = false;
                IsTabOpen = true;
            }
        }

        public async void OpenTab()
        {
            if (!IsLoading && !IsTabOpen)
            {
                await Dispatcher.UIThread.InvokeAsync(async ()  =>
                {
                    try
                    {
                        await Task.Delay(100);
                        IsLoading = false;
                        foreach (var m in Mods)
                        {
                            m.Visible = true;
                            await Task.Delay(5);
                        }
                    }
                    catch { }
                });
            }
            IsTabOpen = true;
        }

        public void ReloadRepoCommand()
        {
            Knossos.ResetBasePath();
        }

        public void ClearView()
        {
            Mods.Clear();
        }

        public void RunModStatusChecks()
        {
            Mods.ForEach(m => m.RefreshSpecialIcons());
            Mods.ForEach(m => m.CheckDependencyActiveVersion());
        }

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

        public async void AddMods(List<Mod> modList)
        {
            var newModCardList = new ObservableCollection<ModCardViewModel>();
            foreach (Mod? mod in modList)
            {
                var modCard = newModCardList.FirstOrDefault(m => m.ID == mod.id);
                if (modCard == null)
                {
                    int i;
                    for (i = 0; i < newModCardList.Count; i++)
                    {
                        if (newModCardList[i].ActiveVersion != null)
                        {
                            if (CompareMods(newModCardList[i].ActiveVersion!, mod) > 0)
                            {
                                break;
                            }
                        }
                    }
                    newModCardList.Insert(i, new ModCardViewModel(mod));
                }
                else
                {
                    modCard.AddModVersion(mod);
                }
            }
            newModCardList.ForEach(m=>m.Visible = false);
            Mods = newModCardList;
            if (IsTabOpen)
            {
                try
                {
                    await Task.Delay(100);
                    IsLoading = false;
                    foreach (var m in Mods)
                    {
                        m.Visible = true;
                        await Task.Delay(5);
                    }
                }
                catch { }
            }
            IsLoading = false;
        }

        internal void OpenScreenshotsFolder()
        {
            try
            {
                KnUtils.OpenFolder(KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "screenshots");
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "MainWindowViewModel.OpenScreenshotsFolder", ex);
            }
        }

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
                Log.Add(Log.LogSeverity.Warning, "ModListViewModel.CompareMods()",ex.Message);
                return 0; 
            }
        }

        public void UpdateIsAvalible(string id,bool value)
        {
            var modCard = Mods.FirstOrDefault(m => m.ID == id);
            if (modCard != null)
            {
                modCard.UpdateIsAvalible(value);
            }
        }

        public void SetInstalling(string id, CancellationTokenSource cancelToken)
        {
            var modCard = Mods.FirstOrDefault(m => m.ID == id);
            if (modCard != null)
            {
                modCard.InstallingMod(cancelToken);
            }
        }

        public void RemoveMod(string id)
        {
            var modCard = Mods.FirstOrDefault(m => m.ID == id);
            if (modCard != null)
            {
                Mods.Remove(modCard);
            }
        }

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
