using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Knossos.NET.Classes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Knossos.NET.Views;

internal partial class AndroidLibraryPickerView : UserControl
{
    private readonly TaskCompletionSource<string?> selection = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal IReadOnlyList<AndroidLibraryVolume> Volumes { get; }
    internal bool HasNoVolumes => Volumes.Count == 0;

    private AndroidLibraryPickerView(IReadOnlyList<AndroidLibraryVolume> volumes)
    {
        Volumes = volumes;
        InitializeComponent();
        DataContext = this;
    }

    internal static Task<string?> PickAsync(IReadOnlyList<AndroidLibraryVolume> volumes)
    {
        var picker = new AndroidLibraryPickerView(volumes);
        if (DialogHost.Show(picker, onDismiss: () => picker.Close(null)) == null)
            return Task.FromResult<string?>(null);

        picker.CancelButton.Focus();
        return picker.selection.Task;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            Close(null);
            e.Handled = true;
        }
    }

    private void AddClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: AndroidLibraryVolume volume })
            Close(volume.Path);
    }

    private void CancelClicked(object? sender, RoutedEventArgs e) => Close(null);

    private void Close(string? path)
    {
        DialogHost.Hide(this);
        selection.TrySetResult(path);
    }
}
