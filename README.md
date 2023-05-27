# Knossos.NET<br />
![KnossosNET](https://i.imgur.com/HGmL9iI.png)
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
-Version 0.0.7 - Most of the user functions are in, except for dev mode<br />
-Working in Windows and Linux<br />
-Mod compression support<br />
-MacOS version is untested<br />
-PxoAPI implementation is very basic and WIP<br />
-Mod details is missing features, waiting for Avalonia 0.11 to see what to do<br />
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
-Avalonia Extension for Visual Studio https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaVS<br />
<br />
<br />
**Included Libs:**<br />
-VP.NET<br />
-IonKiwi.lz4 (modified for LZ41 support)<br />
<br />
<br />
**Current NuGet Packages:**<br />
-Avalonia 0.10.19<br />
-Avalonia.Desktop 0.10.19<br />
-Avalonia.Diagnostics 0.10.19<br />
-CommunityToolkit.Mvvm 8.1.0<br />
-XamlNameReferenceGenerator 1.6.1<br />
-ini-parser-netstandard 2.5.2<br />
-SharpCompress 0.33.0
<br />
<br />

**Compiling for Linux and Mac:**<br />
-Right click on the project -> Publish<br />
-Export to folder<br />
-From there you can pick the dest enviroment on the list<br />