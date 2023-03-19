# Knossos.NET<br />
![KnossosNET](https://i.imgur.com/6JPvYmO.png)
<br />
<br />
This project is intended to create a multi platform launcher for Freespace 2 Open using .NET 6.0 and AvaloniaUI<br />
<br />
<br />
**Runtime Requierement:**<br />
.Net 6.0.14<br />
https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-6.0.14-windows-x64-installer<br />
<br /><br />
**The current status:**<br />
<br />
-Second test version (0.0.2) - Most of the Basic functions are in, including Mod install. Not retail installer yet.<br />
-Directly added SharpCompress as a lib at least until the nuget package get updated. Nebula files cant be currently decompressed with the nuget version.
-Working in Windows and Linux
-The look of the UI is not a priority until all basic functionality is in, but help with the design colors and ideas is very welcome as im not a UI designer.<br />
-MacOS version is untested and unlikely to work.<br />
-PxoAPI implementation is very basic and WIP<br />
-Mod details is missing features<br />
<br /><br />
**CmdLine priority explained:**<br />
On KnossosNET there are multiples sources of cmdline arguments that are eventually joined into a single one to launch the game, repeating arguments are not allowed.<br />
The priority works in this order.<br />
<br />
SystemCMD(Settings) > Global CMD > User/Mod CMD
<br />
<br />
**Dev Enviroment:**<br />
-MSVC 2022<br />
-.NET 6.0.406 SDK https://dotnet.microsoft.com/en-us/download/dotnet/6.0<br />
-Avalonia Extension for Visual Studio 0.10.18 https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaVS<br />
<br />
<br />
**Current NuGet Packages:**<br />
-Avalonia 0.10.18<br />
-Avalonia.Desktop 0.10.18<br />
-Avalonia.Diagnostics 0.10.18<br />
-CommunityToolkit.Mvvm 8.1.0<br />
-XamlNameReferenceGenerator 1.5.1<br />
-Microsoft.System.Speech 7.0.0<br />
-ini-parser-netstandard 2.5.2<br />
<br />
<br />
**Compiling for Linux and Mac:**<br />
-Right click on the project -> Publish<br />
-Export to folder<br />
-From there you can pick the dest enviroment on the list<br />