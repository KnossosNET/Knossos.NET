using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Classes;
using Knossos.NET.Models;

namespace Knossos.NET.ViewModels;

public sealed class InstalledVulkanDriverRow
{
    internal InstalledVulkanDriverRow(InstalledVulkanDriver? driver, bool active)
    {
        Driver = driver;
        IsActive = active;
    }

    internal InstalledVulkanDriver? Driver { get; }
    public string Name => Driver?.Name ?? "System driver";
    public string? ZipName => Driver?.ZipName;
    public bool CanDelete => Driver != null;
    public bool IsActive { get; }
    public string ActivationLabel => IsActive ? "Active" : "Activate";
}

public sealed record VulkanDriverDownloadRow(VulkanDriverReleaseZip Zip, bool IsDownloaded)
{
    public string DownloadLabel => IsDownloaded ? "Downloaded" : "Download";
}

public partial class VulkanDriverManagerViewModel : ViewModelBase
{
    private readonly VulkanDriverStore? store = AndroidHelper.VulkanDrivers;
    private readonly Dictionary<string, IReadOnlyList<VulkanDriverReleaseZip>> releaseCache = new(StringComparer.OrdinalIgnoreCase);

    public bool SupportsCustomDrivers => store != null;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? status;
    [ObservableProperty] private string? catalogStatus = "Reload releases to browse drivers.";
    [ObservableProperty] private IReadOnlyList<InstalledVulkanDriverRow> installedDrivers =
        new[] { new InstalledVulkanDriverRow(null, active: true) };
    [ObservableProperty] private IReadOnlyList<VulkanDriverRepository> repositories = Array.Empty<VulkanDriverRepository>();
    [ObservableProperty] private VulkanDriverRepository? selectedRepository;
    [ObservableProperty] private IReadOnlyList<VulkanDriverDownloadRow> availableZips = Array.Empty<VulkanDriverDownloadRow>();

    public Task LoadInstalledAsync() => RunAsync("Reading installed drivers…", RefreshInstalledAsync);

    public Task ImportAsync(IStorageProvider storage) => RunAsync("Importing driver…", async () =>
    {
        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import a Vulkan driver ZIP",
            FileTypeFilter = new[]
            {
                new FilePickerFileType("ZIP archives")
                {
                    Patterns = new[] { "*.zip" },
                    MimeTypes = new[] { "application/zip", "application/x-zip-compressed" }
                }
            }
        });
        if (files.Count == 0)
            return;

        using var file = files[0];
        using var stream = await file.OpenReadAsync();
        await Task.Run(() => store!.InstallAsync(stream, file.Name));
        await RefreshInstalledAsync();
        Status = $"Installed {file.Name}.";
    });

    public Task ActivateAsync(InstalledVulkanDriverRow row) => RunAsync("Changing Vulkan driver…", async () =>
    {
        await Task.Run(() => store!.SetActive(row.Driver?.Id));
        await RefreshInstalledAsync();
        Status = $"{row.Name} will be used the next time FSO starts.";
    });

    public Task DeleteAsync(InstalledVulkanDriverRow row) => row.Driver is not { } driver ? Task.CompletedTask :
        RunAsync("Deleting driver…", async () =>
        {
            await Task.Run(() => store!.Delete(driver.Id));
            await RefreshInstalledAsync();
            Status = $"Deleted {driver.Name}.";
        });

    public Task ReloadReleasesAsync() => RunAsync("Loading driver releases…", async () =>
    {
        var previousPath = SelectedRepository?.GitHubPath;
        var repositories = await VulkanDriverCatalog.GetRepositoriesAsync();
        Repositories = repositories;
        SelectedRepository = repositories.FirstOrDefault(repo =>
            repo.GitHubPath.Equals(previousPath, StringComparison.OrdinalIgnoreCase)) ?? repositories.FirstOrDefault();

        if (SelectedRepository is not { } selected)
        {
            CatalogStatus = "No Vulkan driver repositories are available.";
            return;
        }
        CatalogStatus = "Loading driver releases…";
        releaseCache[selected.GitHubPath] = await VulkanDriverCatalog.GetReleaseZipsAsync(selected);
        RefreshAvailableZips();
    }, catalog: true);

    public Task DownloadAsync(VulkanDriverDownloadRow row) => row.IsDownloaded ? Task.CompletedTask :
        RunAsync($"Downloading {row.Zip.ZipName}…", async () =>
        {
            await VulkanDriverCatalog.DownloadAsync(row.Zip, store!);
            await RefreshInstalledAsync();
            CatalogStatus = $"Installed {row.Zip.ZipName}.";
        }, catalog: true);

    partial void OnSelectedRepositoryChanged(VulkanDriverRepository? value) => RefreshAvailableZips();

    private async Task RefreshInstalledAsync()
    {
        var rows = await Task.Run(() =>
        {
            var active = store!.GetActive()?.Id;
            var installed = store.GetInstalled();
            return new[] { new InstalledVulkanDriverRow(null, active == null) }
                .Concat(installed.Select(driver => new InstalledVulkanDriverRow(driver, active == driver.Id)))
                .ToArray();
        });
        InstalledDrivers = rows;
        UpdateDownloadRows();
    }

    private void RefreshAvailableZips()
    {
        var loaded = UpdateDownloadRows();
        CatalogStatus = !loaded ? "Reload releases to browse drivers."
            : AvailableZips.Count == 0 ? "This repository has no ZIP assets." : null;
    }

    private bool UpdateDownloadRows()
    {
        if (SelectedRepository == null || !releaseCache.TryGetValue(SelectedRepository.GitHubPath, out var zips))
        {
            AvailableZips = Array.Empty<VulkanDriverDownloadRow>();
            return false;
        }

        var installedUrls = InstalledDrivers.Select(row => row.Driver?.DownloadUrl).OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
        AvailableZips = zips.Select(zip => new VulkanDriverDownloadRow(zip, installedUrls.Contains(zip.DownloadUrl))).ToArray();
        return true;
    }

    private async Task RunAsync(string progress, Func<Task> operation, bool catalog = false)
    {
        if (store == null || IsBusy)
            return;
        IsBusy = true;
        if (catalog)
            CatalogStatus = progress;
        else
            Status = progress;
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            Log.Add(Log.LogSeverity.Warning, nameof(VulkanDriverManagerViewModel), ex);
            if (catalog)
                CatalogStatus = ex.Message;
            else
                Status = ex.Message;
        }
        finally
        {
            IsBusy = false;
            if (Status == progress)
                Status = null;
            if (CatalogStatus == progress)
                CatalogStatus = null;
        }
    }
}
