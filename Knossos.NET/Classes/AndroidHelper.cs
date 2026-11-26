#if ANDROID
using Android.App;
using System.Linq;
using Android.Content;
using Avalonia.Threading;
using Android.OS;
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Knossos.NET.Classes;

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
    /// List of app all external locations, SD, USB drives, etc
    /// </summary>
    public static string[] GetAllExternalAppFilesDirs()
        => (Application.Context.GetExternalFilesDirs(null) ?? System.Array.Empty<Java.IO.File>())
            .Where(f => f is not null)
            .Select(f => Path.Combine(f!.AbsolutePath, "library") )
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
                intent.PutStringArrayListExtra("fsoArgs", LaunchCommandLine.SplitArguments(cmdline));

            ctx.StartActivity(intent);
        }
        catch (Exception ex)
        {
            Log.Add(Log.LogSeverity.Error, "AndroidHelper.LaunchFSO", ex);
        }
    }

    private static readonly SemaphoreSlim queryGate = new(1, 1);

    public static async Task<string> QueryFSOAsync(string engineLibPath, string workingFolder,
        IReadOnlyList<string> args, CancellationToken token)
    {
        var context = Application.Context;
        var resultPath = Path.Combine(context.CacheDir!.AbsolutePath, "fso-query-" + Guid.NewGuid().ToString("N") + ".json");
        await queryGate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var engine = new FileInfo(engineLibPath);
            var queryLibraries = Path.Combine(context.FilesDir!.AbsolutePath, "query-natives");
            StageAllToInternal(engine.Directory!.FullName, queryLibraries);
            var result = new TaskCompletionSource<IBinder?>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var receiver = new QueryResultReceiver(result);

            using var intent = new Intent();
            intent.SetClassName(context, "com.knossosnet.knossosnet.FlagsActivity");
            intent.AddFlags(ActivityFlags.NewTask);
            intent.PutExtra("engineLibName", Path.Combine(queryLibraries, engine.Name));
            intent.PutExtra("workingFolder", workingFolder);
            intent.PutExtra("resultPath", resultPath);
            intent.PutStringArrayListExtra("fsoArgs", new List<string>(args));
            intent.PutExtra("flagsReceiver", receiver);

            token.ThrowIfCancellationRequested();
            context.StartActivity(intent);
            // Let the isolated process exit before another query loads the engine in it.
            var process = await result.Task.WaitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            if (process != null)
            {
                using var death = new QueryProcessDeath();
                try
                {
                    process.LinkToDeath(death, 0);
                    await death.Completion.Task.WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                }
                catch (RemoteException) { /* The query process already exited. */ }
            }

            token.ThrowIfCancellationRequested();
            if (!File.Exists(resultPath)) throw new IOException("FSO exited without sending a launcher query result.");
            return await File.ReadAllTextAsync(resultPath, token).ConfigureAwait(false);
        }
        finally
        {
            try { File.Delete(resultPath); }
            finally { queryGate.Release(); }
        }
    }

    private sealed class QueryResultReceiver : ResultReceiver
    {
        private readonly TaskCompletionSource<IBinder?> result;

        public QueryResultReceiver(TaskCompletionSource<IBinder?> result) : base(new Handler(Looper.MainLooper!))
        {
            this.result = result;
        }

        protected override void OnReceiveResult(int resultCode, Bundle? resultData)
            => result.TrySetResult(resultData?.GetBinder("queryProcess"));
    }

    private sealed class QueryProcessDeath : Java.Lang.Object, IBinderDeathRecipient
    {
        public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void BinderDied() => Completion.TrySetResult(true);
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
    public static string GetUpdateApkPath() => "";
    public static void LaunchFSO(string engineLibPath, string? workingFolder, string cmdline) {  }
#endif
}

