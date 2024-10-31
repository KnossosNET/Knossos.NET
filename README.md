# Knossos.NET<br />
![KnossosNET](https://i.imgur.com/HGmL9iI.png)
<br />
<br />
Knossos.NET, also known as KNet, is a multi-platform launcher for Freespace 2 Open using .NET 6.0 and AvaloniaUI<br />
<br />

<br /><br />
## **Current Status:**<br />
Version 1.2.2 has been released!<br />
Check out our download page: https://knossoslauncher.com/<br />

<br /><br />
## **Main Improvements:**<br />
Knossos.NET offers greatly improved performance and stability over the old Python-based Knossos.  Because it is written in C#, builds can be released much more easily.  Knossos.NET also minimizes the amount of memory and VRAM used, so that slower computers will not suffer performance penalties when running FSO.  Knossos.NET furthermore supports compressed VPs, it can create compressed VPs when using the dev tab, and it can detect duplicate installations and hardlink them, reducing hard drive footprint. Finally Knossos.NET has additional features compared to original Knossos, while still maintaining feature parity.  Some of these features are detailed below.

<br /><br />
## **Mod Hardlinking:**<br />
Knossos.Net is able to create hardlinks between duplicate files or packages within the same mod.  If a file has not changed in a new version of an FSO mod, then a hardlink can be created.  Hardlinks are a way to tell the OS that we want the same file referenced in two different folders.  A hardlinked file will appear in both folders that reference it, and it will be counted again with every reference when calculating hard drive space, even though it only exists once on the disk.  Hardlinking can be disabled in Knossos.Net's settings tab, or during mod installation.
<br /><br />
This greately minimizes hard drive usage, but comes with two things to keep in mind: 
<br />
1) A hardlinked file will only be deleted once all of its references have been deleted.
2) If one copy of a hardlinked file is altered, then all the references point to the altered file. It really is multiple references to the same file.  If you want to edit a mod's hardlinked file, please create a dev version of it in the dev tab.  This will create a non-hardlinked version that can be safely edited.

<br /><br />
## **Quick Launch Cmdline:**<br />
KnossosNET supports direct mod launch by adding a Command Line argument, this will open the launcher, and will launch FSO to play a mod with all the current settings and configurations, and close the launcher.<br />
Example:<br />
<br />
KnossosNET.exe -playmod mod-id -version mod-version (optional) -exec fso-exec-type (optional)<br />
<br />
If no version is given (KnossosNET.exe -playmod mod-id) the highest version of the mod will be launched.<br />
If no fso-exec-type is given or "Default", or an invalid type is passed "Release" will be used instead. <br />
Exec type options:<br />
-Default<br />
-Release<br />
-Debug<br />
-Fred2<br />
-Fred2Debug<br />
-QtFred<br />
-QtFredDebug<br />
The "Settings" window on a mod displays the Command Line to play that mod directly via Quick Launch. You can copy that Quick Launch Command Line and use it to create a shortcut to easily launch that mod. <br />

<br /><br />
## **Software Rendering Mode:**<br />
By default Knet will render the UI in the GPU, can be set to run completely on software rendering what effectively avoids any use of the user GPU, this will come at the cost of increased CPU usage, what should not be a problem when in idle.
<br />
You can force the software rendering mode by using the "-software" Command Line argument or by setting an environment variable "KNET_RENDER_MODE" to "software".

<br /><br />
## **mod.ini Support:**<br />
Using legacy mod.ini files for mod folders is also supported, and some additional keys were added to extend support. mod.ini can be used to attempt to load an old mod or to manually add a mod to the launcher whiout having to write a mod.json file. The folder still has to be placed in the correct path inside the library as with any other mod.<br />

Details on how Knet handles mod.ini and the new keys can be found here:<br />
https://wiki.hard-light.net/index.php/Mod.ini#KnossosNET_Support<br />

<br /><br />
## **CmdLine Priority Explained:**<br />
On KnossosNET there are multiples sources of Command Line arguments that are eventually joined into a single one to launch the game, repeating arguments are not allowed.<br />

- By default, the priority works in this order:<br />
  - _SystemCMD(Global Settings) > Global CMD > User/Mod CMD_

- Users also have the option to not apply the global Command Line on a per-mod basis by checking the box in a mod's settings. If checked, the priority is then only: <br />
  - _User/Mod CMD_


<br /><br />
## **Dev Environment:**<br />
- MSVC 2022<br />
- .NET 8.0.403 SDK https://dotnet.microsoft.com/en-us/download/dotnet/8.0<br />
- Avalonia Extension for Visual Studio https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaVS<br />

<br /><br />
## **Included Libs:**<br />
- VP.NET<br />
- IonKiwi.lz4 (modified for LZ41 support)<br />

<br /><br />
## **Current NuGet Packages:**<br />
- Avalonia 11.0.5<br />
- Avalonia.Desktop 11.0.5<br />
- Avalonia.Diagnostics 11.0.5<br />
- Avalonia.Themes.Fluent 11.0.5<br />
- Avalonia.HtmlRenderer 11.0.0<br />
- CommunityToolkit.Mvvm 8.2.1<br />
- ini-parser-netstandard 2.5.2<br />
- SharpCompress 0.33.0<br />
- AnimatedImage.Avalonia 1.0.7<br />

<br /><br />
## **Compiling for Linux and Mac:**<br />
- Right click on the project -> Publish<br />
- Export to folder<br />
- From there you can pick the dest enviroment on the list<br />
