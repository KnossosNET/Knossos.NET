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
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Collections;
using System.Drawing;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using Avalonia.Controls;

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
            public string? user { get; set; }
            public string? pass { get; set; }
            public bool logged { get; set; }
            public List<NewerModVersionsData> NewerModsVersions { get; set; }


            public NebulaSettings()
            {
                etag = null;
                user = null;
                pass = null;
                logged = false;
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
        private static string? apiUserToken = null;
        public static bool userIsLoggedIn { get { return settings.logged; } }
        public static string? userName { get { return settings.user; } }
        public static string? userPass { get { return settings.pass != null ? SysInfo.DIYStringDecryption(settings.pass) : null; } }
        private static Mod[]? privateMods;

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
                        var result = await Dispatcher.UIThread.InvokeAsync(async () => await TaskViewModel.Instance.AddFileDownloadTask(repoUrl, SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "repo_temp.json", "Downloading repo.json", true, "The repo.json file contains info on all the mods available in Nebula, without this you will not be able to install new mods or engine builds"), DispatcherPriority.Background);
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
                if (updates != null && updates.Any())
                {
                    SaveSettings();
                    if (displayUpdates)
                    {
                        try
                        {
                            await Dispatcher.UIThread.InvokeAsync(() => TaskViewModel.Instance!.AddDisplayUpdatesTask(updates), DispatcherPriority.Background);
                        }
                        catch { }
                    }
                }
                if (cancellationToken != null && cancellationToken!.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                if (userIsLoggedIn)
                {
                    await LoadPrivateMods(cancellationToken);
                } 
            }
            catch (TaskCanceledException)
            {
                if (cancellationToken != null)
                {
                    cancellationToken.Dispose();
                    cancellationToken = null;
                }
            }
        }

        private static async Task LoadPrivateMods(CancellationTokenSource? cancellationToken)
        {
            try
            {
                privateMods = await GetPrivateMods();
                if (privateMods != null && privateMods.Any())
                {
                    Mod? lastMod = null;
                    foreach (var mod in privateMods)
                    {
                        if (cancellationToken != null && cancellationToken!.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }
                        if (mod.type == ModType.engine)
                        {
                            //This is already installed?
                            var isInstalled = Knossos.GetInstalledBuildsList(mod.id)?.Where(b => b.version == mod.version);
                            if (isInstalled == null || isInstalled.Count() == 0)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => FsoBuildsViewModel.Instance?.AddBuildToUi(new FsoBuild(mod)), DispatcherPriority.Background);
                            }
                        }
                        if (mod.type == ModType.tc || mod.type == ModType.mod && (listFS2Override || (mod.parent != "FS2" || mod.parent == "FS2" && Knossos.retailFs2RootFound)))
                        {
                            //This is already installed?
                            var isInstalled = Knossos.GetInstalledModList(mod.id);
                            if (isInstalled == null || isInstalled.Count() == 0)
                            {
                                if (lastMod == null || lastMod.id == mod.id)
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
                                foreach (var intMod in isInstalled)
                                {
                                    int result = SemanticVersion.Compare(intMod.version, mod.version);
                                    if (result >= 0)
                                    {
                                        update = false;
                                        if (result == 0)
                                        {
                                            intMod.inNebula = true;
                                        }
                                    }
                                }
                                if (update)
                                {
                                    MainWindowViewModel.Instance?.MarkAsUpdateAvalible(mod.id);
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.LoadPrivateMods()", ex);
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
            } catch (Exception ex)
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
                        if (mod != null && IsModUpdate(mod))
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
                            if (mod.type == ModType.tc || mod.type == ModType.mod && (listFS2Override || (mod.parent != "FS2" || mod.parent == "FS2" && Knossos.retailFs2RootFound)))
                            {
                                //This is already installed?
                                var isInstalled = Knossos.GetInstalledModList(mod.id);
                                if (isInstalled == null || isInstalled.Count() == 0)
                                {
                                    if (lastMod == null || lastMod.id == mod.id)
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
                                    foreach (var intMod in isInstalled)
                                    {
                                        int result = SemanticVersion.Compare(intMod.version, mod.version);
                                        if (result >= 0)
                                        {
                                            update = false;
                                            if (result == 0)
                                            {
                                                intMod.inNebula = true;
                                            }
                                        }
                                    }
                                    if (update)
                                    {
                                        MainWindowViewModel.Instance?.MarkAsUpdateAvalible(mod.id);
                                    }
                                }
                            }
                        }
                    }
                    fileStream.Close();
                    repoLoaded = true;
                    if (cancellationToken != null)
                    {
                        cancellationToken.Dispose();
                        cancellationToken = null;
                    }
                    return updates;
                }
            }
            catch (TaskCanceledException)
            {
                if (cancellationToken != null)
                {
                    cancellationToken.Dispose();
                    cancellationToken = null;
                }
            }
            catch (Exception ex)
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
            if (cancellationToken != null)
            {
                cancellationToken.Cancel();
            }
        }

        public static async Task<Mod?> GetModData(string id, string version)
        {
            if (privateMods != null && privateMods.Any())
            {
                foreach (Mod mod in privateMods)
                {
                    if (mod != null && mod.id == id && mod.version == version)
                    {
                        return mod;
                    }
                }
            }
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
                    Log.Add(Log.LogSeverity.Error, "Nebula.GetModData()", ex);
                }
                fileStream.Close();
            }
            return null;
        }

        public static async Task<bool> IsModIdInNebula(string id)
        {
            //Use the mod list used for newer versions
            var exist = settings.NewerModsVersions.FirstOrDefault(x => x.Id.ToLower() == id.ToLower());
            if (exist != null)
            {
                return true;
            }
            //If we are logged in check using the api
            if (userIsLoggedIn)
            {
                return !await CheckID(id);
            }
            return false;
        }

        public static async Task<List<Mod>> GetAllModsWithID(string? id)
        {
            var modList = new List<Mod>();
            if (privateMods != null && privateMods.Any())
            {
                foreach (Mod mod in privateMods)
                {
                    if (mod != null && (mod.id == id || id == null))
                    {
                        modList.Add(mod);
                    }
                }
            }
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
                Log.Add(Log.LogSeverity.Information, "Nebula.GetRepoEtag()", "repo.json etag: " + newEtag);
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

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string token { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string reason { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public object[] mods { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public ModMember[] members { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public int[] finished_parts { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public bool done { get; set; }
        }

        private enum ApiMethod
        {
            POST,
            PUT,
            GET
        }

        private static async Task<ApiReply?> ApiCall(string resourceUrl, MultipartFormDataContent? data, bool needsLogIn = false, int timeoutSeconds = 30, ApiMethod method = ApiMethod.POST)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    if (needsLogIn)
                    {
                        if (apiUserToken == null)
                        {
                            await Login();
                            if (apiUserToken == null)
                            {
                                Log.Add(Log.LogSeverity.Warning, "Nebula.ApiCall", "An api call that needed a login token was requested, but we were unable to log into the nebula service.");
                                return null;
                            }
                        }
                        client.DefaultRequestHeaders.Add("X-KN-TOKEN", apiUserToken);
                    }
                    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                    switch (method)
                    {
                        case ApiMethod.POST:
                            {
                                if (data == null)
                                {
                                    throw new ArgumentNullException(nameof(data));
                                }
                                var response = await client.PostAsync(apiURL + resourceUrl, data);
                                data.Dispose();
                                if (response.IsSuccessStatusCode)
                                {
                                    var jsonReply = await response.Content.ReadAsStringAsync();
                                    if (jsonReply == "OK") // multiupload/part hack
                                    {
                                        var reply = new ApiReply();
                                        reply.result = true;
                                        return reply;
                                    }
                                    if (jsonReply != null)
                                    {
                                        var reply = JsonSerializer.Deserialize<ApiReply>(jsonReply);
                                        if (!reply.result)
                                            Log.Add(Log.LogSeverity.Error, "Nebula.ApiCall", "An error has ocurred during nebula api POST call: " + response.StatusCode + "\n" + data);

                                        return reply;
                                    }
                                }
                                Log.Add(Log.LogSeverity.Error, "Nebula.ApiCall", "An error has ocurred during nebula api POST call: " + response.StatusCode);
                            }
                            break;
                        case ApiMethod.PUT:
                            {
                                throw new NotImplementedException();
                            }
                        case ApiMethod.GET:
                            {
                                var response = await client.GetAsync(apiURL + resourceUrl);
                                if (response.IsSuccessStatusCode)
                                {
                                    var json = await response.Content.ReadAsStringAsync();
                                    if (json != null)
                                    {
                                        var reply = JsonSerializer.Deserialize<ApiReply>(json);
                                        if (!reply.result)
                                            Log.Add(Log.LogSeverity.Error, "Nebula.ApiCall", "An error has ocurred during nebula api GET call: " + response.StatusCode + "\n" + json);

                                        return reply;
                                    }
                                }
                                Log.Add(Log.LogSeverity.Error, "Nebula.ApiCall", "An error has ocurred during nebula api GET call: " + response.StatusCode);
                            }
                            break;
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
                var data = new MultipartFormDataContent()
                {
                    { new StringContent(logString), "log" }
                };
                var reply = await ApiCall("log/upload", data);
                if (reply.HasValue)
                {
                    Log.Add(Log.LogSeverity.Information, "Nebula.UploadLog", "Uploaded log file to Nebula: " + nebulaURL + "log/" + reply.Value.id);
                    SysInfo.OpenBrowserURL(nebulaURL + "log/" + reply.Value.id);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.UploadLog", ex);
            }
            return false;
        }

        public static async Task<bool> ReportMod(Mod mod, string reason)
        {
            try
            {
                var data = new MultipartFormDataContent()
                {
                    { new StringContent(mod.id), "mid" },
                    { new StringContent(mod.version), "version" },
                    { new StringContent(reason), "message" }
                };
                var reply = await ApiCall("mod/release/report", data, true);
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

        public static async Task<bool> Login(string? user = null, string? password = null)
        {
            try
            {
                if (apiUserToken != null)
                {
                    //Already logged in
                    return true;
                }
                if (user == null)
                    user = settings.user;
                if (password == null && settings.pass != null)
                    password = SysInfo.DIYStringDecryption(settings.pass);

                if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
                {
                    Log.Add(Log.LogSeverity.Warning, "Nebula.Login", "User or Password was null or empty.");
                    return false;
                }

                var data = new MultipartFormDataContent()
                {
                    { new StringContent(user), "user" },
                    { new StringContent(password), "password" }
                };
                var reply = await ApiCall("login", data);
                if (!reply.HasValue || string.IsNullOrEmpty(reply.Value.token))
                {
                    Log.Add(Log.LogSeverity.Warning, "Nebula.Login", "Nebula login failed.");
                    settings.logged = false;
                    apiUserToken = null;
                    SaveSettings();
                    return false;
                }
                else
                {
                    var encryptedPassword = SysInfo.DIYStringEncryption(password);
                    if (settings.user != user || settings.pass != encryptedPassword || !settings.logged)
                    {
                        settings.user = user;
                        settings.pass = encryptedPassword;
                        settings.logged = true;
                        SaveSettings();
                    }
                    apiUserToken = reply.Value.token;
                    Log.Add(Log.LogSeverity.Information, "Nebula.Login", "Login successful, we got a token!");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.Login", ex);
            }
            return false;
        }

        public static async Task<string> Register(string user, string password, string email)
        {
            try
            {
                var data = new MultipartFormDataContent()
                {
                    { new StringContent(user), "name" },
                    { new StringContent(password), "password" },
                    { new StringContent(email), "email" }
                };
                var reply = await ApiCall("register", data);
                if (reply.HasValue)
                {
                    if (reply.Value.result)
                    {
                        Log.Add(Log.LogSeverity.Information, "Nebula.Register", "Registered new user to nebula.");
                        return "ok";
                    }
                    else
                    {
                        Log.Add(Log.LogSeverity.Information, "Nebula.Register", "Error registering new user to nebula. Reason: " + reply.Value.reason);
                        return reply.Value.reason;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.Register", ex);
            }
            return "unknown error";
        }

        public static async Task<string> Reset(string user)
        {
            try
            {
                var data = new MultipartFormDataContent()
                {
                    { new StringContent(user), "user" }
                };
                var reply = await ApiCall("reset_password", data);
                if (reply.HasValue)
                {
                    if (reply.Value.result)
                    {
                        Log.Add(Log.LogSeverity.Information, "Nebula.Reset", "Requested password reset to nebula for username: " + user);
                        return "ok";
                    }
                    else
                    {
                        Log.Add(Log.LogSeverity.Information, "Nebula.Reset", "Error requesting password reset. Reason: " + reply.Value.reason);
                        return reply.Value.reason;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.Reset", ex);
            }
            return "unknown error";
        }

        public static void LogOff()
        {
            settings.logged = false;
            apiUserToken = null;
            settings.user = null;
            settings.pass = null;
            SaveSettings();
        }

        /// <summary>
        /// Checks if the MODID is avalible in Nebula
        /// (Needs to be logged in)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="title"></param>
        /// <returns>
        /// true if MODID is avalible, false if it is already in use
        /// </returns>
        public static async Task<bool> CheckID(string id, string title = "None")
        {
            try
            {
                var data = new MultipartFormDataContent()
                {
                    { new StringContent(id), "id" },
                    { new StringContent(title), "title" }
                };
                //TODO: A potencially huge problem, for Nebula, modid is case sensitive!
                var reply = await ApiCall("mod/check_id", data, true);
                if (reply.HasValue)
                {
                    Log.Add(Log.LogSeverity.Information, "Nebula.CheckID", "Check Mod ID:" + id + " is avalible in Nebula: " + reply.Value.result);
                    return reply.Value.result;
                }
                else
                {
                    Log.Add(Log.LogSeverity.Error, "Nebula.CheckID", "Unable to check mod in in Nebula.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.CheckID", ex);
            }
            return false;
        }

        /// <summary>
        /// Checks if the user has write permissions to a modid
        /// </summary>
        /// <param name="id"></param>
        /// <returns>true if user has write access, false if not or not logged in</returns>
        public static async Task<bool> IsModEditable(string id)
        {
            try
            {
                var data = new MultipartFormDataContent()
                {
                    { new StringContent(id), "mid" }
                };

                var reply = await ApiCall("mod/is_editable", data, true);
                if (reply.HasValue)
                {
                    Log.Add(Log.LogSeverity.Information, "Nebula.IsModEditable", "Is mod id:" + id + " editable? " + reply.Value.result);
                    return reply.Value.result;
                }
                else
                {
                    Log.Add(Log.LogSeverity.Error, "Nebula.IsModEditable", "Unable to check if mod is editable.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.IsModEditable", ex);
            }
            return false;
        }

        /// <summary>
        /// Get an array of mods that the user has write access to.
        /// </summary>
        /// <returns>An array of Mods or null</returns>
        public static async Task<string[]?> GetEditableModIDs()
        {
            try
            {
                var reply = await ApiCall("mod/editable", null, true, 30, ApiMethod.GET);
                if (reply.HasValue)
                {
                    if (reply.Value.mods != null && reply.Value.mods.Any())
                    {
                        Log.Add(Log.LogSeverity.Information, "Nebula.GetEditableMods", "Editable mod in Nebula: " + string.Join(", ", reply.Value.mods));
                        return reply.Value.mods.Select(s => s.ToString()).ToArray()!;
                    }
                    else
                    {
                        Log.Add(Log.LogSeverity.Information, "Nebula.GetEditableMods", "Returned an empty array.");
                    }
                }
                else
                {
                    Log.Add(Log.LogSeverity.Error, "Nebula.GetEditableMods", "Unable to check editable mod list.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.GetEditableMods", ex);
            }
            return null;
        }

        /// <summary>
        /// Get an array of private mods that the user has access to.
        /// </summary>
        /// <returns>An array of mods</returns>
        public static async Task<Mod[]?> GetPrivateMods()
        {
            try
            {
                var reply = await ApiCall("mod/list_private", null, true, 30, ApiMethod.GET);
                if (reply.Value.mods != null && reply.Value.mods.Any())
                {
                    foreach (Mod mod in reply.Value.mods)
                    {
                        Log.Add(Log.LogSeverity.Information, "Nebula.GetPrivateMods", "Private mod in Nebula with access: " + mod);
                    }
                    return reply.Value.mods.Select(s => (Mod)s).ToArray()!;
                }
                else
                {
                    Log.Add(Log.LogSeverity.Information, "Nebula.GetPrivateMods", "Returned an empty array.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.GetPrivateMods", ex);
            }
            return null;
        }

        /// <summary>
        /// Gets all members in a mod
        /// Needs to be logged in
        /// </summary>
        /// <param name="modid"></param>
        /// <returns>An ModMember[] array or null if failed.</returns>
        public static async Task<ModMember[]?> GetTeamMembers(string modid)
        {
            try
            {
                var data = new MultipartFormDataContent()
                {
                    { new StringContent(modid), "mid" }
                };

                var reply = await ApiCall("mod/team/fetch", data, true);
                if (reply.HasValue)
                {
                    if (reply.Value.result)
                    {
                        if (reply.Value.members != null)
                        {
                            var members = string.Empty;
                            foreach (var item in reply.Value.members)
                            {
                                members += item.user + "(" + item.role.ToString() + ") ";
                            }
                            Log.Add(Log.LogSeverity.Information, "Nebula.GetTeamMembers", "Mod id: " + modid + " members: " + members);
                        }
                    }
                    else
                    {
                        Log.Add(Log.LogSeverity.Error, "Nebula.GetTeamMembers", "Error while getting mod members: " + reply.Value.reason);
                    }
                    return reply.Value.members;
                }
                else
                {
                    Log.Add(Log.LogSeverity.Error, "Nebula.GetTeamMembers", "Unable to check if mod team members.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.GetTeamMembers", ex);
            }
            return null;
        }

        /// <summary>
        /// Updates mod team members, the entire list must be passed here, owner included.
        /// </summary>
        /// <param name="modid"></param>
        /// <param name="members"></param>
        /// <returns>"ok" if successfull, a reason if we were unable to update members or null if the call failed</returns>
        public static async Task<string?> UpdateTeamMembers(string modid, ModMember[] members)
        {
            try
            {
                var newMembers = JsonSerializer.Serialize(members);
                var data = new MultipartFormDataContent()
                {
                    { new StringContent(modid), "mid" },
                    { new StringContent(newMembers), "members" }
                };

                var reply = await ApiCall("mod/team/update", data, true);
                if (reply.HasValue)
                {
                    if (reply.Value.result)
                    {
                        Log.Add(Log.LogSeverity.Information, "Nebula.UpdateTeamMembers", "Updated mod members for mod: " + modid + " New Members: " + newMembers);
                        return "ok";
                    }
                    else
                    {
                        Log.Add(Log.LogSeverity.Error, "Nebula.UpdateTeamMembers", "Error while updating mod members: " + reply.Value.reason + " Passed members: " + newMembers);
                        return reply.Value.reason;
                    }
                }
                else
                {
                    Log.Add(Log.LogSeverity.Error, "Nebula.UpdateTeamMembers", "Unable to update mod team members.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.UpdateTeamMembers", ex);
            }
            return null;
        }

        /// <summary>
        /// Deletes a given Mod Version release from Nebula
        /// Needs to be logged in and have write permissions
        /// </summary>
        /// <param name="mod"></param>
        /// <returns>returns "ok" if successfull, a reason if we were unable to delete the mod version or null if the call failed</returns>
        public static async Task<string?> DeleteModVersion(Mod mod)
        {
            try
            {
                var data = new MultipartFormDataContent()
                {
                    { new StringContent(mod.id), "mid" },
                    { new StringContent(mod.version), "version" }
                };

                var reply = await ApiCall("mod/release/delete", data, true);
                if (reply.HasValue)
                {
                    if (reply.Value.result)
                    {
                        Log.Add(Log.LogSeverity.Information, "Nebula.DeleteModVersion", "Deleted mod/version : " + mod + " from Nebula.");
                        return "ok";
                    }
                    else
                    {
                        Log.Add(Log.LogSeverity.Error, "Nebula.DeleteModVersion", "Error while deleting mod/version: " + mod + " Reason: " + reply.Value.reason);
                        return reply.Value.reason;
                    }
                }
                else
                {
                    Log.Add(Log.LogSeverity.Error, "Nebula.DeleteModVersion", "Unable to delete mod version. ");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.DeleteModVersion", ex);
            }
            return null;
        }

        /// <summary>
        /// Checks if a file is already uploaded to Nebula
        /// Checksum or contentChecksum must be provided
        /// TODO: Find out the difference
        /// </summary>
        /// <param name="checksum"></param>
        /// <param name="contentChecksum"></param>
        /// <returns>true if it exist, false otherwise</returns>
        public static async Task<bool> IsFileUploaded(string? checksum = null, string? contentChecksum = null)
        {
            try
            {
                if (checksum == null && contentChecksum == null)
                    throw new ArgumentNullException(nameof(checksum) + " " + nameof(contentChecksum));

                var data = new MultipartFormDataContent();
                if (checksum != null)
                {
                    data.Add(new StringContent(checksum), "checksum");
                }
                else
                {
                    data.Add(new StringContent(contentChecksum!), "content_checksum");
                }

                var reply = await ApiCall("upload/check", data, true);
                if (reply.HasValue)
                {
                    Log.Add(Log.LogSeverity.Information, "Nebula.IsFileUploaded", "Is file: " + checksum + contentChecksum + " uploaded? " + reply.Value.result);
                    return reply.Value.result;
                }
                else
                {
                    Log.Add(Log.LogSeverity.Error, "Nebula.IsFileUploaded", "Unable to check if a file is uploaded");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.IsFileUploaded", ex);
            }
            return false;
        }

        /// <summary>
        /// Uploads an image file to Nebula
        /// It also checks if the image is already uploaded
        /// 10MB max limit
        /// </summary>
        /// <param name="path"></param>
        /// <returns>true if the operation was successfull, false otherwise</returns>
        public static async Task<bool> UploadImage(string path)
        {
            try
            {
                if (!File.Exists(path))
                    throw new Exception("File " + path + " dosent exist.");

                using (FileStream? file = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    if (file.Length > 10485760)
                        throw new Exception("File " + path + " is over the 10MB limit.");

                    var checksumString = string.Empty;
                    using (SHA256 checksum = SHA256.Create())
                    {
                        checksumString = BitConverter.ToString(await checksum.ComputeHashAsync(file)).Replace("-", String.Empty).ToLower();
                    }

                    if (await IsFileUploaded(checksumString))
                    {
                        //Already uploaded
                        return true;
                    }

                    file.Seek(0, SeekOrigin.Begin);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        file.CopyTo(ms);
                        var imgBytes = ms.ToArray();

                        var data = new MultipartFormDataContent()
                        {
                            { new StringContent(checksumString), "checksum" },
                            { new ByteArrayContent(imgBytes, 0, imgBytes.Length), "file", "upload" }
                        };

                        var reply = await ApiCall("upload/file", data, true);
                        if (reply.HasValue)
                        {
                            if (reply.Value.result)
                            {
                                Log.Add(Log.LogSeverity.Information, "Nebula.UploadImage", "File: " + path + " uploaded to Nebula with checksum: " + checksumString);
                            }
                            else
                            {
                                Log.Add(Log.LogSeverity.Error, "Nebula.UploadImage", "Unable to upload File: " + path + " to Nebula. Reason: " + reply.Value.reason);
                            }
                            return reply.Value.result;
                        }
                        else
                        {
                            Log.Add(Log.LogSeverity.Error, "Nebula.UploadImage", "Unable upload image file: " + path);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "Nebula.UploadImage", ex);
            }
            return false;
        }

        /* Multipart Uploader */
        #region MultiPartUploader
        public class MultipartUploader : IDisposable
        {
            private bool disposedValue;
            private static readonly int maxUploadParallelism = 3;
            private static readonly int maxUploadRetries = 3;
            private static readonly long partMaxSize = 10485760;
            private List<FilePart> fileParts = new List<FilePart>();
            private CancellationTokenSource cancellationTokenSource;
            private FileStream fs;
            private Action<string>? progressCallback;
            private string? fileChecksum;
            private Queue<object> readQueue = new Queue<object>();
            private long fileLenght = 0;
            private bool completed = false;
            private bool verified = false;

            public MultipartUploader(string filePath, CancellationTokenSource? cancellationTokenSource, Action<string>? progressCallback)
            {
                this.progressCallback = progressCallback;
                if (cancellationTokenSource != null)
                {
                    this.cancellationTokenSource = cancellationTokenSource;
                }
                else
                {
                    this.cancellationTokenSource = new CancellationTokenSource();
                }
                fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                fs.Seek(0, SeekOrigin.Begin);
                fileLenght = fs.Length;
                var partIdx = 0;
                while(partIdx * partMaxSize < fs.Length)
                {
                    var partLengh = fs.Length - ( partIdx * partMaxSize ) < partMaxSize ? fs.Length - (partIdx * partMaxSize) : partMaxSize;
                    fileParts.Add(new FilePart(this, partIdx, partIdx * partMaxSize, partLengh));
                    partIdx++;
                }
            }

            public async Task<bool> Upload()
            {
                if (cancellationTokenSource.IsCancellationRequested)
                    throw new TaskCanceledException();

                if (progressCallback != null)
                    progressCallback.Invoke("Starting Checkup...");

                var start = await Start();

                if(!start)
                {
                    if(progressCallback != null)
                        progressCallback.Invoke("Error while getting starting response.");
                    return false;
                }
                
                //Upload was completed previously?
                if(completed)
                {
                    if (progressCallback != null)
                        progressCallback.Invoke("Already uploaded.");
                    return true;
                }

                await Parallel.ForEachAsync(fileParts, new ParallelOptions { MaxDegreeOfParallelism = maxUploadParallelism }, async (part, token) =>
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                        throw new TaskCanceledException();
                    if (progressCallback != null)
                        progressCallback.Invoke("Uploading part id: " + part.GetID());
                    var partCompleted = false;
                    var attempt = 1;
                    do
                    {
                        var result = await part.Upload();
                        if(result)
                        {
                            if (progressCallback != null)
                                progressCallback.Invoke("Verifying part id: " + part.GetID());
                            partCompleted = await part.Verify();
                        }
                    } 
                    while ( partCompleted != true && attempt++ <= maxUploadRetries);

                    if (partCompleted)
                    {
                        if (progressCallback != null)
                            progressCallback.Invoke("Part uploaded. ID: " + part.GetID());
                    }
                    else
                    {
                        cancellationTokenSource.Cancel();
                    }
                });

                completed = true;

                if (progressCallback != null)
                    progressCallback.Invoke("Verifying Upload...");

                if (cancellationTokenSource.IsCancellationRequested)
                    throw new TaskCanceledException();

                verified = await Finish();

                if (progressCallback != null)
                    progressCallback.Invoke("Verify: " + verified);

                return verified;
            }

            private async Task<bool> Finish()
            {
                var data = new MultipartFormDataContent()
                {
                    { new StringContent(fileChecksum!), "id" },
                    { new StringContent(fileChecksum!), "checksum" },
                    { new StringContent("None"), "content_checksum" },
                    { new StringContent("None"), "vp_checksum" }
                };

                var reply = await ApiCall("multiupload/finish", data, true);
                if (reply.HasValue)
                {
                    if (!reply.Value.result)
                    {
                        Log.Add(Log.LogSeverity.Error, "MultipartUploader.Finish", "Unable to multi part upload process to Nebula. Reason: " + reply.Value.reason);
                    }
                    else
                    {
                        Log.Add(Log.LogSeverity.Information, "MultipartUploader.Finish", "Multiupload: File uploaded to Nebula! " + fs.Name);
                    }
                    completed = reply.Value.result;
                    return reply.Value.result;
                }
                return false;
            }

            private async Task<bool> Start()
            {
                await GetChecksum();
                var data = new MultipartFormDataContent()
                {
                    { new StringContent(fileChecksum!), "id" },
                    { new StringContent(fileLenght.ToString()), "size" },
                    { new StringContent(fileParts.Count().ToString()), "parts" }
                };

                var reply = await ApiCall("multiupload/start", data, true);
                if (reply.HasValue)
                {
                    if (!reply.Value.result)
                    {
                        Log.Add(Log.LogSeverity.Error, "MultipartUploader.Start", "Unable to multi part upload process to Nebula. Reason: " + reply.Value.reason);
                    }
                    completed = reply.Value.done;
                    if(!completed && reply.Value.finished_parts != null)
                    {
                        foreach(var finished in reply.Value.finished_parts)
                        {
                            foreach (var part in fileParts.ToList())
                            {
                                if(part.GetID() == finished)
                                {
                                    fileParts.Remove(part);
                                    Log.Add(Log.LogSeverity.Information, "MultipartUploader.Start", "Removing part id " + part.GetID() + " because it was already uploaded.");
                                }
                            }
                        }
                    }
                    return reply.Value.result;
                }
                return false;
            }

            internal async Task<byte[]> ReadBytes(long offset, long length)
            {
                var queueObject = new object();
                readQueue.Enqueue(queueObject);
                while(readQueue.Peek() != queueObject)
                {
                    await Task.Delay(100);
                }
                fs.Seek(offset, SeekOrigin.Begin);
                var buffer = new byte[length];
                var numberReadBytes = await fs.ReadAsync(buffer, 0, buffer.Length);
                readQueue.Dequeue();
                if (numberReadBytes != length)
                    throw new Exception(" We wanted to read " + length + " bytes, but we read " + buffer.Length + " bytes.");
                return buffer;
            }

            internal async Task<string?> GetChecksum()
            {
                if(fileChecksum != null)
                {
                    return fileChecksum;
                }
                else
                {
                    var queueObject = new object();
                    readQueue.Enqueue(queueObject);
                    while (readQueue.Peek() != queueObject)
                    {
                        await Task.Delay(100);
                    }
                    using (SHA256 sha256 = SHA256.Create())
                    {
                        fs.Seek(0, SeekOrigin.Begin);
                        fileChecksum = BitConverter.ToString(await sha256.ComputeHashAsync(fs)).Replace("-", String.Empty).ToLower();
                        readQueue.Dequeue();
                        return fileChecksum;
                    }
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        fs.Close();
                        fs.Dispose();
                    }

                    disposedValue = true;
                }
            }
        }

        private class FilePart
        {
            private MultipartUploader uploader;
            private string? fileID;
            private byte[]? partBytes;
            private string? partChecksum;
            private long fileOffset;
            private long partLength;
            private int partIndex = 0;
            private bool verified = false;
            private bool completed = false;

            public FilePart(MultipartUploader uploader, int partID, long offset, long partLength)
            {
                this.partIndex = partID;
                this.uploader = uploader;
                this.partLength = partLength;
                fileOffset = offset;
            }

            private async Task ReadData()
            {
                if (partBytes == null)
                {
                    partBytes = await uploader.ReadBytes(fileOffset, partLength);
                    if(partBytes.Length != partLength)
                        throw new Exception("Expected to read " + partLength + " bytes, but we read " + partBytes.Length + " instead.");
                }
            }

            public async Task<bool> Upload()
            {
                completed = false;
                if(fileID == null)
                {
                    fileID = await uploader.GetChecksum();
                }

                if (fileID != null)
                {
                    await ReadData();

                    if (partBytes != null)
                    {
                        var data = new MultipartFormDataContent()
                        {
                            { new StringContent(fileID), "id" },
                            { new StringContent(partIndex.ToString()), "part" },
                            { new ByteArrayContent(partBytes, 0, partBytes.Length), "file", "upload" }
                        };

                        var reply = await ApiCall("multiupload/part", data, true);
                        if (reply.HasValue)
                        {
                            if (!reply.Value.result)
                            {
                                Log.Add(Log.LogSeverity.Error, "FilePart.Upload", "Unable to upload file part to Nebula. Reason: " + reply.Value.reason);
                            }
                            completed = reply.Value.result;
                            return reply.Value.result;
                        }
                    }
                }
                Log.Add(Log.LogSeverity.Error, "FilePart.Upload", "Unable upload file part.");
                return false;
            }

            public int GetID()
            {
                return partIndex;
            }

            public async Task<bool> Verify()
            {
                if (!completed)
                    return false;
                if (fileID == null)
                    fileID = await uploader.GetChecksum();

                await GetChecksum();
                if (fileID != null && partChecksum != null)
                {
                    var data = new MultipartFormDataContent()
                    {
                        { new StringContent(fileID), "id" },
                        { new StringContent(partIndex.ToString()), "part" },
                        { new StringContent(partChecksum), "checksum" },
                    };

                    var reply = await ApiCall("multiupload/verify_part", data, true);
                    if (reply.HasValue)
                    {
                        if (!reply.Value.result)
                        {
                            Log.Add(Log.LogSeverity.Error, "FilePart.Verify", "Unable to verify file part. Reason: " + reply.Value.reason);
                        }
                        verified = reply.Value.result;
                        return reply.Value.result;
                    }
                }
                Log.Add(Log.LogSeverity.Error, "FilePart.Verify", "Unable to verify file part.");
                return false;
            }

            public async Task<string> GetChecksum()
            {
                if(partChecksum == null)
                {
                    if(partBytes == null)
                    {
                        await ReadData();
                    }
                }
                else
                {
                    return partChecksum;
                }
                if (partBytes != null)
                {
                    using (SHA256 sha256 = SHA256.Create())
                    {
                        partChecksum = BitConverter.ToString(sha256.ComputeHash(partBytes)).Replace("-", String.Empty).ToLower();
                        return partChecksum;
                    }
                }
                else
                {
                    throw new Exception("The byte array was null while attemping to generate a sha256 hash.");
                }
            }

            public bool IsCompleted()
            {
                return completed;
            }

            public bool IsVerified()
            { 
                return verified; 
            }
        }
        #endregion
        #endregion
    }
}
