using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
    public partial class CustomHomeViewModel : ViewModelBase
    {
        private List<Mod> modVersions = new List<Mod>();
        private List<Mod> nebulaModVersions = new List<Mod>();
        private CancellationTokenSource? cancellationTokenSource = null;

        [ObservableProperty]
        internal int activeVersionIndex = 0;

        internal ObservableCollection<string> VersionItems { get; set; } = new ObservableCollection<string>();

        [ObservableProperty]
        internal bool installed = false;

        [ObservableProperty]
        internal bool installing = false;

        [ObservableProperty]
        internal bool update = false;

        [ObservableProperty]
        internal bool nebulaVersionsAvailable = false;

        [ObservableProperty]
        internal string? backgroundImage = CustomLauncher.HomeBackgroundImage;

        [ObservableProperty]
        internal int animate = 0;

        public CustomHomeViewModel()
        {
        }

        internal void HardcodedButtonCommand(object cmd)
        {
            if (ActiveVersionIndex == -1)
            {
                Log.Add(Log.LogSeverity.Error, "CustomHomeViewModel.HardcodedButtonCommand()", "Crash prevented: ActiveVersionIndex was " + ActiveVersionIndex + " and modVersions.Count() was " + modVersions.Count());
                ActiveVersionIndex = 0;
            }
            switch ((string)cmd)
            {
                case "play": Knossos.PlayMod(modVersions[ActiveVersionIndex], FsoExecType.Release); break;
                case "playvr": Knossos.PlayMod(modVersions[ActiveVersionIndex], FsoExecType.Release, false, 0, true); break;
                case "fred2": Knossos.PlayMod(modVersions[ActiveVersionIndex], FsoExecType.Fred2); break;
                case "fred2debug": Knossos.PlayMod(modVersions[ActiveVersionIndex], FsoExecType.Fred2Debug); break;
                case "debug": Knossos.PlayMod(modVersions[ActiveVersionIndex], FsoExecType.Debug); break;
                case "qtfred": Knossos.PlayMod(modVersions[ActiveVersionIndex], FsoExecType.QtFred); break;
                case "qtfreddebug": Knossos.PlayMod(modVersions[ActiveVersionIndex], FsoExecType.QtFredDebug); break;
                case "install": Install(); break;
                case "cancel": Cancel(); break;
                case "update":  break;
                case "modify":  break;
                case "delete": break;
                case "details": break;
                case "settings":  break;
                case "logfile":  break;
            }
        }

        private void Cancel()
        {
            Installing = false;
            try
            {
                cancellationTokenSource?.Cancel();
            }
            catch { }
            cancellationTokenSource = null;
            TaskViewModel.Instance?.CancelAllInstallTaskWithID(CustomLauncher.ModID!, null);
        }

        private async void Install()
        {
            if (nebulaModVersions.Any())
            {
                var dialog = new ModInstallView();
                dialog.DataContext = new ModInstallViewModel(nebulaModVersions.First(), dialog);
                await dialog.ShowDialog<ModInstallView?>(MainWindow.instance!);
            }
            else
            {
                Log.Add(Log.LogSeverity.Error, "CustomHomeViewModel.Install()", "Tried to install but no nebula versions were loaded.");
            }
        }

        public void RemoveInstalledModVersion(Mod mod)
        {
            if (CustomLauncher.ModID == mod.id)
            {
                Installed = modVersions.Any();
                Installing = false;
            }
        }

        public void RemoveMod(string id)
        {
            if (CustomLauncher.ModID == id)
            {
                Installed = false;
                Installing = false;
            }
        }

        public void CancelModInstall(string id)
        {
            if (CustomLauncher.ModID == id)
            {
                Cancel();
            }
        }

        public void SetInstalling(string id, CancellationTokenSource cancelToken)
        {
            cancellationTokenSource = cancelToken;
            Installing = true;
        }

        /// <summary>
        /// Add a installed mod version of this TC.
        /// It will check if the ID matches the one in CustomLauncher.ModID
        /// </summary>
        /// <param name="modJson"></param>
        public void AddModVersion(Mod modJson)
        {
            if (modJson.id == CustomLauncher.ModID)
            {
                Log.Add(Log.LogSeverity.Information, "CustomHomeViewModel.AddModVersion()", "Adding additional version for mod id: " + CustomLauncher.ModID + " -> " + modJson.folderName);
                if (modVersions.Any())
                {
                    string currentVersion = modVersions[ActiveVersionIndex].version;
                    modVersions.Add(modJson);
                    modVersions.Sort((o1, o2) => -SemanticVersion.Compare(o1.version, o2.version));
                    if (SemanticVersion.Compare(modJson.version, currentVersion) > 0)
                    {
                        Log.Add(Log.LogSeverity.Information, "CustomHomeViewModel.AddModVersion()", "Changing active version for " + modJson.title + " from " + modVersions[ActiveVersionIndex].version + " to " + modJson.version);
                        VersionItems.Clear();
                        modVersions.ForEach(x => VersionItems.Add(x.version));
                        ActiveVersionIndex = -1;
                        ActiveVersionIndex = modVersions.FindIndex((m) => m.version.Equals(modJson.version));
                    }
                    else
                    {
                        VersionItems.Add(modJson.version);
                    }
                }
                else
                {
                    ActiveVersionIndex = -1;
                    modVersions.Add(modJson);
                    VersionItems.Add(modJson.version);
                    ActiveVersionIndex = 0;
                }
                Installed = modVersions.Any();
            }
        }

        /// <summary>
        /// Add a Nebula mod version of this TC.
        /// It will check if the ID matches the one in CustomLauncher.ModID
        /// </summary>
        /// <param name="modJson"></param>
        public void AddNebulaModVersion(Mod modJson)
        {
            if (modJson.id == CustomLauncher.ModID)
            {
                Log.Add(Log.LogSeverity.Information, "CustomHomeViewModel.AddNebulaModVersion()", "Adding additional nebula version for mod id: " + CustomLauncher.ModID + " -> " + modJson.version);
                nebulaModVersions.Add(modJson);
                nebulaModVersions.Sort((o1, o2) => -SemanticVersion.Compare(o1.version, o2.version));
                NebulaVersionsAvailable = true;
            }
        }

        /// <summary>
        /// Tell the TC home screen an update is avalible or not
        /// It will check if the mod id actually matches to the custom CustomLauncher.ModID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        public void UpdateIsAvailable(string id, bool value)
        {
            if (id == CustomLauncher.ModID)
            {
                Update = value;
            }
        }

        /// <summary>
        /// Run code when the user clicks the menu item to open this view
        /// </summary>
        public void ViewOpened()
        {
            Animate = 1;

            //download remote image if we have to
            if (BackgroundImage != null && BackgroundImage.ToLower().StartsWith("http"))
            {
                _ = Task.Factory.StartNew(async () =>
                {
                    var temp = BackgroundImage;
                    BackgroundImage = "";
                    var imageFile = await KnUtils.GetRemoteResource(temp).ConfigureAwait(false);
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        if (imageFile != null)
                            BackgroundImage = imageFile;
                    });
                });
            }
        }

        /// <summary>
        /// Run code when the user exit this view
        /// </summary>
        public void ViewClosed()
        {
            Animate = 0;
        }
    }
}
