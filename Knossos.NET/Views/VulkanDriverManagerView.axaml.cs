using Avalonia.Controls;
using Avalonia.Interactivity;
using Knossos.NET.ViewModels;

namespace Knossos.NET.Views;

public partial class VulkanDriverManagerView : UserControl
{
    public VulkanDriverManagerView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is VulkanDriverManagerViewModel manager)
            await manager.LoadInstalledAsync();
    }

    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is VulkanDriverManagerViewModel manager && TopLevel.GetTopLevel(this) is { } top)
            await manager.ImportAsync(top.StorageProvider);
    }

    private async void OnActivateClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is VulkanDriverManagerViewModel manager && sender is Button { DataContext: InstalledVulkanDriverRow row })
            await manager.ActivateAsync(row);
    }

    private async void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is VulkanDriverManagerViewModel manager && sender is Button { DataContext: InstalledVulkanDriverRow row })
            await manager.DeleteAsync(row);
    }

    private async void OnReloadClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is VulkanDriverManagerViewModel manager)
            await manager.ReloadReleasesAsync();
    }

    private async void OnDownloadClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is VulkanDriverManagerViewModel manager && sender is Button { DataContext: VulkanDriverDownloadRow row })
            await manager.DownloadAsync(row);
    }
}
