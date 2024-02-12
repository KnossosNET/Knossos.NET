# Knossos.NET<br />
![KnossosNET](https://i.imgur.com/HGmL9iI.png)
<br />
<br />
Knossos.NET, also known as KNet, is a multi-platform launcher for Freespace 2 Open using .NET 6.0 and AvaloniaUI<br />
<br />

<br /><br />
## **Current Status:**<br />
Version 1.0 has been released!<br />
Check out our download page: https://knossosnet.github.io/Knossos-Release-Page/<br />

<br /><br />
## **Main Improvements:**<br />
Knossos.NET offers greatly improved performance and stability over the old Python-based Knossos.  Because it is written in C#, builds can be released much more easily.  Knossos.NET also minimizes the amount of memory and VRAM used, so that slower computers will not suffer performance penalties when running FSO.  Knossos.NET furthermore supports compressed VPs and their creation, reducing hard drive footprint. Finally Knossos.NET has additional features compared to original Knossos, while still maintaining feature parity.  Some of these features are detailed below.

<br /><br />
## **Quick Launch Cmdline:**<br />
KnossosNET supports direct mod launch by adding a cmdline argument, this will open the launcher, and will launch FSO to play a mod with all the current settings and configurations, and close the launcher.<br />
Example:<br />
<br />
KnossosNET.exe -playmod mod-id -version mod-version (optional)<br />
<br />
If no version is given (KnossosNET.exe -playmod mod-id) the highest version of the mod will be launched.<br />
The "Mod Settings" window displays the cmdline to play that mod directly via quick launch.<br />

<br /><br />
## **Software Rendering Mode:**<br />
By default Knet will render the UI in the GPU, can be set to run completely on software rendering what effectively avoids any use of the user GPU, this will come at the cost of increased CPU usage, what should not be a problem when in idle.
<br />
You can force the software rendering mode by using the "-software" cmdline argument or by setting an environment variable "KNET_RENDER_MODE" to "software".

<br /><br />
## **mod.ini Support:**<br />
Using legacy mod.ini files for mod folders is also supported, and some additional keys were added to extend support. mod.ini can be used to attempt to load an old mod or to manually add a mod to the launcher whiout having to write a mod.json file. The folder still has to be placed in the correct path inside the library as with any other mod.<br />

Details on how Knet handles mod.ini and the new keys can be found here:<br />
https://wiki.hard-light.net/index.php/Mod.ini#KnossosNET_Support<br />

<br /><br />
## **CmdLine Priority Explained:**<br />
On KnossosNET there are multiples sources of cmdline arguments that are eventually joined into a single one to launch the game, repeating arguments are not allowed.<br />

- By default, the priority works in this order:<br />
  - _SystemCMD(Global Settings) > Global CMD > User/Mod CMD_

- Users also have the option to not apply the global cmdline on a per-mod basis by checking the box in a mod's settings. If checked, the priority is then only: <br />
  - _User/Mod CMD_


<br /><br />
## **Dev Environment:**<br />
- MSVC 2022<br />
- .NET 6.0.406 SDK https://dotnet.microsoft.com/en-us/download/dotnet/6.0<br />
- Avalonia Extension for Visual Studio https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaVS<br />

<br /><br />
## **Included Libs:**<br />
- VP.NET<br />
- IonKiwi.lz4 (modified for LZ41 support)<br />

<br /><br />
## **Current NuGet Packages:**<br />
- Avalonia 11.0.3<br />
- Avalonia.Desktop 11.0.3<br />
- Avalonia.Diagnostics 11.0.3<br />
- Avalonia.Themes.Fluent 11.0.3<br />
- Avalonia.HtmlRenderer 11.0.0<br />
- CommunityToolkit.Mvvm 8.2.1<br />
- ini-parser-netstandard 2.5.2<br />
- SharpCompress 0.33.0<br />

<br /><br />
## **Compiling for Linux and Mac:**<br />
- Right click on the project -> Publish<br />
- Export to folder<br />
- From there you can pick the dest enviroment on the list<br />
