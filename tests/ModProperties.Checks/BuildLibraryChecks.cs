using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Knossos.NET;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.ViewModels;
using Knossos.NET.Views;
using Knossos.NET.Views.Windows;
using System.Reflection;

static partial class Program
{
    static void BuildLibraryChecks(string output, string first, string second)
    {
        var playModel = new ModListViewModel();
        playModel.RefreshLibraryFilter();
        var play = new ModListView { DataContext = playModel };
        var host = new Window { Width = 850, Height = 450, Content = play };
        host.Show();
        Pump();
        var filterButton = play.FindControl<Button>("FilterFlyout")!;
        filterButton.Flyout!.ShowAt(filterButton);
        Pump();
        var filters = (WrapPanel)((Flyout)filterButton.Flyout).Content!;
        var library = filters.Children.OfType<Expander>().Last();
        var picker = (ComboBox)library.Content!;
        Check((string?)library.Header == "Library" && library.IsVisible && picker.ItemCount == 3 &&
              library.Bounds.Left >= filters.Children[1].Bounds.Right,
            "the Play flyout shows Library to the right of Sort by and Filters");
        picker.SelectedIndex = 2;
        Check(playModel.SelectedLibraryFilter?.Path == second, "the flyout selector updates the Play filter");
        filterButton.Flyout.Hide();

        var settings = Knossos.NET.Knossos.globalSettings;
        var originalMode = settings.singleViewMode;
        var originalLibraries = settings.libraryPaths;
        var originalMainView = MainView.instance;
        try
        {
            var message = string.Join(" ", Enumerable.Repeat(
                "Select the library where the FreeSpace Open engine packages should be downloaded and installed.", 18));
            foreach (var singleWindow in new[] { false, true })
            {
                settings.singleViewMode = singleWindow;
                host.Width = singleWindow ? 330 : 850;
                host.Height = singleWindow ? 240 : 450;
                if (singleWindow)
                    host.Content = new MainView();
                Pump();

                var pending = MessageBox.ShowWithLibrary(host, message, "Install FSO engine build",
                    singleWindow ? MessageBox.MessageBoxButtons.InstallBuildModes : MessageBox.MessageBoxButtons.YesNo);
                Pump();
                var dialog = singleWindow
                    ? host.GetVisualDescendants().OfType<MessageBox>().Single()
                    : (MessageBox)host.OwnedWindows.Single().Content!;
                var body = dialog.FindControl<TextBlock>("BodyText")!;
                var buttons = dialog.FindControl<WrapPanel>("ButtonsHostPanel")!;
                var librarySelector = dialog.FindControl<ComboBox>("LibrarySelector")!;
                Check(body.Bounds.Height > 100 && body.Bounds.Width < 520 &&
                      buttons.Children.OfType<Button>().All(b => b.Bounds.Right <= buttons.Bounds.Width + 1),
                    $"message wraps and all buttons fit in {(singleWindow ? "single-window overlay" : "native window")}");
                Check((string?)librarySelector.SelectedItem == first, "engine confirmation defaults to the first library");
                librarySelector.SelectedIndex = 1;
                var button = buttons.Children.OfType<Button>().Single(b =>
                    Equals(b.Content, singleWindow ? "Developer mode" : "Yes"));
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Pump(pending);
                Check(pending.Result == (singleWindow ? MessageBox.MessageBoxResult.InstallDeveloper : MessageBox.MessageBoxResult.Yes, second),
                    "engine confirmation returns both the action and the selected library");
            }

            settings.libraryPaths = new() { first };
            var singleLibrary = MessageBox.ShowWithLibrary(host, "Install this engine?", "Install FSO engine build",
                MessageBox.MessageBoxButtons.YesNo);
            Pump();
            var prompt = host.GetVisualDescendants().OfType<MessageBox>().Single();
            Check(!prompt.FindControl<ComboBox>("LibrarySelector")!.IsEffectivelyVisible,
                "the confirmation hides its library picker when there is only one library");
            prompt.FindControl<WrapPanel>("ButtonsHostPanel")!.Children.OfType<Button>()
                .Single(b => b.Content is "Yes").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Pump(singleLibrary);
            Check(singleLibrary.Result == (MessageBox.MessageBoxResult.Yes, first),
                "the only configured library is returned even when the picker is hidden");
        }
        finally
        {
            settings.singleViewMode = originalMode;
            settings.libraryPaths = originalLibraries;
            MainView.instance = originalMainView;
            host.Close();
        }

        var engine = Make("engine-library-test", "1.0.0", Pkg("engine", "required"));
        engine.type = ModType.engine;
        var package = engine.packages[0];
        var os = KnUtils.IsWindows ? "windows" : KnUtils.IsLinux ? "linux" : "macosx";
        var architecture = KnUtils.CpuArch == "X64" ? "x86_64" : KnUtils.CpuArch == "Arm64" ? "arm64" : "x86";
        package.environment = $"{os} && {architecture}";
        package.files = Array.Empty<ModFile>();
        package.executables = new() { new() { file = "fixture.exe", properties = FsoBuild.FillProperties(package.environment) } };
        var tasks = new TaskViewModel();
        var install = tasks.InstallBuild(new FsoBuild(engine), null, engine, libraryPath: second);
        Pump(install);
        var installed = install.Result!;
        var buildPath = Path.Combine(second, "bin", engine.id + "-" + engine.version);
        Check(Path.TrimEndingDirectorySeparator(installed.folderPath) == buildPath && File.Exists(Path.Combine(buildPath, "mod.json")),
            "the queued engine install writes the build in the selected library");

        var extra = new ModPackage { name = "extra", isSelected = false, filelist = new[] { new ModFilelist { filename = "extra.txt" } } };
        installed.modData!.packages.Add(extra);
        File.WriteAllText(Path.Combine(buildPath, "extra.txt"), "installed package");
        var modify = tasks.InstallBuild(installed, null, installed.modData, modifyPkgs: new() { extra });
        Pump(modify);
        Check(modify.Result != null && !File.Exists(Path.Combine(buildPath, "extra.txt")) &&
              Path.TrimEndingDirectorySeparator(modify.Result.folderPath) == buildPath &&
              !Directory.Exists(Path.Combine(first, "bin", engine.id + "-" + engine.version)),
            "modifying the build uses the installed library and leaves the default library untouched");

        var source = Directory.CreateDirectory(Path.Combine(output, "custom-build-source")).FullName;
        File.WriteAllText(Path.Combine(source, "fixture.exe"), "custom engine payload");
        var importer = new AddUserBuildViewModel
        {
            BuildName = "Custom engine", BuildVersion = "1.0.0", Release = "fixture.exe", X64 = true, Stage2 = true
        };
        var importerType = typeof(AddUserBuildViewModel);
        importerType.GetField("folderPath", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(importer, source);
        importerType.GetField("buildId", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(importer, "test-custom-library-engine");
        var importView = new AddUserBuildView { DataContext = importer };
        var importHost = new Window { Content = importView, Width = 540, Height = 650 };
        importHost.Show();
        Pump();
        var importPicker = importView.GetVisualDescendants().OfType<ComboBox>().Single();
        importPicker.SelectedIndex = 1;
        Check(importPicker.IsEffectivelyVisible && importer.BuildNewPath == Path.Combine(second, "bin", "test-custom-library-engine"),
            "custom build import updates its preview when the library changes");
        importView.GetVisualDescendants().OfType<Button>().Single(button => button.Content is "Add new build")
            .Command!.Execute(null);
        for (var deadline = DateTime.UtcNow.AddSeconds(4); importer.IsCopying && DateTime.UtcNow < deadline;)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }
        Pump();
        Check(Knossos.NET.Knossos.GetInstalledBuild("test-custom-library-engine", "1.0.0") != null &&
              File.ReadAllText(Path.Combine(importer.BuildNewPath, "fixture.exe")) == "custom engine payload",
            "custom build import copies and registers the executable in the selected library");
        importHost.Close();
    }
}
