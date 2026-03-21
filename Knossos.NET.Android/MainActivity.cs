using Android.Content.PM;
using Android.Views;
using Avalonia;
using Avalonia.Android;

namespace Knossos.NET.Android;

[Activity(
    Label = "Knossos.NET",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation
        | ConfigChanges.ScreenSize
        | ConfigChanges.UiMode,
    ScreenOrientation = ScreenOrientation.SensorLandscape)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
        //Window?.AddFlags(WindowManagerFlags.Fullscreen);
        base.OnCreate(savedInstanceState);
    }
}
