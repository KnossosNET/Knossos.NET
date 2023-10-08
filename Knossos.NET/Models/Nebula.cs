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
        private class NewerModVersionsData
        {
            public string Id { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
        }

        private struct NebulaSettings
        {
            public string? etag { get; set; }
            public List<NewerModVersionsData> NewerModsVersions { get; set; } 

            public NebulaSettings()
            {
                etag = null;
                NewerModsVersions = new List<NewerModVersionsData>();
            }
        }

        //https://cf.fsnebula.org/storage/repo.json
        //https://dl.fsnebula.org/storage/repo.json
        //https://aigaion.feralhosting.com/discovery/nebula/repo.json
        //https://fsnebula.org/storage/repo.json"
        private static readonly string repoUrl = @"https://fsnebula.org/storage/repo.json";
        private static readonly string apiURL = @"https://api.fsnebula.org/api/1/";
        private static readonly string nebulaURL = @"https://fsnebula.org/";
        private static readonly bool listFS2Override = false;
        private static CancellationTokenSource? cancellationToken = null;
        public static bool repoLoaded = false;
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
                bool displayUpdates = settings.NewerModsVersions.Any() ? true : false;
                var webEtag = await GetRepoEtag();
                if (!File.Exists(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "repo.json") || settings.etag != webEtag)
                {
                    //Download the repo.json
                    if (TaskViewModel.Instance != null)
                    {
                        var result = await Dispatcher.UIThread.InvokeAsync(async()=>await TaskViewModel.Instance.AddFileDownloadTask(repoUrl, SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "repo_temp.json", "Downloading repo.json", true, "The repo.json file contains info on all the mods available in Nebula, without this you will not be able to install new mods or engine builds"), DispatcherPriority.Background);
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
                    displayUpdates = false;
                }
                if (cancellationToken != null && cancellationToken!.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                var updates = await ParseRepoJson();
                if(updates != null && updates.Any())
                {
                    SaveSettings();
                    if(displayUpdates)
                    {
                        try
                        {
                            await Dispatcher.UIThread.InvokeAsync(() => TaskViewModel.Instance!.AddDisplayUpdatesTask(updates), DispatcherPriority.Background);
                        }
                        catch { }
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
        }

        private static bool IsModUpdate(Mod mod)
        {
            try
            {
                string id = mod.id;
                if (mod.type == ModType.engine)
                {
                    if (mod.stability == "rc")
                    {
                        id += "_RC";
                    }
                    else if (mod.stability == "nightly")
                    {
                        id += "_nightly";
                    }
                }
                var exist = settings.NewerModsVersions.FirstOrDefault(m => m.Id == id);
                if (exist != null)
                {
                    if (SemanticVersion.Compare(exist.Version, mod.version) < 0)
                    {
                        exist.Version = mod.version;
                        return true;
                    }
                }
                else
                {
                    settings.NewerModsVersions.Add(new NewerModVersionsData { Id = id, Version = mod.version });
                    mod.isNewMod = true;
                    return true;
                }
            }catch(Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.IsModUpdate()", ex);
            }
            return false;
        }

        private static async Task<List<Mod>?> ParseRepoJson()
        {
            try
            {
                await WaitForFileAccess(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "repo.json");
                if (cancellationToken != null && cancellationToken!.IsCancellationRequested)
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

                    var updates = new List<Mod>();
                    Mod? lastMod = null;
                    await foreach (Mod? mod in mods)
                    {
                        if(mod != null && IsModUpdate(mod))
                        {
                            updates.Add(mod);
                        }
                        if (cancellationToken != null && cancellationToken!.IsCancellationRequested)
                        {
                            fileStream.Close();
                            throw new TaskCanceledException();
                        }
                        if (mod != null && !mod.isPrivate)
                        {
                            
                            if (mod.type == ModType.engine)
                            {
                                //This is already installed?
                                var isInstalled = Knossos.GetInstalledBuildsList(mod.id)?.Where(b => b.version == mod.version);
                                if (isInstalled == null || isInstalled.Count() == 0)
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() => FsoBuildsViewModel.Instance?.AddBuildToUi(new FsoBuild(mod)), DispatcherPriority.Background);
                                }
                            }
                            if (mod.type == ModType.tc || mod.type == ModType.mod && ( listFS2Override || ( mod.parent != "FS2" || mod.parent == "FS2" && Knossos.retailFs2RootFound) ) )
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
                    return updates;
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
            return null;
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
                if (!File.Exists(filename))
                {
                    return;
                }
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
                if (cancellationToken != null && cancellationToken!.IsCancellationRequested)
                {
                    return;
                }
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
                    client.Timeout = TimeSpan.FromSeconds(10);
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

        public static async void SaveSettings()
        {
            try
            {
                await WaitForFileAccess(SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "nebula.json");
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

        /* Nebula API handling starts here */
        #region NebulaApi
        private struct ApiReply
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public bool result { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string id { get; set; }
        }

        private enum ApiMethod
        {
            POST,
            PUT,
            GET
        }

        private static async Task<ApiReply?> ApiCall(string resourceUrl, Dictionary<string, string> keyValues, ApiMethod method = ApiMethod.POST) 
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    switch (method)
                    {
                        case ApiMethod.POST:
                            {
                                using (FormUrlEncodedContent content = new FormUrlEncodedContent(keyValues))
                                {
                                    var response = await client.PostAsync(apiURL + resourceUrl, content);
                                    if (response.IsSuccessStatusCode)
                                    {
                                        var json = await response.Content.ReadAsStringAsync();
                                        var reply = JsonSerializer.Deserialize<ApiReply>(json);
                                        if (reply.result)
                                            return reply;

                                        Log.Add(Log.LogSeverity.Error, "Nebula.ApiCall", "An error has ocurred during nebula api POST call: " + response.StatusCode + "\n" + json);
                                    }
                                    else
                                    {
                                        Log.Add(Log.LogSeverity.Error, "Nebula.ApiCall", "An error has ocurred during nebula api POST call: " + response.StatusCode);
                                    }
                                }
                            } break;
                        case ApiMethod.PUT:
                            {
                                throw new NotImplementedException();
                            }
                        case ApiMethod.GET:
                            {
                                throw new NotImplementedException();
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.ApiCall", ex);
            }
            return null;
        }

        public static async Task<bool> UploadLog(string logString)
        {
            try
            {
                var reply = await ApiCall("log/upload", new Dictionary<string, string> { { "log", logString } });
                if (reply.HasValue)
                {
                    Log.Add(Log.LogSeverity.Information, "Nebula.UploadLog", "Uploaded log file to Nebula: " + nebulaURL + "log/" + reply.Value.id);
                    Knossos.OpenBrowserURL(nebulaURL + "log/" + reply.Value.id);
                    return true;
                }
            }
            catch(Exception ex) 
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.UploadLog", ex);
            }
            return false;
        }

        public static async Task<bool> ReportMod(Mod mod, string reason)
        {
            try
            {
                var data = new Dictionary<string, string>()
                {
                    { "mid", mod.id },
                    { "version", mod.version },
                    { "message", reason }
                };
                var reply = await ApiCall("mod/release/report", data);
                if (reply.HasValue)
                {
                    Log.Add(Log.LogSeverity.Information, "Nebula.ReportMod", "Reported Mod: " + mod + " to fsnebula successfully.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.ReportMod", ex);
            }
            return false;
        }
        #endregion
    }
}
