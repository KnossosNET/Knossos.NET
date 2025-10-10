#if ANDROID
using Android.App;
using Android.OS;
using System.Linq;
using Android.Content;
#endif
using System;
using System.Collections.Generic;
using System.IO;

namespace Knossos.NET.Classes;

public static class AndroidHelper
{
#if ANDROID
    /// <summary>
    /// App main storage inside the internal phone memory
    /// </summary>
    public static string? GetExternalAppFilesDir() => Application.Context.GetExternalFilesDir(null)?.AbsolutePath;

    /// <summary>
    /// Internal app storage, not accessible, only as fallback, should not be used
    /// </summary>
    public static string GetInternalAppFilesDir() => Application.Context.FilesDir!.AbsolutePath;

    /// <summary>
    /// List of app all external locations, SD, USB drives, etc
    /// </summary>
    public static string[] GetAllExternalAppFilesDirs()
        => (Application.Context.GetExternalFilesDirs(null) ?? System.Array.Empty<Java.IO.File>())
            .Where(f => f is not null)
            .Select(f => f!.AbsolutePath)
            .ToArray();

    /// <summary>
    /// Default knossos library folder in android
    /// </summary>
    public static string GetDefaultLibraryDir()
    {
        var baseDir = GetExternalAppFilesDir() ?? GetInternalAppFilesDir();
        var library = Path.Combine(baseDir, "library");
        Directory.CreateDirectory(library);
        return library;
    }

    /// <summary>
    /// Default knossos directory folder in android, i dont belive this is ever used, just in case
    /// </summary>
    public static string GetDefaultKnetDir()
    {
        var baseDir = GetExternalAppFilesDir() ?? GetInternalAppFilesDir();
        var knossos = Path.Combine(baseDir, "knossos");
        Directory.CreateDirectory(knossos);
        return knossos;
    }

    /// <summary>
    /// Default knossos data dir in android, equivalent to the one on appdata in windows
    /// </summary>
    public static string GetDefaultKnetDataDir()
    {
        var baseDir = GetExternalAppFilesDir() ?? GetInternalAppFilesDir();
        var data = Path.Combine(baseDir, "data");
        Directory.CreateDirectory(data);
        return data;
    }

    /// <summary>
    /// Default FSO data path, the one thats on appdata on Windows
    /// </summary>
    public static string GetDefaultFSODataDir()
    {
        return GetInternalAppFilesDir();
    }

    /// <summary>
    /// Copy build .so files to internal app folder for execution
    /// </summary>
    private static void StageAllToInternal(string srcAbiDir, string dstAbiDir)
    {
        Directory.CreateDirectory(dstAbiDir);

        if (!Directory.Exists(srcAbiDir))
        {
            Log.Add(Log.LogSeverity.Error, "AndroidHelper.StageAllToInternal", "Source dir not found: " + srcAbiDir);
            return;
        }

        foreach (var src in Directory.EnumerateFiles(srcAbiDir, "*.so"))
        {
            string dst = System.IO.Path.Combine(dstAbiDir, System.IO.Path.GetFileName(src));
            var si = new FileInfo(src);
            var di = new FileInfo(dst);
            if (!di.Exists || di.Length != si.Length || si.LastWriteTimeUtc != di.LastWriteTimeUtc)
            {
                Log.Add(Log.LogSeverity.Information, "AndroidHelper.StageAllToInternal", "Copy " + src + " to "+dst);
                using (var input = File.OpenRead(src))
                using (var output = File.Create(dst))
                    input.CopyTo(output);

                File.SetLastWriteTime(dst, File.GetLastWriteTime(src));
            }
        }
    }

    /// <summary>
    /// Launch FSO, on Android.
    /// All so files will be copied to app internal storage
    /// </summary>
    /// <param name="engineLibPath"></param>
    /// <param name="workingFolder"></param>
    /// <param name="cmdline"></param>
    public static void LaunchFSO(string engineLibPath, string? workingFolder, string cmdline)
    {
        try
        {
            var ctx = Application.Context;
            string dstAbiDir = System.IO.Path.Combine(ctx.FilesDir!.AbsolutePath, "natives");
            var fi = new FileInfo(engineLibPath);
            var folderPath = fi.Directory!.FullName;
            if (!folderPath.EndsWith("/"))
                folderPath += "/";
            StageAllToInternal(folderPath, dstAbiDir);
            var libName = fi.Name;
            var intent = new Intent();
            intent.SetClassName(ctx, "com.knossosnet.knossosnet.GameActivity");
            intent.AddFlags(ActivityFlags.NewTask);
            intent.PutExtra("engineLibName", Path.Combine(dstAbiDir, libName));

            if (workingFolder != null)
            {
                cmdline += " -working_folder " + workingFolder;
            }

            if (cmdline.Length > 0)
                intent.PutStringArrayListExtra("fsoArgs", cmdline.Split(" "));

            ctx.StartActivity(intent);
        }
        catch (Exception ex)
        {
            Log.Add(Log.LogSeverity.Error, "AndroidHelper.LaunchFSO", ex);
        }
    }
#else
    //Stubs
    public static string? GetExternalAppFilesDir() => "";
    public static string GetInternalAppFilesDir() => "";
    public static string[] GetAllExternalAppFilesDirs() => new string[] { };
    public static string GetDefaultLibraryDir() => "";
    public static string GetDefaultKnetDir() => "";
    public static string GetDefaultKnetDataDir() => "";
    public static string GetDefaultFSODataDir() => "";
    public static void LaunchFSO(string engineLibPath, string? workingFolder, string cmdline) {  }
#endif
}

