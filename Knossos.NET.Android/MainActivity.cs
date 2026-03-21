using Android.Content.PM;
using Android.Views;
using AndroidX.Core.View;
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

    private void hideSystemUI()
    {
        WindowCompat.SetDecorFitsSystemWindows(Window, false);

        var windowInsetsController = WindowCompat.GetInsetsController(Window, Window?.DecorView);

        if (windowInsetsController != null)
        {
            // Hide both navigation and status bars
            windowInsetsController.Hide(WindowInsetsCompat.Type.SystemBars());

            // Or only navigation bars:
            //windowInsetsController.Hide(WindowInsetsCompat.Type.NavigationBars());

            // Set behavior to show bars temporarily on swipe
            windowInsetsController.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
        }
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if(hasFocus)
            hideSystemUI();
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
        //Window?.AddFlags(WindowManagerFlags.Fullscreen);

        base.OnCreate(savedInstanceState);
        hideSystemUI();
    }

    protected override void OnResume()
    {
        base.OnResume();
        hideSystemUI();
    }
}
