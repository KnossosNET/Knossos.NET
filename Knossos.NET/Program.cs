using Avalonia;
using Avalonia.Platform;
using System;

namespace Knossos.NET
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            bool softwareRendering = false;

            //Check app args
            foreach (var arg in args)
            {
                if (arg.ToLower() == "-software")
                {
                    softwareRendering = true;
                }
            }

            //Check enviroment variables
            var renderMode = KnUtils.GetEnvironmentVariable("KNET_RENDER_MODE");

            if (renderMode != null && renderMode.ToLower() == "software")
            {
                softwareRendering = true;
            }

            //Start App
            BuildAvaloniaApp(softwareRendering).StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp(bool softwareRendering)
        {
            //Disable Hardware Renderer
            if(softwareRendering)
            {
                return AppBuilder.Configure<App>()
                       .UsePlatformDetect()
                       .LogToTrace()
                       .With(new Win32PlatformOptions
                       {   //Windows
                           RenderingMode = new[]
                           {
                                Win32RenderingMode.Software
                           }
                       })
                       .With(new X11PlatformOptions
                       {   //Linux
                           RenderingMode = new[]
                           {
                               X11RenderingMode.Software
                           }
                       })
                       .With(new AvaloniaNativePlatformOptions
                       {   //MacOS
                           RenderingMode = new[]
                           {
                               AvaloniaNativeRenderingMode.Software
                           }
                       });
            }

            //Default
            return AppBuilder.Configure<App>()
                   .UsePlatformDetect()
                   .LogToTrace();
        }
    }
}
