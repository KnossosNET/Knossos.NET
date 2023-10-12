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
                if (cancellationToken != null && cancellationToken!.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                if (userIsLoggedIn)
                {
                    await LoadPrivateMods(cancellationToken);
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
            }catch(Exception ex)
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
                                        int result = SemanticVersion.Compare(intMod.version, mod.version);
                                        if (result >= 0)
                                        {
                                            update = false;
                                            if(result == 0)
                                            {
                                                intMod.inNebula = true;
                                            }
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
            if(privateMods != null && privateMods.Any())
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
            if ( exist != null )
            {
                return true;
            }
            //If we are logged in check using the api
            if( userIsLoggedIn )
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
                if(!File.Exists(filename))
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

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string token { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string reason { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public object[] mods { get; set; }
        }

        private enum ApiMethod
        {
            POST,
            PUT,
            GET
        }

        private static async Task<ApiReply?> ApiCall(string resourceUrl, Dictionary<string, string>? keyValues, bool needsLogIn = false, int timeoutSeconds = 30, ApiMethod method = ApiMethod.POST) 
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
                            if(keyValues == null)
                            {
                                throw new ArgumentNullException(nameof(keyValues));
                            }
                            using (FormUrlEncodedContent content = new FormUrlEncodedContent(keyValues))
                            {
                                var response = await client.PostAsync(apiURL + resourceUrl, content);
                                if (response.IsSuccessStatusCode)
                                {
                                    var json = await response.Content.ReadAsStringAsync();
                                    if (json != null) 
                                    { 
                                        var reply = JsonSerializer.Deserialize<ApiReply>(json);
                                        if (!reply.result)
                                            Log.Add(Log.LogSeverity.Error, "Nebula.ApiCall", "An error has ocurred during nebula api POST call: " + response.StatusCode + "\n" + json);

                                        return reply;
                                    }
                                }
                                Log.Add(Log.LogSeverity.Error, "Nebula.ApiCall", "An error has ocurred during nebula api POST call: " + response.StatusCode);
                            }
                        } break;
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
                        } break;
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
                    SysInfo.OpenBrowserURL(nebulaURL + "log/" + reply.Value.id);
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
                if(apiUserToken != null)
                {
                    //Already logged in
                    return true;
                }
                if(user == null)
                    user = settings.user;
                if (password == null && settings.pass != null)
                    password = SysInfo.DIYStringDecryption(settings.pass);

                if(string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
                {
                    Log.Add(Log.LogSeverity.Warning, "Nebula.Login", "User or Password was null or empty.");
                    return false;
                }

                var data = new Dictionary<string, string>()
                {
                    { "user", user },
                    { "password", password }
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
                var data = new Dictionary<string, string>()
                {
                    { "name", user },
                    { "password", password },
                    { "email", email }
                };
                var reply = await ApiCall("register", data);
                if (reply.HasValue)
                {
                    if( reply.Value.result )
                    {
                        Log.Add(Log.LogSeverity.Information, "Nebula.Register", "Registered new user to nebula.");
                        return "ok";
                    }
                    else
                    {
                        Log.Add(Log.LogSeverity.Information, "Nebula.Register", "Error registering new user to nebula. Reason: "+ reply.Value.reason);
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
                var data = new Dictionary<string, string>()
                {
                    { "user", user }
                };
                var reply = await ApiCall("reset_password", data);
                if (reply.HasValue)
                {
                    if (reply.Value.result)
                    {
                        Log.Add(Log.LogSeverity.Information, "Nebula.Reset", "Requested password reset to nebula for username: "+user);
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
                var data = new Dictionary<string, string>()
                {
                    { "id", id },
                    { "title", title }
                };
                //TODO: A potencially huge problem, for Nebula, modid is case sensitive!
                var reply = await ApiCall("mod/check_id", data, true);
                if (reply.HasValue)
                {
                    Log.Add(Log.LogSeverity.Information, "Nebula.CheckID", "Check Mod ID:" + id +" is avalible in Nebula: " + reply.Value.result);
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
                var data = new Dictionary<string, string>()
                {
                    { "mid", id }
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
                    foreach(Mod mod in reply.Value.mods)
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
        #endregion
    }
}
