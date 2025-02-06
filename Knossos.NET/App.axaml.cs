using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.ViewModels;
using Knossos.NET.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Knossos.NET
{
    public partial class App : Application
    {
        TrayIcon? trayIcon = null;
        bool minimizeToTray = false;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void DisableMinimizeToTrayRuntime()
        {
            minimizeToTray = false;
        }

        public void EnableMinimizeToTrayRuntime()
        {
            if (!minimizeToTray && ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow != null)
                {
                    desktop.MainWindow.PropertyChanged += (v, __) =>
                    {
                        if (minimizeToTray && v is MainWindow view && view.WindowState == WindowState.Minimized)
                        {
                            desktop.MainWindow.Hide();
                            desktop.MainWindow.WindowState = WindowState.Normal;
                            StartTrayIcon();
                            trayIcon!.IsVisible = true;
                        }
                    };
                    desktop.MainWindow.Closing += (_, __) =>
                    {
                        if (trayIcon != null)
                        {
                            trayIcon.IsVisible = false;
                            trayIcon = null;
                        }
                    };
                    minimizeToTray = true;
                }
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                minimizeToTray = Environment.GetCommandLineArgs().FirstOrDefault(x => x.ToLower() == "-traymode") != null;
                if (minimizeToTray)
                {
                    desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    Knossos.StartUp(false, false);
                    StartTrayIcon();
                }
                else
                {
                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = new MainWindowViewModel()
                    };
                }
            }

            base.OnFrameworkInitializationCompleted();
        }


        private async void StartTrayIcon()
        {
            if (trayIcon == null)
            {
                trayIcon = new TrayIcon
                {
                    IsVisible = true,
                    ToolTipText = "Knossos.NET v" + Knossos.AppVersion,
                    Icon = new WindowIcon(new Bitmap(AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/knossos-icon.ico")))),
                    Menu = new NativeMenu() { new NativeMenuItem("Loading...") }
                };

                while (!Knossos.initIsComplete) { await Task.Delay(10); }
            }

            trayIcon.Menu = new NativeMenu();

            /*****************************OPEN***********************************/
            var open = new NativeMenuItem("Open");
            open.Click += (s, _) => {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    if (desktop.MainWindow == null)
                    {
                        desktop.MainWindow = new MainWindow
                        {
                            DataContext = new MainWindowViewModel()
                        };
                        desktop.MainWindow.PropertyChanged += (v, __) =>
                        {
                            if (v is MainWindow view && view.WindowState == WindowState.Minimized)
                            {
                                desktop.MainWindow.Hide();
                                desktop.MainWindow.WindowState = WindowState.Normal;
                                StartTrayIcon();
                                trayIcon.IsVisible = true;
                            }
                        };
                        desktop.MainWindow.Closing += (_, __) =>
                        {
                            if (trayIcon != null)
                            {
                                trayIcon.IsVisible = false;
                                trayIcon = null;
                            }
                        };
                        desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
                    }
                    desktop.MainWindow?.Show();
                    trayIcon.IsVisible = false;
                    GC.Collect();
                }
            };
            trayIcon.Menu.Add(open);
            trayIcon.Menu.Add(new NativeMenuItemSeparator());

            try
            {
                /*****************************PLAY***********************************/
                var play = new NativeMenuItem("Play") { Menu = new NativeMenu(), Icon = new Bitmap(AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/general/menu_play.png"))) };
                var mods = Knossos.GetInstalledModList(null);
                mods.Sort(Mod.CompareTitles);
                var filters = ModTags.GetListAllFilters();
                foreach (var filter in filters)
                {
                    TextInfo myTI = new CultureInfo("en-US", false).TextInfo;
                    var displayName = myTI.ToTitleCase(filter.Replace("_", " "));
                    var filterItem = new NativeMenuItem(displayName) { Menu = new NativeMenu() };
                    var modsInFilter = mods.Where(x => ModTags.IsFilterPresentInModID(x.id, filter));
                    if (modsInFilter.Any())
                    {
                        var addedIds = new List<string>();
                        foreach (var mod in modsInFilter)
                        {
                            if (!addedIds.Contains(mod.id))
                            {
                                var m = mods.Where(x => x.id == mod.id)?.MaxBy(x => new SemanticVersion(x.version));
                                if (m != null)
                                {
                                    var modItem = new NativeMenuItem(m.ToString()) { Menu = new NativeMenu() };
                                    /*************************************MOD SUB BUTTONS*************************************/
                                    modItem.Menu.Add(CreateLaunchFSOButton(m, FsoExecType.Release));
                                    modItem.Menu.Add(CreateLaunchFSOButton(m, FsoExecType.Debug));
                                    modItem.Menu.Add(CreateLaunchFSOButton(m, FsoExecType.Fred2));
                                    modItem.Menu.Add(CreateLaunchFSOButton(m, FsoExecType.Fred2Debug));
                                    modItem.Menu.Add(CreateLaunchFSOButton(m, FsoExecType.QtFred));
                                    modItem.Menu.Add(CreateLaunchFSOButton(m, FsoExecType.QtFredDebug));
                                    var settings = new NativeMenuItem("Settings");
                                    settings.Click += (s, e) => {
                                        var dialog = new ModSettingsView();
                                        dialog.DataContext = new ModSettingsViewModel(m);
                                        dialog.Show();
                                    };
                                    modItem.Menu.Add(settings);
                                    /*****************************************************************************************/
                                    filterItem.Menu.Add(modItem);
                                    addedIds.Add(mod.id);
                                }
                            }
                        }
                        play.Menu.Add(filterItem);
                    }
                }
                if(play.Menu.Any())
                    trayIcon.Menu.Add(play);
                /*****************************DEVELOP***********************************/
                var dev = new NativeMenuItem("Develop") { Menu = new NativeMenu(), Icon = new Bitmap(AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/general/menu_develop.png"))) };
                var devMods = mods.Where(x => x.devMode);
                if(devMods != null && devMods.Any())
                {
                    var addedIds = new List<string>();
                    foreach (var devMod in devMods)
                    {
                        if (!addedIds.Contains(devMod.id))
                        {
                            var m = mods.Where(x => x.id == devMod.id)?.MaxBy(x => new SemanticVersion(x.version));
                            if(m != null)
                            {
                                var modItem = new NativeMenuItem(m.ToString()) { Menu = new NativeMenu() };
                                /*************************************MOD SUB BUTTONS*************************************/
                                modItem.Menu.Add(CreateLaunchFSOButton(m, FsoExecType.Release));
                                modItem.Menu.Add(CreateLaunchFSOButton(m, FsoExecType.Debug));
                                modItem.Menu.Add(CreateLaunchFSOButton(m, FsoExecType.Fred2));
                                modItem.Menu.Add(CreateLaunchFSOButton(m, FsoExecType.Fred2Debug));
                                modItem.Menu.Add(CreateLaunchFSOButton(m, FsoExecType.QtFred));
                                modItem.Menu.Add(CreateLaunchFSOButton(m, FsoExecType.QtFredDebug));
                                var settings = new NativeMenuItem("Settings");
                                settings.Click += (s, e) => {
                                    var dialog = new ModSettingsView();
                                    dialog.DataContext = new ModSettingsViewModel(m);
                                    dialog.Show();
                                };
                                modItem.Menu.Add(settings);
                                /*****************************************************************************************/
                                dev.Menu.Add(modItem);
                                addedIds.Add(devMod.id);
                            }
                        }
                    }
                }    
                if(dev.Menu.Any())
                    trayIcon.Menu.Add(dev);
                /*****************************TOOLS*************************************/
                var toolsItem = new NativeMenuItem("Tools") { Menu = new NativeMenu(), Icon = new Bitmap(AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/general/custom-config-icon.png"))) };
                var tools = Knossos.GetTools();
                if (tools != null && tools.Any())
                {
                    foreach (var tool in tools)
                    {
                        var tItem = new NativeMenuItem(tool.name);
                        tItem.Click += (s, e) => {
                            if(tool.isInstalled)
                                tool?.Open();
                        };
                        toolsItem.Menu.Add(tItem);
                    }
                }
                if (toolsItem.Menu.Any())
                    trayIcon.Menu.Add(toolsItem);
                /*****************************DEBUG*************************************/
                var debug = new NativeMenuItem("Debug") { Menu = new NativeMenu(), Icon = new Bitmap(AssetLoader.Open(new Uri("avares://Knossos.NET/Assets/general/menu_debug.png"))) };
                var openFs2Log = new NativeMenuItem("Open fs2_open.log");
                openFs2Log.Click += (s, e) => {
                    OpenFS2Log();
                };
                debug.Menu.Add(openFs2Log);
                var openLog = new NativeMenuItem("Open knossos.log");
                openLog.Click += (s, e) => {
                    OpenLog();
                };
                debug.Menu.Add(openLog);
                trayIcon.Menu.Add(debug);
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "App.StartTrayIconApp()", ex);
            }

            /*****************************CLOSE***********************************/
            trayIcon.Menu.Add(new NativeMenuItemSeparator());
            var close = new NativeMenuItem("Exit Knossos.NET");
            close.Click += (s, e) => { 
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
                if(trayIcon != null)
                {
                    trayIcon.IsVisible = false;
                    trayIcon = null;
                }
            };
            trayIcon.Menu.Add(close);
            GC.Collect();
        }

        private NativeMenuItem CreateLaunchFSOButton(Mod mod, FsoExecType fsoExecType)
        {
            var item = new NativeMenuItem(fsoExecType.ToString());
            item.Click += (s, e) => {
                Knossos.PlayMod(mod, fsoExecType);
            };
            return item;
        }

        private void OpenLog()
        {
            if (File.Exists(KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "Knossos.log"))
            {
                try
                {
                    var cmd = new Process();
                    cmd.StartInfo.FileName = KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "Knossos.log";
                    cmd.StartInfo.UseShellExecute = true;
                    cmd.Start();
                    cmd.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Add(Log.LogSeverity.Error, "App.OpenLog", ex);
                }
            }
            else
            {
                if (MainWindow.instance != null)
                    MessageBox.Show(MainWindow.instance, "Log File " + KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "Knossos.log not found.", "File not found", MessageBox.MessageBoxButtons.OK);
            }
        }

        private void OpenFS2Log()
        {
            if (File.Exists(KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar + "fs2_open.log"))
            {
                try
                {
                    var cmd = new Process();
                    cmd.StartInfo.FileName = KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar + "fs2_open.log";
                    cmd.StartInfo.UseShellExecute = true;
                    cmd.Start();
                    cmd.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Add(Log.LogSeverity.Error, "App.OpenFS2Log", ex);
                }
            }
            else
            {
                if (MainWindow.instance != null)
                    MessageBox.Show(MainWindow.instance, "Log File " + KnUtils.GetFSODataFolderPath() + Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar + "fs2_open.log not found.", "File not found", MessageBox.MessageBoxButtons.OK);
            }
        }
    }
}
