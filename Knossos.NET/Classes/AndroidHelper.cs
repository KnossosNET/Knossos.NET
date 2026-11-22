#if ANDROID
using Android.App;
using Android.Content;
using Avalonia.Threading;
using Android.OS;
using Android.OS.Storage;
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Knossos.NET.Classes;

internal sealed record AndroidLibraryVolume(string Path, string Description, string FreeSpace);

public static class AndroidHelper
{
    public static Func<string, Task>? ShareTextAsyncFunc { get; set; }
    public static Func<string, string, Task>? ShareFileAsyncFunc { get; set; }
    public static Func<string, Task>? OpenUrlAsyncFunc { get; set; }
    public static Func<string, Task>? InstallApkAsyncFunc { get; set; }

    /// <summary>
    /// Open URL on external android web browser
    /// </summary>
    /// <param name="url"></param>
    /// <returns>task</returns>
    public static Task OpenUrlAsync(string url)
    {
        return OpenUrlAsyncFunc?.Invoke(url) ?? Task.CompletedTask;
    }

    /// <summary>
    /// Open text with a external android app
    /// </summary>
    /// <param name="text"></param>
    /// <returns>task</returns>
    public static Task ShareTextAsync(string text)
    {
        if (text.Length == 0 || ShareTextAsyncFunc == null)
            return Task.CompletedTask;
        return ShareTextAsyncFunc.Invoke(text);
    }

    /// <summary>
    /// Open a file with a external android app
    /// </summary>
    /// <param name="path"></param>
    /// <param name="mimeType"></param>
    /// <returns>task</returns>
    public static Task ShareFileAsync(string path, string mimeType = "text/plain")
    {
        if (path.Length == 0 || ShareFileAsyncFunc == null)
            return Task.CompletedTask;
        return ShareFileAsyncFunc.Invoke(path, mimeType);
    }

    /// <summary>
    /// Sends an APK to Android's package installer.
    /// </summary>
    /// <param name="fullPath">Full path to the downloaded APK.</param>
    public static Task InstallApkAsync(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || InstallApkAsyncFunc == null)
            return Task.CompletedTask;
        return InstallApkAsyncFunc.Invoke(fullPath);
    }

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
    /// Mounted storage volumes where Android provides an app-specific library folder.
    /// </summary>
    internal static IReadOnlyList<AndroidLibraryVolume> GetLibraryVolumes()
    {
        var context = Application.Context;
        var storage = context.GetSystemService(Context.StorageService) as StorageManager;
        var volumes = new List<AndroidLibraryVolume>();
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var directory in context.GetExternalFilesDirs(null) ?? Array.Empty<Java.IO.File>())
        {
            if (directory == null)
                continue;

            try
            {
                if (Android.OS.Environment.GetExternalStorageState(directory) != Android.OS.Environment.MediaMounted)
                    continue;

                var path = Path.Combine(directory.AbsolutePath, "library");
                if (!paths.Add(path))
                    continue;

                var description = OperatingSystem.IsAndroidVersionAtLeast(24)
                    ? storage?.GetStorageVolume(directory)?.GetDescription(context)
                    : null;
                if (string.IsNullOrWhiteSpace(description))
                    description = Android.OS.Environment.InvokeIsExternalStorageRemovable(directory) ? "Removable storage" : "Internal storage";

                volumes.Add(new AndroidLibraryVolume(path, description, GetVolumeFreeSpace(directory.AbsolutePath)));
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Warning, "AndroidHelper.GetLibraryVolumes()", ex);
            }
        }

        if (volumes.Count == 0)
        {
            var directory = GetInternalAppFilesDir();
            volumes.Add(new AndroidLibraryVolume(Path.Combine(directory, "library"), "Internal app storage", GetVolumeFreeSpace(directory)));
        }
        return volumes;
    }

    private static string GetVolumeFreeSpace(string directory)
    {
        try
        {
            using var stats = new StatFs(directory);
            return $"{KnUtils.FormatBytes(stats.AvailableBytes)} free";
        }
        catch (Exception ex)
        {
            Log.Add(Log.LogSeverity.Warning, "AndroidHelper.GetVolumeFreeSpace()", ex);
            return "Free space unavailable";
        }
    }

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
    /// APK update destination inside the FileProvider's shared-files directory.
    /// Android can clear the cached APK safely after installation.
    /// </summary>
    public static string GetUpdateApkPath()
    {
        var cacheDir = Application.Context.CacheDir?.AbsolutePath ?? GetInternalAppFilesDir();
        var sharedFilesDir = Path.Combine(cacheDir, "shared-files");
        Directory.CreateDirectory(sharedFilesDir);
        return Path.Combine(sharedFilesDir, "update.apk");
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
    /// Launch FSO, on Android.g
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
                intent.PutExtra("workingFolder", workingFolder);
            }
            
            if (cmdline.Length > 0)
                intent.PutStringArrayListExtra("fsoArgs", cmdline.Split(" "));

            ctx.StartActivity(intent);
        }
        catch (Exception ex)
        {
            Log.Add(Log.LogSeverity.Error, "AndroidHelper.LaunchFSO", ex);
            throw;
        }
    }

    /// <summary>
    /// Gets FSO flags
    /// </summary>
    /// <param name="engineLibPath"></param>
    /// <param name="timeoutMs"></param>
    /// <returns>json string or empty string</returns>
    public static async Task<string> GetFlagsStringFSOAsync(string engineLibPath, int timeoutMs = 30000,
        string? workingFolder = null, bool portableMode = false)
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

            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var receiver = new FlagsResultReceiver(tcs);

            var intent = new Intent();
            intent.SetClassName(ctx, "com.knossosnet.knossosnet.FlagsActivity");
            intent.AddFlags(ActivityFlags.NewTask);
            intent.PutExtra("engineLibName", System.IO.Path.Combine(dstAbiDir, libName));
            if (workingFolder != null)
                intent.PutExtra("workingFolder", workingFolder);

            var args = new List<string> { "-get_flags", "json_v2" };
            if (portableMode)
                args.Insert(0, "-portable_mode");
            intent.PutStringArrayListExtra("fsoArgs", args);
            intent.PutExtra("flagsReceiver", receiver);

            ctx.StartActivity(intent);

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs)).ConfigureAwait(false);
            if (completed != tcs.Task)
            {
                Log.Add(Log.LogSeverity.Error, "AndroidHelper.GetFlagsStringFSO", "Timeout waiting for fso flags (" + timeoutMs + "ms).");
                return "";
            }

            return await tcs.Task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Add(Log.LogSeverity.Error, "AndroidHelper.GetFlagsStringFSO", ex);
        }
        return "";
    }

    private sealed class FlagsResultReceiver : ResultReceiver
    {
        private readonly TaskCompletionSource<string> _tcs;

        public FlagsResultReceiver(TaskCompletionSource<string> tcs)
            : base(new Handler(Looper.MainLooper!))
        {
            _tcs = tcs;
        }

        protected override void OnReceiveResult(int resultCode, Bundle? resultData)
        {
            if (resultCode == (int)Result.Ok)
                _tcs.TrySetResult(resultData?.GetString("flagsJson") ?? "");
            else
                _tcs.TrySetResult("");
        }
    }

#else
    //Stubs
    public static string? GetExternalAppFilesDir() => "";
    public static string GetInternalAppFilesDir() => "";
    internal static IReadOnlyList<AndroidLibraryVolume> GetLibraryVolumes() => Array.Empty<AndroidLibraryVolume>();
    public static string GetDefaultLibraryDir() => "";
    public static string GetDefaultKnetDir() => "";
    public static string GetDefaultKnetDataDir() => "";
    public static string GetDefaultFSODataDir() => "";
    public static string GetUpdateApkPath() => "";
    public static void LaunchFSO(string engineLibPath, string? workingFolder, string cmdline) {  }
    public static Task<string> GetFlagsStringFSOAsync(string engineLibPath, int timeoutMs = 30000,
        string? workingFolder = null, bool portableMode = false) => Task.FromResult(string.Empty);
#endif
}

