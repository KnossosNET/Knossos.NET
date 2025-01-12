using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Knossos.NET.ViewModels
{
    public partial class CustomHomeViewModel : ViewModelBase
    {
        private List<Mod> modVersions = new List<Mod>();
        private int activeVersionIndex = 0;

        private List<Mod> nebulaModVersions = new List<Mod>();

        [ObservableProperty]
        internal string? modVersion;

        [ObservableProperty]
        internal string? backgroundImage = CustomLauncher.HomeBackgroundImage;

        [ObservableProperty]
        internal int animate = 0;

        public CustomHomeViewModel()
        {
        }

        public void RemoveInstalledModVersion(Mod mod)
        {
            if (CustomLauncher.ModID == mod.id)
            {
            }
        }

        public void RemoveMod(string id)
        {
            if (CustomLauncher.ModID == id)
            {
            }
        }

        public void CancelModInstall(string id)
        {
            if (CustomLauncher.ModID == id)
            {
            }
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
                string currentVersion = modVersions[activeVersionIndex].version;
                modVersions.Add(modJson);
                modVersions.Sort((o1, o2) => -SemanticVersion.Compare(o1.version, o2.version));
                if (SemanticVersion.Compare(modJson.version, currentVersion) > 0)
                {
                    Log.Add(Log.LogSeverity.Information, "CustomHomeViewModel.AddModVersion()", "Changing active version for " + modJson.title + " from " + modVersions[activeVersionIndex].version + " to " + modJson.version);
                    activeVersionIndex = modVersions.FindIndex((m) => m.version.Equals(modJson.version));
                    ModVersion = modJson.version + " (+" + (modVersions.Count - 1) + ")";
                }
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
                    var imageFile = await KnUtils.GetImagePath(temp).ConfigureAwait(false);
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
