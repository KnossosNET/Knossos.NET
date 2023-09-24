using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
    public partial class DevModEditorViewModel : ViewModelBase
    {
        public DevModEditorViewModel() 
        {

        }

        [ObservableProperty]
        public DevModVersionsViewModel? versionsView;
        [ObservableProperty]
        public DevModPkgMgrViewModel? pkgMgrView;

        [ObservableProperty]
        public string name = string.Empty;
        [ObservableProperty]
        public string version = string.Empty;
        [ObservableProperty]
        public bool isEngineBuild = false;

        [ObservableProperty]
        private Bitmap? image;

        private List<Mod> mods = new List<Mod>();
        private int index = 0;

        public Mod ActiveVersion
        {
            get
            {
                return mods[index];
            }
        }

        public List<Mod> GetModList()
        {
            return mods;
        }

        public void AddModToList(Mod newMod)
        {
            var currentActive = ActiveVersion;
            mods.Add(newMod);
            mods.Sort(Mod.CompareVersion);
            ChangeActiveVersion(currentActive);
        }

        public void ChangeActiveVersion(Mod mod)
        {
            if (mod != ActiveVersion)
            {
                index = mods.IndexOf(mod);
                LoadActiveVersion();
            }
        }

        public void StartModEditor(Mod mod)
        {
            try
            {
                //Clean old data
                Name = Version = string.Empty;
                mods.Clear();
                Image?.Dispose();
                VersionsView = null;
                PkgMgrView = null;
                IsEngineBuild = false;
                Image = new Bitmap(AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/general/NebulaDefault.png")));
                index = 0;
                //Get all installed mods with this ID
                switch (mod.type)
                {
                    case ModType.tc:
                    case ModType.mod:
                        mods = Knossos.GetInstalledModList(mod.id);
                        break;
                    case ModType.engine:
                        var builds = Knossos.GetInstalledBuildsList(mod.id);
                        IsEngineBuild = true;
                        break;
                    case ModType.tool:
                        break;
                }
                //Sort by version
                mods.Sort(Mod.CompareVersion);
                //Filter and determine best active version
                foreach (var m in mods.ToList())
                {
                    if (m.modSource != ModSource.nebula)
                    {
                        mods.Remove(m);
                    }
                    else
                    {
                        if (SemanticVersion.Compare(ActiveVersion.version, m.version) < 1)
                        {
                            ChangeActiveVersion(m);
                        }
                    }
                }
                //Load Mod to UI
                LoadActiveVersion();
            }
            catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "DevModEditorViewModel.StartModEditor()", ex);
            }
        }

        private void LoadActiveVersion()
        {
            try
            {
                var mod = ActiveVersion;
                Name = mod.title;
                Version = mod.version;
                if (!string.IsNullOrEmpty(mod.tile) && File.Exists(mod.fullPath + Path.DirectorySeparatorChar + mod.tile))
                {
                    Image?.Dispose();
                    Image = new Bitmap(mod.fullPath + Path.DirectorySeparatorChar + mod.tile);
                }
                //Templated Elements
                PkgMgrView = new DevModPkgMgrViewModel(this);
                VersionsView = new DevModVersionsViewModel(this);
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "DevModEditorViewModel.LoadMod()", ex);
            }
        }

        /* Button Commands */
        internal void PlayMod(object type)
        {
            try
            {
                switch ((string)type)
                {
                    case "release": Knossos.PlayMod(ActiveVersion,FsoExecType.Release); break;
                    case "fred2": Knossos.PlayMod(ActiveVersion, FsoExecType.Fred2); break;
                    case "qtfred": Knossos.PlayMod(ActiveVersion, FsoExecType.QtFred); break;
                    case "debug": Knossos.PlayMod(ActiveVersion, FsoExecType.Debug); break;
                    case "fred2debug": Knossos.PlayMod(ActiveVersion, FsoExecType.Fred2Debug); break;
                }
            }catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "DevModEditorViewModel.PlayMod()", ex);
            }
        }

        internal void OpenTool()
        {
            try
            {
            
            }catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "DevModEditorViewModel.OpenTool()", ex);
            }
        }

        internal void OpenFolder()
        {
            try
            {
                SysInfo.OpenFolder(ActiveVersion.fullPath);
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "DevModEditorViewModel.OpenFolder()", ex);
            }
        }
    }
}
