using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.VisualTree;
using Knossos.NET;
using Knossos.NET.Classes;
using Knossos.NET.Models;
using Knossos.NET.ViewModels;
using Knossos.NET.Views;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using VP.NET;

static partial class Program
{
    static void LocalFilesChecks(string output)
    {
        var library = Path.Combine(output, "local-library");
        var savedLibrary = Knossos.NET.Knossos.globalSettings.basePath;
        var savedCompression = Knossos.NET.Knossos.globalSettings.modCompression;
        Knossos.NET.Knossos.globalSettings.basePath = library;
        Knossos.NET.Knossos.globalSettings.modCompression = CompressionSettings.Manual;
        var tasks = new TaskViewModel();
        var queue = (Queue<TaskItemViewModel>)typeof(TaskViewModel).GetProperty("taskQueue",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(tasks)!;
        var taskList = (System.Collections.ObjectModel.ObservableCollection<TaskItemViewModel>)typeof(TaskViewModel)
            .GetProperty("TaskList", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(tasks)!;

        var active = Make("local-files", "2.0.0", Pkg("core", "required"));
        var old = Make(active.id, "1.0.0", Pkg("core", "required"));
        active.title = old.title = "Blue Planet Complete";
        var catalog = new Catalog { UseActualInstalled = true, CanDevelop = false };
        foreach (var mod in new[] { active, old })
        {
            mod.fullPath = Path.Combine(library, mod.id, mod.id + "-" + mod.version);
            mod.installed = true;
            mod.packages[0].files = Array.Empty<ModFile>();
            Directory.CreateDirectory(Path.Combine(mod.fullPath, "data"));
            catalog.Available.Add(mod);
            Knossos.NET.Knossos.AddMod(mod);
        }

        try
        {
            var data = Path.Combine(active.fullPath, "data", "example.tbl");
            var previous = Path.Combine(old.fullPath, "data", "example.tbl");
            var bytes = new byte[2_146_393];
            Random.Shared.NextBytes(bytes.AsSpan(0, 4096));
            File.WriteAllBytes(data, bytes);
            var shared = HardLink.CreateFileLink(data, previous);
            if (!shared) File.Copy(data, previous);
            var vpPath = Path.Combine(active.fullPath, "assets.vp");
            var vp = new VPContainer();
            vp.AddFolderToRoot(active.fullPath);
            Pump(vp.SaveAsAsync(vpPath));
            var originalVp = File.ReadAllBytes(vpPath);
            active.packages[0].filelist = new[]
            {
                new ModFilelist { filename = "data/example.tbl", checksum = new[] { "sha256", Convert.ToHexString(SHA256.HashData(bytes)) } },
                new ModFilelist { filename = "assets.vp", checksum = new[] { "sha256", Convert.ToHexString(SHA256.HashData(originalVp)) } }
            };
            active.SaveJson();
            old.SaveJson();
            long Size(Mod mod) => Directory.GetFiles(mod.fullPath, "*", SearchOption.AllDirectories).Sum(file => new FileInfo(file).Length);

            var host = new Window { Width = 1600, Height = 960 };
            host.Show();
            var firstView = new ModPropertiesView();
            var owner = ModPropertiesViewModel.GetOrCreate(active, firstView, ModPropertiesViewModel.MenuPageEntry.Files, catalog: catalog);
            firstView.DataContext = owner;
            firstView.Show(host);
            Pump(owner.Files.CurrentMeasurement);
            Check(owner.IsFilesSelected && owner.Files.LibraryLocation == library && owner.Files.FreeSpace != "—", "Local Files shows the configured library and its free space");
            Check(owner.Files.ActiveSize == KnUtils.FormatBytes(Size(active)) && owner.Files.AllVersionsSize == "—", "active version size is automatic and total size is on demand");
            Pump(owner.Files.MeasureAllCommand.ExecuteAsync(null));
            Check(owner.Files.AllVersionsSize == KnUtils.FormatBytes(Size(active) + Size(old)), "all installed versions are measured and shared paths count in each version");
            var page = firstView.GetVisualDescendants().OfType<ModLocalFilesView>().Single();
            var window = (Window)TopLevel.GetTopLevel(firstView)!;
            window.Width = 1600; window.Height = 900; Pump();
            window.CaptureRenderedFrame()!.Save(Path.Combine(output, "local-files-desktop.png"));
            window.Width = 390; window.Height = 844; Pump();
            window.CaptureRenderedFrame()!.Save(Path.Combine(output, "local-files-phone.png"));
            Check(page.FindControl<StackPanel>("PageLayout")!.Bounds.Width <= page.Bounds.Width, "Local Files fits a narrow phone viewport");

            Task ChangeCompression(ModPropertiesViewModel model, CompressionScope scope)
            {
                var change = model.Files.ChangeCompressionCommand.ExecuteAsync(null);
                Pump();
                Check(model.IsCompressionScopeModal && model.ActiveCompressionVersionLabel.Contains(active.version),
                    "compression scope names the active version when several are installed");
                model.SelectCompressionScopeCommand.Execute(scope);
                return change;
            }

            var dismiss = owner.Files.ChangeCompressionCommand.ExecuteAsync(null);
            Pump();
            owner.CloseModalCommand.Execute(null);
            Pump(dismiss);
            Check(queue.Count == 0 && !owner.Versions.IsApplying, "dismissing the scope dialog queues no operation");

            var blocker = new TaskItemViewModel();
            queue.Enqueue(blocker);
            var cancelled = ChangeCompression(owner, CompressionScope.Active);
            Pump();
            var activeTask = owner.Versions.ActiveTask;
            Check(activeTask?.Info == "In Queue" && owner.Versions.IsApplying, "Local Files adds compression to the shared task queue");
            firstView.Close();
            Pump();
            var reopened = new ModPropertiesView();
            var resumed = ModPropertiesViewModel.GetOrCreate(active, reopened, ModPropertiesViewModel.MenuPageEntry.Versions, catalog: catalog);
            reopened.DataContext = resumed;
            reopened.Show(host);
            Pump(resumed.Versions.CurrentLoad);
            Check(ReferenceEquals(owner, resumed) && ReferenceEquals(activeTask, resumed.Versions.ActiveTask) &&
                reopened.GetVisualDescendants().OfType<ModVersionsView>().Single().GetVisualDescendants()
                    .OfType<TextBlock>().Any(text => text.Text == activeTask!.Name),
                "reopening in Versions displays the same queued compression task");
            resumed.Versions.CancelOperationCommand.Execute(null);
            Pump(cancelled);
            Check(queue.Count == 1 && ReferenceEquals(queue.Peek(), blocker) && !resumed.Versions.IsApplying && File.Exists(data),
                "cancelling queued compression removes only that request");
            queue.Dequeue();

            resumed.NavigateCommand.Execute(ModPropertiesViewModel.MenuPageEntry.Files);
            Pump(ChangeCompression(resumed, CompressionScope.Active));
            Pump(resumed.Files.CurrentMeasurement);
            var loose = data + ".lz41";
            var archive = vpPath + "c";
            Check(resumed.Files.IsCompressed && File.Exists(loose) && File.Exists(archive) && !File.Exists(data),
                "compression updates the active installation and creates both supported compressed formats");
            Check(File.ReadAllBytes(previous).SequenceEqual(bytes), "compressing the active version preserves the older " + (shared ? "hardlinked" : "copied") + " file");
            var checkedOk = tasks.VerifyModAsync(Copy(active));
            Pump(checkedOk);
            Check(checkedOk.Result && File.Exists(archive) && File.Exists(loose) && !File.Exists(vpPath) && !File.Exists(data),
                "verification hashes compressed loose files and reconstructed VPs without unpacking to disk");
            var corrupt = File.ReadAllBytes(loose);
            File.WriteAllBytes(loose, corrupt[..^2]);
            var bad = tasks.VerifyModAsync(Copy(active));
            Pump(bad);
            Check(!bad.Result && tasks.IsSafeState() && File.Exists(loose), "truncated LZ41 fails verification and releases the queue");
            // Serve a real repair archive from memory through the normal downloader.
            using var zipBytes = new MemoryStream();
            using (var zip = new ZipArchive(zipBytes, ZipArchiveMode.Create, leaveOpen: true))
            using (var entry = zip.CreateEntry("data/example.tbl").Open())
                entry.Write(bytes);
            var payload = zipBytes.ToArray();
            active.packages[0].files = new[] { new ModFile
            {
                filename = "repair.zip", dest = "",
                urls = new[] { "https://example.invalid/repair.zip" },
                checksum = new[] { "sha256", Convert.ToHexString(SHA256.HashData(payload)) }
            } };
            active.SaveJson();
            var clientFactory = typeof(KnUtils).GetField("httpClientFactory",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
            var previousFactory = clientFactory.GetValue(null);
            try
            {
                clientFactory.SetValue(null, new LocalRepairClientFactory(payload));
                var repair = tasks.VerifyModAsync(Copy(active));
                Pump(repair);
                Check(repair.Result && active.modSettings.isCompressed && File.Exists(loose) && !File.Exists(data) &&
                    File.ReadAllBytes(previous).SequenceEqual(bytes), "automatic repair replaces the corrupt file and keeps the version compressed");
                Check(!Directory.GetDirectories(active.fullPath, ".repair-*").Any(), "repair cleans its staging directory");
                var repairedCheck = tasks.VerifyModAsync(Copy(active));
                Pump(repairedCheck);
                Check(repairedCheck.Result && tasks.IsSafeState(), "repaired compressed files verify again");
            }
            finally { clientFactory.SetValue(null, previousFactory); }
            active.packages[0].files = Array.Empty<ModFile>();
            active.SaveJson();

            var originalArchive = File.ReadAllBytes(archive);
            File.WriteAllBytes(archive, originalArchive[..^1]);
            bad = tasks.VerifyModAsync(Copy(active));
            Pump(bad);
            Check(!bad.Result && tasks.IsSafeState() && File.Exists(archive), "truncated VPC fails verification and releases the queue");
            File.WriteAllBytes(archive, originalArchive);
            var baselineTasks = taskList.Count;
            Pump(ChangeCompression(resumed, CompressionScope.All));
            Pump(resumed.Files.CurrentMeasurement);
            Check(taskList.Skip(baselineTasks).Count(task => task.Name.StartsWith("Decompressing mod: ")) == 1 &&
                !old.modSettings.isCompressed, "decompressing all skips the version that is already decompressed");
            Check(!resumed.Files.IsCompressed && !File.Exists(loose) && !File.Exists(archive) &&
                File.ReadAllBytes(data).SequenceEqual(bytes) && File.ReadAllBytes(vpPath).SequenceEqual(originalVp),
                "decompression restores original loose files and VP bytes");

            File.WriteAllBytes(loose, corrupt);
            File.Delete(data);
            resumed.NavigateCommand.Execute(ModPropertiesViewModel.MenuPageEntry.Information);
            resumed.NavigateCommand.Execute(ModPropertiesViewModel.MenuPageEntry.Files);
            Pump(resumed.Files.CurrentMeasurement);
            Check(resumed.Files.CompressionLabel == "Decompress Mod", "an interrupted compression offers decompression after reloading files");
            Pump(ChangeCompression(resumed, CompressionScope.Active));
            Pump(resumed.Files.CurrentMeasurement);
            Check(File.ReadAllBytes(data).SequenceEqual(bytes) && !File.Exists(loose), "decompression recovers a partially converted installation");

            Pump(tasks.ChangeModCompressionAsync(old, compress: true));
            baselineTasks = taskList.Count;
            Pump(ChangeCompression(resumed, CompressionScope.All));
            Pump(resumed.Files.CurrentMeasurement);
            Check(active.modSettings.isCompressed && old.modSettings.isCompressed &&
                taskList.Skip(baselineTasks).Count(task => task.Name.StartsWith("Compressing mod: ")) == 1 &&
                taskList.Skip(baselineTasks).Any(task => task.Name.EndsWith(active.version)),
                "compressing all queues only the version that is not already compressed");

            baselineTasks = taskList.Count;
            Pump(ChangeCompression(resumed, CompressionScope.All));
            Check(!active.modSettings.isCompressed && !old.modSettings.isCompressed &&
                taskList.Skip(baselineTasks).Count(task => task.Name.StartsWith("Decompressing mod: ")) == 2,
                "decompressing all processes both compressed versions");

            Pump(resumed.Files.CurrentMeasurement);
            queue.Enqueue(blocker);
            var queued = ChangeCompression(resumed, CompressionScope.Active);
            Pump();
            Check(resumed.Versions.ActiveTask?.Info == "In Queue", "compression waits for its queue slot before converting files");
            active.modSettings.Load(active.fullPath);
            active.modSettings.isCompressed = true;
            active.modSettings.Save();
            var skipped = resumed.Versions.ActiveTask!;
            queue.Dequeue();
            Pump(queued);
            Check(skipped.Info == "Already compressed" && File.Exists(data),
                "a version that changes state while queued is rechecked before any conversion");

            using var mobileModel = (ModLocalFilesViewModel)Activator.CreateInstance(typeof(ModLocalFilesViewModel),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, new object[] { resumed, false }, null)!;
            var mobilePage = new ModLocalFilesView { DataContext = mobileModel, Classes = { "Futuristic" } };
            var mobileWindow = new Window { Content = mobilePage, Width = 275, Height = 844 };
            mobileWindow.Show(); Pump();
            Check(!mobilePage.FindControl<Button>("OpenLibraryButton")!.IsVisible && !mobilePage.FindControl<Button>("OpenModButton")!.IsVisible,
                "Android style hides both folder shortcuts");
            mobileWindow.Close();
            reopened.Close();
            host.Close();
            Pump();
        }
        finally
        {
            Knossos.NET.Knossos.globalSettings.basePath = savedLibrary;
            Knossos.NET.Knossos.globalSettings.modCompression = savedCompression;
        }
    }
}

sealed class LocalRepairClientFactory : IHttpClientFactory
{
    private readonly byte[] payload;
    public LocalRepairClientFactory(byte[] payload) => this.payload = payload;
    public HttpClient CreateClient(string name) => new(new LocalRepairResponse(payload));

    private sealed class LocalRepairResponse : HttpMessageHandler
    {
        private readonly byte[] payload;
        public LocalRepairResponse(byte[] payload) => this.payload = payload;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(payload) });
    }
}
