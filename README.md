# Knossos.NET
This project is intended to create a multi platform launcher for Freespace 2 Open using .NET 6.0 and AvaloniaUI

**Runtime Requierement:**
.Net 6.0.14
https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-6.0.14-windows-x64-installer

**The current status:**

-Released a initial test version (0.0.1), this version does not support mod installing or nebula mod browsing.
-Currently working on adding mod installing
-The look of the UI is not a priority until all basic functionality is in, but help with the design colors and ideas is very welcome as im not a UI designer.
-Linux and MacOS versions are untested and unlikely to work.
-PxoAPI implementation is very basic and WIP
-Mod details is missing features

**CmdLine priority explained:**
On KnossosNET there are multiples sources of cmdline arguments that are eventually joined into a single one to launch the game, repeating arguments are not allowed.
The priority works in this order

SystemCMD(Settings) > Global CMD > User/Mod CMD

**Dev Enviroment:**
-MSVC 2022
-.NET 6.0.406 SDK https://dotnet.microsoft.com/en-us/download/dotnet/6.0
-Avalonia Extension for Visual Studio 0.10.18 https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaVS

**Current NuGet Packages:**
-Avalonia 0.10.18
-Avalonia.Desktop 0.10.18
-Avalonia.Diagnostics 0.10.18
-CommunityToolkit.Mvvm 8.1.0
-XamlNameReferenceGenerator 1.5.1

**Compiling for Linux and Mac:**
-Right click on the project -> Publish
-Export to folder
-From there you can pick the dest enviroment on the list