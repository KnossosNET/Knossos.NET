using Avalonia;
using Avalonia.Controls;

namespace Knossos.NET.Views;

public partial class ModIngameSettingsView : UserControl
{
    public ModIngameSettingsView()
    {
        InitializeComponent();
        SizeChanged += (_, _) =>
        {
            var compact = Bounds.Width < 480;
            Classes.Set("compact", compact);
            SettingsLayout.Margin = new Thickness(compact ? 12 : 24, 20);
        };
    }
}
