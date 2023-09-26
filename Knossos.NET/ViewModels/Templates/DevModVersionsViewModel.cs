using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
    public partial class DevModVersionsViewModel : ViewModelBase
    {
        [ObservableProperty]
        private List<Mod> mods = new List<Mod>();

        private int selectedIndex = -1;
        private int SelectedIndex
        {
            get
            {
                return selectedIndex;
            }
            set
            {
                if (selectedIndex != value)
                {
                    selectedIndex = value;
                    newVersion = string.Empty;
                    if (selectedIndex < Mods.Count && selectedIndex >= 0 && editor != null)
                    {
                        editor.ChangeActiveVersion(Mods[value]);
                    }
                }
            }
        }

        private DevModEditorViewModel? editor;

        private string CurrentVersion 
        {
            get 
            {
                if (editor != null)
                    return editor.ActiveVersion.version;
                else
                    return "1.0.0-Default";
            }
        }

        private string newVersion = string.Empty;
        private string NewVersion
        {
            get 
            {
                if (newVersion != string.Empty)
                    return newVersion;
                else
                    return CurrentVersion;
            }
            set
            {
                if(newVersion != value)
                {
                    newVersion = value;
                }
            }
        }


        public DevModVersionsViewModel() 
        {
        }

        public DevModVersionsViewModel(DevModEditorViewModel editor)
        {
            this.editor = editor;
            mods = editor.GetModList();
            foreach(var m in mods)
            {
                if(editor.ActiveVersion == m)
                {
                    selectedIndex = mods.IndexOf(m);
                }
            }
        }

        private bool VerifyNewVersion()
        {
            //Verify Version String
            try
            {
                var sm = new SemanticVersion(NewVersion);
                if (sm != null)
                {
                    if (sm.ToString() == "0.0.0")
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "DevModVersionsViewModel.VerifyNewVersion()", ex);
                return false;
            }
            return true;
        }

        /* Button Commands */

        internal async void LoadVersionsFromNebula()
        {
            try
            {
                if (editor != null)
                {
                    var dialog = new ModInstallView();
                    dialog.DataContext = new ModInstallViewModel(editor.ActiveVersion, editor.ActiveVersion.version, true);
                    await dialog.ShowDialog<ModInstallView?>(MainWindow.instance!);
                }
            }
            catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "DevModVersionsViewModel.LoadVersionsFromNebula()", ex);
            }
        }

        internal async void CreateNewVersion(object control) 
        {
            if (editor == null)
                return;
            //Validate new version string
            if(!VerifyNewVersion())
            {
                await MessageBox.Show(MainWindow.instance!, "'" + NewVersion + "' is not a valid semantic version.", "Validation error", MessageBox.MessageBoxButtons.OK);
                return;
            }
            if (Mods.FirstOrDefault(m => m.version == NewVersion) != null)
            {
                await MessageBox.Show(MainWindow.instance!, "'" + NewVersion + "' already exist.", "Validation error", MessageBox.MessageBoxButtons.OK);
                return;
            }
            var parentDir = new DirectoryInfo(editor.ActiveVersion.fullPath).Parent;
            if(parentDir == null)
            {
                Log.Add(Log.LogSeverity.Error, "DevModVersionsViewModel.CreateNewVersion()", editor.ActiveVersion.fullPath+" get parent folder was null");
                return;
            }
            var newDir = parentDir.FullName + Path.DirectorySeparatorChar + editor.ActiveVersion.id + "-" + NewVersion;
            if (Directory.Exists(newDir))
            {
                await MessageBox.Show(MainWindow.instance!, "The directory '"+ newDir + "' already exist.", "Validation error", MessageBox.MessageBoxButtons.OK);
                return;
            }

            //Version string should be valid
            var button = (Button)control;
            if (button.Flyout != null)
                button.Flyout.Hide();

            TaskViewModel.Instance!.CreateModVersion(editor.ActiveVersion, NewVersion);
        }
    }
}
