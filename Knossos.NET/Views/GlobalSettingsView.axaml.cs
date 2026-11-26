using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Knossos.NET.Classes;
using Knossos.NET.ViewModels;

namespace Knossos.NET.Views
{
    public partial class GlobalSettingsView : UserControl
    {
        public GlobalSettingsView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            Knossos.LibraryPathsChanged += RefreshLibraryPaths;
            RefreshLibraryPaths();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            Knossos.LibraryPathsChanged -= RefreshLibraryPaths;
            base.OnDetachedFromVisualTree(e);
        }

        private void RefreshLibraryPaths()
        {
            if (DataContext is GlobalSettingsViewModel settings)
                settings.RefreshLibraryPaths();
        }

        private async void RemoveLibraryClicked(object? sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: string path } && DataContext is GlobalSettingsViewModel settings)
                await settings.RemoveLibraryAsync(path);
        }
    }
}
