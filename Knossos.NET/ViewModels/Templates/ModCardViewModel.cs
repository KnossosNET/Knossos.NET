using Avalonia.Platform;
using Avalonia.Media.Imaging;
using System;
using Knossos.NET.Models;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using Knossos.NET.Classes;
using Knossos.NET.Views;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Avalonia.Threading;

namespace Knossos.NET.ViewModels
{
    public partial class ModCardViewModel : ViewModelBase, IComparable<ModCardViewModel>
    {
        /* General Mod variables */
        private readonly List<Mod> modVersions = new();
        private Mod? preferredVersion;
        private string? libraryFilter;
        public string ID { get; set; }

        public Mod? ActiveVersion { get; private set; }
        private int ActiveVersionIndex => ActiveVersion == null ? -1 : modVersions.IndexOf(ActiveVersion);
        internal bool MatchesLibraryFilter { get; private set; } = true;

        /* UI Bindings */
        [ObservableProperty]
        internal string? name;
        [ObservableProperty]
        internal string? modVersion;
        [ObservableProperty]
        internal Bitmap? image;
        private bool visible = true;
        /// <summary>
        /// External property to enable/disable mod card visibility
        /// </summary>
        public bool Visible
        {
            get
            {
                return visible;
            }
            set
            {
                if (visible != value)
                {
                    visible = value;
                    CardVisible = value;
                    if (CardVisible)
                    {
                        _ = LazyReLoadTileImageAsync();
                    }
                    else
                    {
                        Image = MainViewModel.Instance?.placeholderTileImage;
                    }
                }
            }
        }
        [ObservableProperty]
        internal bool updateAvailable = false;
        [ObservableProperty]
        internal IBrush borderColor = KnUtils.GetResourceColor("ModCardBorderNormal");
        [ObservableProperty]
        internal bool isLocalMod = false;
        [ObservableProperty]
        internal bool isCustomConfig = false;
        [ObservableProperty]
        internal bool cardVisible = true;
        [ObservableProperty]
        internal bool isQtFredAvailable = false;
        [ObservableProperty]
        internal bool isQtFredDebugAvailable = false;
        private Bitmap? tileModBitmap;

        public bool androidButtonsEnabled = false;

        /* Should only be used by the editor preview */
        public ModCardViewModel()
        {
            Name = "default test string very long";
            ModVersion = "1.0.0";
            ID = "test";
            Image = new Bitmap(AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/general/NebulaDefault.png")));
        }

        public ModCardViewModel(Mod modJson)
        {
            modJson.ClearUnusedData();
            modVersions.Add(modJson);
            ActiveVersion = preferredVersion = modJson;
            Name = modJson.title;
            ModVersion = modJson.version;
            if (modJson.modSource == ModSource.local)
            {
                isLocalMod = true;
            }
            ID = modJson.id;
            if (modJson.devMode && ID != "FS2")
            {
                BorderColor = KnUtils.GetResourceColor("ModCardBorderDevMode");
            }
            LoadImage();
            FreespaceViewModel.Instance?.SetInstalled(ID, true);
        }

        /// <summary>
        /// Reloads the tile image to the view at a random time
        /// after the mod card itseft becomes visible
        /// </summary>
        private async Task LazyReLoadTileImageAsync()
        {
            await Task.Delay(new Random().Next(200, 700));
            Image = tileModBitmap != null ? tileModBitmap : MainViewModel.Instance?.placeholderTileImage;
        }

        public void AddModVersion(Mod modJson)
        {
            modJson.ClearUnusedData();
            Log.Add(Log.LogSeverity.Information, "ModCardViewModel.AddModVersion()", "Adding additional version for mod id: " + ID + " -> " + modJson.folderName);
            modVersions.Add(modJson);
            modVersions.Sort((o1, o2) => -SemanticVersion.Compare(o1.version, o2.version));
            if (preferredVersion == null || SemanticVersion.Compare(modJson.version, preferredVersion.version) > 0)
            {
                preferredVersion = modJson;
            }
            UpdateActiveVersion();
        }

        public async void CheckDependencyActiveVersion()
        {
            if (ActiveVersion is not { } activeVersion) return;

            await Task.Run(() =>
            {
                if (activeVersion.installed && !activeVersion.devMode && !UpdateAvailable)
                {
                    var missingDeps = activeVersion.GetMissingDependenciesList();
                    foreach (var dependency in missingDeps.ToList())
                    {
                        //If we are missing the FSO dep and the user selected a custom build, ignore it
                        if (activeVersion.modSettings.customBuildId != null && dependency.id == "FSO")
                        {
                            missingDeps.Remove(dependency);
                        }
                    }
                    if (missingDeps.Any())
                    {
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (ActiveVersion == activeVersion && !UpdateAvailable)
                                BorderColor = KnUtils.GetResourceColor("ModCardBorderError");
                        });
                    }
                    else
                    {
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (ActiveVersion == activeVersion && !UpdateAvailable)
                                BorderColor = KnUtils.GetResourceColor("ModCardBorderNormal");
                        });
                    }
                }

                RefreshQtFredAvailability();
            });
        }

        public void SwitchModVersion(int newIndex, bool force = false)
        {
            preferredVersion = modVersions[newIndex];
            UpdateActiveVersion(force);
        }

        internal bool SetLibraryFilter(string? path)
        {
            libraryFilter = path;
            return UpdateActiveVersion();
        }

        private bool UpdateActiveVersion(bool force = false)
        {
            var versionsInLibrary = libraryFilter == null ? modVersions : modVersions.Where(IsInFilteredLibrary).ToList();
            MatchesLibraryFilter = libraryFilter == null || versionsInLibrary.Count > 0;
            var selected = preferredVersion != null && versionsInLibrary.Contains(preferredVersion)
                ? preferredVersion
                : versionsInLibrary.FirstOrDefault() ?? preferredVersion;
            if (selected == null) return false;

            var count = versionsInLibrary.Count;
            ModVersion = count > 1 ? selected.version + " (+" + (count - 1) + ")" : selected.version;
            if (!force && ActiveVersion == selected) return false;

            ActiveVersion = selected;
            Name = selected.title;
            IsLocalMod = selected.modSource == ModSource.local;
            if (!UpdateAvailable)
                BorderColor = KnUtils.GetResourceColor(selected.devMode && ID != "FS2" ? "ModCardBorderDevMode" : "ModCardBorderNormal");
            LoadImage();
            CheckDependencyActiveVersion();
            RefreshSpecialIcons();
            RefreshQtFredAvailability();
            return true;
        }

        private bool IsInFilteredLibrary(Mod mod) =>
            Knossos.LibraryPathComparer.Equals(libraryFilter, Knossos.GetLibraryForPath(mod.fullPath));

        /// <summary>
        /// Deletes a mod from the mod versions list and switches the active version to the newerest
        /// </summary>
        /// <param name="mod"></param>
        public void DeleteModVersion(Mod mod)
        {
            try
            {
                modVersions.Remove(mod);
                if (preferredVersion == mod) preferredVersion = modVersions.FirstOrDefault();
                UpdateActiveVersion();
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "ModCardViewModel.DeleteModVersion()", ex);
            }
        }

        /// <summary>
        /// Returns the number of mods in the modVersions list
        /// </summary>
        /// <returns>int</returns>
        public int GetNumberOfModVersions()
        {
            return modVersions.Count();
        }

        public void UpdateIsAvailable(bool value)
        {
            UpdateAvailable = value;
            if (value)
            {
                BorderColor = KnUtils.GetResourceColor("ModCardBorderUpdate");
            }
            else
            {
                BorderColor = KnUtils.GetResourceColor("ModCardBorderNormal");
            }
        }

        public void RefreshSpecialIcons()
        {
            IsCustomConfig = ActiveVersion?.modSettings.IsDefaultConfig() == false;
        }

        public void RefreshQtFredAvailability()
        {
            var bools = ActiveVersion?.IsQtFredAvailable();

            if (bools == null || bools.Length < 2)
            {
                IsQtFredAvailable = false;
                IsQtFredDebugAvailable = false;
                return;
            }

            IsQtFredAvailable = bools[0];
            IsQtFredDebugAvailable = bools[1];
        }

        /* Button Commands */
        internal void ButtonCommand(object command)
        {
            if (KnUtils.IsAndroid && !androidButtonsEnabled)
            {
                return;
            }
            ButtonCommandRelay(command);
        }

        internal void ButtonCommandRelay(object command)
        {
            if (ActiveVersion is not { } activeVersion) return;

            switch ((string)command)
            {
                case "play": Knossos.PlayMod(activeVersion, FsoExecType.Release); break;
                case "playvr": Knossos.PlayMod(activeVersion, FsoExecType.Release, false, 0, true); break;
                case "playvrdebug": Knossos.PlayMod(activeVersion, FsoExecType.Debug, false, 0, true); break;
                case "fred2": Knossos.PlayMod(activeVersion, FsoExecType.Fred2); break;
                case "fred2debug": Knossos.PlayMod(activeVersion, FsoExecType.Fred2Debug); break;
                case "debug": Knossos.PlayMod(activeVersion, FsoExecType.Debug); break;
                case "qtfred": Knossos.PlayMod(activeVersion, FsoExecType.QtFred); break;
                case "qtfreddebug": Knossos.PlayMod(activeVersion, FsoExecType.QtFredDebug); break;
                case "update": ButtonCommandUpdate(); break;
                case "modify": ButtonCommandModify(); break;
                case "delete": ButtonCommandDelete(); break;
                case "details": ButtonCommandDetails(); break;
                case "settings": ButtonCommandSettings(); break;
            }
        }

        public void SelectVersion(string version)
        {
            var index = modVersions.FindIndex(m => m.version == version);
            if (index >= 0) SwitchModVersion(index);
        }

        internal async void ButtonCommandUpdate()
        {
            var dialog = new ModPropertiesView();
            dialog.DataContext = ModPropertiesViewModel.GetOrCreate(modVersions, ActiveVersionIndex, this, dialog, ModPropertiesViewModel.MenuPageEntry.Versions);
            await dialog.ShowDialog<ModPropertiesView?>(MainWindow.instance);
        }

        internal async void ButtonCommandModify()
        {
            var dialog = new ModPropertiesView();
            dialog.DataContext = ModPropertiesViewModel.GetOrCreate(modVersions, ActiveVersionIndex, this, dialog, ModPropertiesViewModel.MenuPageEntry.Versions);
            await dialog.ShowDialog<ModPropertiesView?>(MainWindow.instance);
        }

        internal async void ButtonCommandDelete()
        {
            if (ActiveVersion is not { } activeVersion) return;

            if (!activeVersion.devMode)
            {
                if (TaskViewModel.Instance!.IsSafeState())
                {
                    var result = await MessageBox.Show(MainWindow.instance, "Delete all installed versions of " + Name + "? To delete a specific version, open Versions & Packages in Mod Properties.", "Delete mod", MessageBox.MessageBoxButtons.OKCancel);
                    if (result == MessageBox.MessageBoxResult.OK)
                    {
                        modVersions[modVersions.Count - 1].installed = false;
                        if (!IsLocalMod)
                        {
                            MainViewModel.Instance?.AddNebulaMod(modVersions[modVersions.Count - 1]);
                        }
                        Knossos.RemoveMod(activeVersion.id);
                        MainViewModel.Instance?.RunModStatusChecks();
                        FreespaceViewModel.Instance?.SetInstalled(ID, false);
                        if (Knossos.globalSettings.hiddenModIds.Remove(activeVersion.id))
                        {
                            Knossos.globalSettings.Save();
                        }
                    }
                }
                else
                {
                    await MessageBox.Show(MainWindow.instance, "You can not delete a mod while other install tasks are running, wait until they finish and try again.", "Tasks are running", MessageBox.MessageBoxButtons.OK);
                }
            }
            else
            {
                await MessageBox.Show(MainWindow.instance, "Dev mode mods can not be delated from the main view, go to the Development section.", "Mod is dev mode", MessageBox.MessageBoxButtons.OK);
            }
        }

        internal async void ButtonCommandDetails()
        {
            var dialog = new ModPropertiesView();
            dialog.DataContext = ModPropertiesViewModel.GetOrCreate(modVersions, ActiveVersionIndex, this, dialog);
            await dialog.ShowDialog<ModPropertiesView?>(MainWindow.instance);
        }

        internal async void ButtonCommandSettings()
        {
            var dialog = new ModPropertiesView();
            dialog.DataContext = ModPropertiesViewModel.GetOrCreate(modVersions, ActiveVersionIndex, this, dialog,
                ModPropertiesViewModel.MenuPageEntry.Options);
            await dialog.ShowDialog<ModPropertiesView?>(MainWindow.instance);
        }

        private void LoadImage()
        {
            Image = MainViewModel.Instance?.placeholderTileImage;
            tileModBitmap = null;
            if (ActiveVersion is not { } activeVersion) return;

            try
            {
                var tile = activeVersion.tile;
                if (!string.IsNullOrEmpty(tile))
                {
                    if (!tile.ToLower().Contains("http"))
                    {
                        tileModBitmap = new Bitmap(activeVersion.fullPath + Path.DirectorySeparatorChar + tile);
                    }
                    else
                    {
                        Task.Run(async () =>
                        {
                            using (var fs = await KnUtils.GetRemoteResourceStream(tile))
                            {
                                if (fs == null) return;
                                var bitmap = new Bitmap(fs);
                                Dispatcher.UIThread.Post(() =>
                                {
                                    if (ActiveVersion == activeVersion)
                                    {
                                        tileModBitmap = bitmap;
                                        if (Visible) Image = bitmap;
                                    }
                                    else
                                    {
                                        bitmap.Dispose();
                                    }
                                });
                            }
                        });
                    }
                    if (tileModBitmap != null)
                    {
                        Image = tileModBitmap;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "ModCardViewModel.LoadImage", ex);
            }
        }

        public int CompareTo(ModCardViewModel? other)
        {
            if (other == null)
                return -1;
            return Mod.SortMods(ActiveVersion, other.ActiveVersion);
        }
    }
}
