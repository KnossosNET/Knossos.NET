using Avalonia.Controls;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Knossos.NET.ViewModels.DevModPkgMgrViewModel;

namespace Knossos.NET.ViewModels
{
    public partial class DevModPkgMgrViewModel : ViewModelBase
    {
        public partial class EditorDependencyItem : ObservableObject
        {
            public ModDependency Dependency { get; set; }

            [ObservableProperty]
            private int versionSelectedIndex = 0;

            [ObservableProperty]
            private int versionTypeIndex = 0;

            private int modSelectedIndex = 0;
            private int ModSelectedIndex
            {
                get
                {
                    return modSelectedIndex;
                }
                set
                {
                    if (modSelectedIndex != value)
                    {
                        modSelectedIndex = value;
                        VersionItems.Clear();
                        FillAllVersions();
                        VersionSelectedIndex = 1;
                    }
                }
            }


            private ObservableCollection<ComboBoxItem> VersionItems { get; set; } = new ObservableCollection<ComboBoxItem>();

            private ObservableCollection<ComboBoxItem> ModItems { get; set; } = new ObservableCollection<ComboBoxItem>();

            private EditorPackageItem EditorPackageItem { get; set; }

            public EditorDependencyItem(ModDependency dep, EditorPackageItem pkgItem)
            {
                Dependency = dep;
                EditorPackageItem = pkgItem;

                FillModList();

                var currentMod=ModItems.FirstOrDefault(x => x.Tag != null && x.Tag.ToString() == dep.id);
                if(currentMod != null)
                {
                    modSelectedIndex=ModItems.IndexOf(currentMod);
                }
                else
                {
                    var itemMod = new ComboBoxItem();
                    itemMod.Tag = dep.id;
                    itemMod.Content = dep.id;
                    ModItems.Add(itemMod);
                    modSelectedIndex = 0;
                
                }

                FillAllVersions();

                var currentVersion = VersionItems.FirstOrDefault(x => x.Content != null && dep.version != null && x.Content.ToString() == dep.version.Trim().Replace(">=","").Replace("~", ""));
                if (currentVersion != null)
                {
                    versionSelectedIndex = VersionItems.IndexOf(currentVersion);
                    if(dep.version!.Contains("~"))
                    {
                        versionTypeIndex = 2; 
                    }
                    else
                    {
                        if (dep.version!.Contains(">="))
                        {
                            versionTypeIndex = 1;
                        }
                        else
                        {
                            versionTypeIndex = 0;
                        }
                    }
                }
                else
                {
                    VersionSelectedIndex = 0;
                }
            }

            private void FillModList()
            {
                var mods = Knossos.GetInstalledModList(null);
                var builds = Knossos.GetInstalledBuildsList(null);
                var usedIds = new List<string>();

                var separator = new ComboBoxItem();
                separator.Content = "--- Mods ---";
                separator.IsEnabled = false;
                separator.Tag = "-1";
                ModItems.Add(separator);

                foreach (var mod in mods)
                {
                    if (usedIds.IndexOf(mod.id) == -1 )
                    {
                        var itemMod = new ComboBoxItem();
                        itemMod.Content = mod.title + "  [ "+ mod.id+" ]";
                        itemMod.Tag = mod.id;
                        ModItems.Add(itemMod);
                        usedIds.Add(mod.id);
                    }
                }

                separator = new ComboBoxItem();
                separator.Tag = "-1";
                separator.Content = "--- Engine Builds ---";
                separator.IsEnabled = false;
                ModItems.Add(separator);

                foreach (var build in builds)
                {
                    if (usedIds.IndexOf(build.id) == -1 )
                    {
                        var itemMod = new ComboBoxItem();
                        itemMod.Content = build.title + "  [ " + build.id + " ]";
                        itemMod.Tag = build.id;
                        ModItems.Add(itemMod);
                        usedIds.Add(build.id);
                    }
                }
            }

            private void FillAllVersions()
            {
                if (ModItems.Count > 0)
                {
                    var id = ModItems[ModSelectedIndex].Tag!.ToString();
                    var mods = Knossos.GetInstalledModList(id);

                    var anyVer = new ComboBoxItem();
                    anyVer.Content = "Any";
                    VersionItems.Add(anyVer);


                    if (mods.Any())
                    {
                        mods.Reverse();
                        foreach (var mod in mods)
                        {
                            var itemVer = new ComboBoxItem();
                            itemVer.Content = mod.version;
                            VersionItems.Add(itemVer);
                        }
                    }
                    else
                    {
                        var builds = Knossos.GetInstalledBuildsList(id);
                        if (builds.Any())
                        {
                            builds.Reverse();
                            foreach (var build in builds)
                            {
                                var itemVer = new ComboBoxItem();
                                itemVer.Content = build.version;
                                VersionItems.Add(itemVer);
                            }
                        }
                    }
                }
            }
        }

        public partial class EditorPackageItem : ObservableObject
        {
            public ModPackage Package { get; set; }

            [ObservableProperty]
            private ObservableCollection<EditorDependencyItem> dependencyItems = new ObservableCollection<EditorDependencyItem>();

            private DevModPkgMgrViewModel PkgMgr { get; set; }

            public EditorPackageItem(ModPackage pkg, DevModPkgMgrViewModel pkgmgr) 
            {
                Package = pkg;
                PkgMgr = pkgmgr;
                if(pkg.dependencies != null)
                {
                    foreach(var dep in pkg.dependencies)
                    {
                        DependencyItems.Add(new EditorDependencyItem(dep, this));
                    }
                }
            }
        }

        public class EditorFlagItem
        {
            public string Flag { get; set; }

            public string FlagName { get; set; }

            private DevModPkgMgrViewModel PkgMgr { get; set; }

            bool IsThisMod { get; set; }

            public EditorFlagItem(string flag, DevModPkgMgrViewModel pkgmgr, bool thisMod)
            {
                Flag = flag;
                PkgMgr = pkgmgr;
                IsThisMod = thisMod;
                var mod = Knossos.GetInstalledModList(flag);
                if(mod.Any())
                {
                    FlagName = mod[0].title + "  [ " + flag + " ]";
                }
                else
                {
                    var build = Knossos.GetInstalledBuildsList(flag);
                    if (build.Any())
                    {
                        FlagName = build[0].title + "  [ " + flag + " ]";
                    }
                    else
                    {
                        FlagName = flag;
                    }
                }
            }

            internal void FlagUP()
            {
                PkgMgr.FlagUP(this);
            }
            internal void FlagDown()
            {
                PkgMgr.FlagDown(this);
            }
        }

        private DevModEditorViewModel? editor;
        [ObservableProperty]
        private ObservableCollection<EditorFlagItem> editorFlagItems = new ObservableCollection<EditorFlagItem>();
        [ObservableProperty]
        private ObservableCollection<EditorPackageItem> editorPackageItems = new ObservableCollection<EditorPackageItem>();

        public DevModPkgMgrViewModel()
        {
        }

        public DevModPkgMgrViewModel(DevModEditorViewModel editor)
        {
            this.editor = editor;
            foreach (var item in editor.ActiveVersion.modFlag)
            {
                EditorFlagItems.Add(new EditorFlagItem(item, this, item == editor.ActiveVersion.id));
            }

            foreach (var item in editor.ActiveVersion.packages)
            {
                EditorPackageItems.Add(new EditorPackageItem(item, this));
            }
        }

        /* Button Commands */
        internal void FlagUP(EditorFlagItem flagItem)
        {
            int index = EditorFlagItems.IndexOf(flagItem);
            if (index > 0)
            {
                EditorFlagItems.Move(index, index - 1);
            }
        }

        internal void FlagDown(EditorFlagItem flagItem)
        {
            int index = EditorFlagItems.IndexOf(flagItem);
            if (index + 1  < EditorFlagItems.Count())
            {
                EditorFlagItems.Move(index, index + 1);
            }
        }
    }
}
