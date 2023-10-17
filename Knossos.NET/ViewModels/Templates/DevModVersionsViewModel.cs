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

        internal async void UploadMod()
        {
            /* 
             *  Pre-Upload Checks:
             *  NO-GO:
             *  1) At least one package must exist. --done
             *  2) One of the packages must be marked as requiered. --done
             *  3) If type = TC check it must have no mod dependencies of other mod parent or a TC --done
             *  4) If type = Engine check if the enviroment string is valid and the execs are accesible. --TODO
             *  5) If type = mod and parent is not "FS2" it must have a dependency pointing to a TC/Version mod. --done
             *  6) if type == mod or tc, it must have a FSO engine as dependency, and it must be already uploaded --TODO
             *  7) User must be logged in nebula. --done
             *  8) Check modid to see if it exist, and if does the user must have write permisions to it. --done
             *  
             *  WARNINGS:
             *  -(Always) Warn about the mod visibility before upload, for both private and public, and what it means. --done
             *  -if there is no tile image (for type != engine) or description. --done
             *  -Metadata update only if already uploaded --TODO, maybe i can use the new api call to get a mod version to get this early
             *  
             *  UPLOAD PROCESS:
             *  A) Do pre_flight API call, im guessing that if the mod version is already uploaded Nebula will report that here somehow. YES: "duplicated version"
             *  B) Upload mod tile image(check if already uploaded), get checksum and import it on modjson.
             *  C) Upload banner image and screenshoots(check if already uploaded), get checksum and import it on modjson.
             *  D)  1) If new mod do a create_mod api call, insert first release date (yyyy-mm-dd) and fields: id, title, type, parent, logo, tile, first_release
             *      2) If mod update do a update_mod api call, fields: id(REALLY?), title, logo, tile, first_release (for what?). 
             *         Im petty sure this step is a left behind of an initial implementation and it must NOT be done. ngld never told me about it. TODO: Confirm it! 
             *  E) If package = vp create a vp in mod\kn_upload\vps\{packagename}.vp (No Compression)
             *  F) 7z all packages folders and vp file and place them in kn_upload\{packagename}.7z
             *  G) Wipe and re-generate data in package.files and filelist. "files" is for the 7z file we are uploading to nebula. "filelist" is for all files inside the package folder (folder or vp)
             *  H) Use multipartuploader to upload all packages (will auto-skip if already uploaded)
             *  I) Api Call to "mod/release" with the mod meta (full json)
            */

            if (editor != null)
            {
                try
                {
                    if(!TaskViewModel.Instance!.IsSafeState())
                    {
                        await MessageBox.Show(MainWindow.instance!, "You must wait for other tasks to finish before uploading a new mod.", "Basic Check Fail", MessageBox.MessageBoxButtons.OK);
                        return;
                    }

                    var mod = editor.ActiveVersion;
                    if (mod != null)
                    {
                        //Packages exist? && One requiered Package
                        if (mod.packages == null || !mod.packages.Any() || mod.packages.FirstOrDefault(x => x.status == "required") == null)
                        {
                            await MessageBox.Show(MainWindow.instance!, "The mod must include at least one required package.", "Basic Check Fail", MessageBox.MessageBoxButtons.OK);
                            return;
                        }

                        //If type = TC check it must have no mod dependencies of other mod parent
                        if (mod.type == ModType.tc)
                        {
                            foreach(var pkg in mod.packages)
                            {
                                if(pkg.dependencies != null && pkg.dependencies.Any())
                                {
                                    foreach (var dep in pkg.dependencies)
                                    {
                                        var depMod = dep.SelectMod();
                                        if(depMod != null && (depMod.type == ModType.mod || depMod.type == ModType.tc) && depMod.parent != mod.id)
                                        {
                                            if (mod.type == ModType.mod)
                                            {
                                                await MessageBox.Show(MainWindow.instance!, "This mod depends on: " + depMod + ". That mod has a diferent parent mod: "
                                                    + depMod.parent + ". And it is not intended to be used with your TC mod: " + mod.id, "Basic Check Fail", MessageBox.MessageBoxButtons.OK);
                                            }
                                            if (mod.type == ModType.tc)
                                            {
                                                await MessageBox.Show(MainWindow.instance!, "This mod depends on: " + depMod + ". Thats a different TC mod. Your TC mod cant depend on another TC mod."
                                                    , "Basic Check Fail", MessageBox.MessageBoxButtons.OK);
                                            }
                                            return;
                                        }
                                    }
                                }
                            }
                        }

                        //If type = mod and parent is not "FS2" it must have a dependency pointing to a TC/Version mod.
                        if(mod.type == ModType.mod && mod.parent != "FS2")
                        {
                            var found = false;
                            foreach (var pkg in mod.packages)
                            {
                                if (pkg.dependencies != null && pkg.dependencies.Any())
                                {
                                    var result = pkg.dependencies.FirstOrDefault(x => x.SelectMod()?.type == ModType.tc); 
                                    if(result != null)
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                            }
                            if(!found)
                            {
                                await MessageBox.Show(MainWindow.instance!, "Mods for TC other than 'FS2' must also include a dependency to that TC mod.", "Basic Check Fail", MessageBox.MessageBoxButtons.OK);
                                return;
                            }
                        }

                        //If type = Engine check if the enviroment string is valid and the execs are accesible.
                        if(mod.type == ModType.engine)
                        {
                            //unimplemented
                        }

                        //If type == mod or tc, it must have a FSO engine as dependency, and it must be already uploaded
                        if(mod.type == ModType.mod || mod.type == ModType.tc)
                        {
                            //unimplemented
                        }

                        //User must be logged in nebula.
                        if(!await Nebula.Login())
                        {
                            await MessageBox.Show(MainWindow.instance!, "You must be logged in to nebula in order to upload mods.", "Basic Check Fail", MessageBox.MessageBoxButtons.OK);
                            return;
                        }

                        //Check modid to see if it exist, and if does the user must have write permisions to it.
                        var isNewMod = await Nebula.CheckIDAvalible(mod.id);
                        if (!isNewMod)
                        {
                            if(!await Nebula.IsModEditable(mod.id))
                            {
                                await MessageBox.Show(MainWindow.instance!, "You dont have write permissions to this mod.", "Basic Check Fail", MessageBox.MessageBoxButtons.OK);
                                return;
                            }
                        }

                        //Warnings
                        //if there is no tile image (for type != engine) or description.
                        if (string.IsNullOrEmpty(mod.description))
                        {
                            if(await MessageBox.Show(MainWindow.instance!, "Your mod does not include a description, it is recomended you set a description for users. " +
                                "This is only a warning and you can continue the upload if you want.", "Basic Check Warning", MessageBox.MessageBoxButtons.ContinueCancel) != MessageBox.MessageBoxResult.Continue)
                            { 
                                return;
                            }  
                        }
                        if (string.IsNullOrEmpty(mod.tile))
                        {
                            if (await MessageBox.Show(MainWindow.instance!, "Your mod does not include a tile image, it is recomended you set a tile image for users. " +
                                "This is only a warning and you can continue the upload if you want.", "Basic Check Warning", MessageBox.MessageBoxButtons.ContinueCancel) != MessageBox.MessageBoxResult.Continue)
                            {
                                return;
                            }
                        }

                        //Warn about the mod visibility before upload, for both private and public, and what it means.
                        if(mod.isPrivate)
                        {
                            if (await MessageBox.Show(MainWindow.instance!, "The mod version you are uploading is set to 'PRIVATE'. " +
                                "This means only the mod members will be able to see it. Do you want to continue?", "Basic Check Warning", MessageBox.MessageBoxButtons.ContinueCancel) != MessageBox.MessageBoxResult.Continue)
                            {
                                return;
                            }
                        }
                        else
                        {
                            if (await MessageBox.Show(MainWindow.instance!, "The mod version you are uploading is set to 'PUBLIC'. " +
                                "This means the mod will be avalible for everyone. Do you want to continue?", "Basic Check Warning", MessageBox.MessageBoxButtons.ContinueCancel) != MessageBox.MessageBoxResult.Continue)
                            {
                                return;
                            }
                        }

                        //Add release date if new mod and last update
                        if (mod.firstRelease == null)
                        {
                            mod.firstRelease = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString("00") + "-" + DateTime.Now.Day.ToString("00");
                        }
                        mod.lastUpdate = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString("00") + "-" + DateTime.Now.Day.ToString("00");

                        //Basic check finish, create task
                        TaskViewModel.Instance!.UploadModVersion(mod, isNewMod);
                    }
                }
                catch(Exception ex)
                {
                    Log.Add(Log.LogSeverity.Error, "DevModVersionsViewModel.UploadMod()", ex);
                }
            }
        }
    }
}
