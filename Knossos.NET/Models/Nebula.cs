using System.Text.Json;
using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Knossos.NET.ViewModels;
using System.Linq;
using System.Text.Json.Serialization;
using Avalonia.Threading;
using System.Collections.Generic;
using Knossos.NET.Classes;
using System.Threading;

namespace Knossos.NET.Models
{
    /*
        Model to handle all the Nebula website operations.
    */
    public static class Nebula
    {
        //https://cf.fsnebula.org/storage/repo.json
        //https://dl.fsnebula.org/storage/repo.json
        private static readonly string repoUrl = @"https://aigaion.feralhosting.com/discovery/nebula/repo.json";
        private static readonly bool listFS2Override = false;
        private static CancellationTokenSource? cancellationToken = null;
        public static bool repoLoaded = false;

        private struct NebulaSettings
        {
            public string? etag { get; set; }
        }

        private static NebulaSettings settings = new NebulaSettings();

        public static async void Trinity()
        {
            try
            {
                repoLoaded = false;
                if (cancellationToken != null)
                {
                    cancellationToken.Cancel();
                    await Task.Delay(5000);
                }
                cancellationToken = new CancellationTokenSource();

                if (File.Exists(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "nebula.json"))
                {
                    string jsonString = File.ReadAllText(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "nebula.json");
                    settings = JsonSerializer.Deserialize<NebulaSettings>(jsonString);
                    Log.Add(Log.LogSeverity.Information, "Nebula.Constructor()", "Nebula seetings has been loaded");
                }
                else
                {
                    Log.Add(Log.LogSeverity.Information, "Nebula.Constructor()", "File nebula.json does not exist.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.Constructor()", ex);
            }
            try
            {
                var webEtag = await GetRepoEtag();
                if (!File.Exists(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "repo.json") || settings.etag != webEtag)
                {
                    //Download the repo.json
                    if (TaskViewModel.Instance != null)
                    {
                        var result = await TaskViewModel.Instance.AddFileDownloadTask(repoUrl, SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "repo_temp.json", "Downloading repo.json", true, "The repo.json file contains info on all the mods available in Nebula, without this you will not be able to install new mods or engine builds");
                        if (cancellationToken!.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }
                        if (result != null && result == true)
                        {
                            try
                            {
                                File.Delete(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "repo.json");
                                File.Move(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "repo_temp.json", SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "repo.json");
                                settings.etag = webEtag;
                                SaveSettings();
                            }
                            catch (Exception ex)
                            {
                                if (cancellationToken != null)
                                {
                                    cancellationToken.Dispose();
                                    cancellationToken = null;
                                }
                                Log.Add(Log.LogSeverity.Error, "Nebula.Trinity()", ex);
                            }
                        }
                    }
                }
                else
                {
                    //No update is needed
                    await Dispatcher.UIThread.InvokeAsync(() => TaskViewModel.Instance!.AddMessageTask("Nebula: repo.json is up to date!"), DispatcherPriority.Background);
                    Log.Add(Log.LogSeverity.Information, "Nebula.Trinity()", "repo.json is up to date!");
                }
                if (cancellationToken!.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                await ParseRepoJson();
            }catch(TaskCanceledException)
            {
                if (cancellationToken != null)
                {
                    cancellationToken.Dispose();
                    cancellationToken = null;
                }
            }
        }

        private static async Task ParseRepoJson()
        {
            try
            {
                await WaitForFileAccess(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "repo.json");
                if (cancellationToken!.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                using (FileStream? fileStream = new FileStream(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "repo.json", FileMode.Open, FileAccess.ReadWrite))
                {
                    fileStream.Seek(-1, SeekOrigin.End);
                    if (fileStream.ReadByte() == '}')
                    {
                        fileStream.SetLength(fileStream.Length - 1);
                    }
                    fileStream.Seek(9, SeekOrigin.Begin);

                    JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
                    var mods = JsonSerializer.DeserializeAsyncEnumerable<Mod?>(fileStream);

                    Mod? lastMod = null;
                    await foreach (Mod? mod in mods)
                    {
                        if (cancellationToken!.IsCancellationRequested)
                        {
                            fileStream.Close();
                            throw new TaskCanceledException();
                        }
                        if (mod != null && !mod.isPrivate)
                        {
                            if (mod.type == "engine")
                            {
                                //This is already installed?
                                var isInstalled = Knossos.GetInstalledBuildsList(mod.id)?.Where(b => b.version == mod.version);
                                if (isInstalled == null || isInstalled.Count() == 0)
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() => FsoBuildsViewModel.Instance?.AddBuildToUi(new FsoBuild(mod)), DispatcherPriority.Background);
                                }
                            }
                            if (mod.type == "tc" || mod.type == "mod" && ( listFS2Override || ( mod.parent != "FS2" || mod.parent == "FS2" && Knossos.retailFs2RootFound) ) )
                            {
                                //This is already installed?
                                var isInstalled = Knossos.GetInstalledModList(mod.id);
                                if (isInstalled == null || isInstalled.Count() == 0)
                                {
                                    if(lastMod == null || lastMod.id == mod.id)
                                    {
                                        lastMod = mod;
                                    }
                                    else
                                    {
                                        if (lastMod.id != mod.id)
                                        {
                                            await Dispatcher.UIThread.InvokeAsync(() => MainWindowViewModel.Instance!.AddNebulaMod(lastMod), DispatcherPriority.Background);
                                            lastMod = mod;
                                        }
                                    }
                                    
                                }
                                else
                                {
                                    bool update = true;
                                    foreach(var intMod in isInstalled)
                                    {
                                        if(SemanticVersion.Compare(intMod.version, mod.version) >= 0)
                                        {
                                            update = false;
                                        }
                                    }
                                    if(update)
                                    {
                                        MainWindowViewModel.Instance?.MarkAsUpdateAvalible(mod.id);
                                    }
                                }
                            }
                        }
                    }
                    fileStream.Close();
                    repoLoaded = true;
                    if(cancellationToken != null)
                    {
                        cancellationToken.Dispose();
                        cancellationToken = null;
                    }
                }
            }
            catch(TaskCanceledException)
            {
                if (cancellationToken != null)
                {
                    cancellationToken.Dispose();
                    cancellationToken = null;
                }
            }
            catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.ParseRepoJson()", ex);
                if (cancellationToken != null)
                {
                    cancellationToken.Dispose();
                    cancellationToken = null;
                }
            }
            return;
        }

        public static void CancelOperations()
        {
            if(cancellationToken != null)
            {
                cancellationToken.Cancel();
            }
        }

        public static async Task<Mod?> GetModData(string id, string version)
        {
            await WaitForFileAccess(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "repo.json");
            using (FileStream? fileStream = new FileStream(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "repo.json", FileMode.Open, FileAccess.ReadWrite))
            {
                try
                {
                    fileStream.Seek(-1, SeekOrigin.End);
                    if (fileStream.ReadByte() == '}')
                    {
                        fileStream.SetLength(fileStream.Length - 1);
                    }
                    fileStream.Seek(9, SeekOrigin.Begin);

                    JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
                    var mods = JsonSerializer.DeserializeAsyncEnumerable<Mod?>(fileStream);

                    await foreach (Mod? mod in mods)
                    {
                        if (mod != null && mod.id == id && mod.version == version)
                        {
                            fileStream.Close();
                            return mod;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Add(Log.LogSeverity.Error, "Nebula.ParseRepoJson()", ex);
                }
                fileStream.Close();
            }
            return null;
        }

        public static async Task<List<Mod>> GetAllModsWithID(string? id)
        {
            var modList = new List<Mod>();
            await WaitForFileAccess(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "repo.json");
            using (FileStream? fileStream = new FileStream(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "repo.json", FileMode.Open, FileAccess.ReadWrite))
            {
                try
                {
                    fileStream.Seek(-1, SeekOrigin.End);
                    if (fileStream.ReadByte() == '}')
                    {
                        fileStream.SetLength(fileStream.Length - 1);
                    }
                    fileStream.Seek(9, SeekOrigin.Begin);

                    JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
                    var mods = JsonSerializer.DeserializeAsyncEnumerable<Mod?>(fileStream);

                    await foreach (Mod? mod in mods)
                    {
                        if (mod != null && (mod.id == id || id == null))
                        {
                            modList.Add(mod);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Add(Log.LogSeverity.Error, "Nebula.ParseRepoJson()", ex);
                }
                fileStream.Close();
                return modList;
                
            }
        }

        private static async Task WaitForFileAccess(string filename)
        {
            try
            {
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read))
                {
                    inputStream.Close();
                    return;
                }
            }
            catch (IOException)
            {
                Log.WriteToConsole("repo.json is in use. Waiting for file access...");
                await Task.Delay(500);
                await WaitForFileAccess(filename);
            }
        }

        private static async Task<string?> GetRepoEtag()
        {
            try
            {
                string? newEtag = null;
                Log.Add(Log.LogSeverity.Information, "Nebula.GetRepoEtag()", "Getting repo.json etag.");
                using (HttpClient client = new HttpClient())
                {
                    var result = await client.GetAsync(repoUrl, HttpCompletionOption.ResponseHeadersRead);
                    newEtag = result.Headers?.ETag?.ToString().Replace("\"", "");
                }
                Log.Add(Log.LogSeverity.Information, "Nebula.GetRepoEtag()", "repo.json etag: "+ newEtag);
                return newEtag;
            }
            catch (Exception ex) 
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.GetRepoEtag()", ex);
                return null;
            }
        }

        public static void SaveSettings()
        {
            try
            {
                var encoderSettings = new TextEncoderSettings();
                encoderSettings.AllowRange(UnicodeRanges.All);

                var options = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.Create(encoderSettings),
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "nebula.json", json, Encoding.UTF8);
                Log.Add(Log.LogSeverity.Information, "Nebula.SaveSettings()", "Nebula settings has been saved.");
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.SaveSettings()", ex);
            }
        }
    }
}
