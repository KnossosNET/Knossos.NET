using Knossos.NET.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Knossos.NET.Models
{
    /* Using enums (and a large number of switch() as a result), in general must be avoided if possible. But they will do for now. */
    public enum FsoExecType
    {
        Unknown,
        Release,
        Debug,
        Fred2,
        Fred2Debug,
        QtFred,
        QtFredDebug
    }

    public enum FsoExecArch
    {
        x86,
        x64,
        x86_avx,
        x64_avx,
        x86_avx2,
        x64_avx2,
        arm32,
        arm64,
        other
    }

    public enum FsoExecEnvironment
    {
        Windows,
        Linux,
        MacOSX,
        Unknown
    }

    public enum FsoStability
    {
        Stable,
        RC,
        Nightly,
        Custom
    }

    public class FsoBuild
    {
        public string id;
        public string? title;
        public string? description;
        public string version;
        public FsoStability stability;
        public List<FsoFile> executables = new List<FsoFile>();
        public string folderPath;
        public string? date = string.Empty;
        public string? directExec = null;
        public bool isInstalled = true;
        public bool devMode = false;
        public Mod? modData; 
        /*
         Direct Exe
         */
        public FsoBuild(string directExecpath)
        {
            id = "DirectExec";
            title = "DirectExec";
            try
            {
                var parts = directExecpath.Split(Path.DirectorySeparatorChar);
                if (parts.Length > 1)
                {
                    var filename = parts[parts.Count() - 1];
                    parts = filename.Split('_');
                    if (parts.Length > 5)
                    {
                        version = parts[2] + "." + parts[3] + "." + parts[4] + "-" + SysInfo.GetTimestamp(DateTime.Now);
                    }
                    else
                    {
                        version = "99.99.99-" + SysInfo.GetTimestamp(DateTime.Now);
                    }
                }
                else
                {
                    version = "99.99.99-" + SysInfo.GetTimestamp(DateTime.Now);
                }
            }
            catch
            {
                version = "99.99.99-" + SysInfo.GetTimestamp(DateTime.Now);
            }
            try
            {
                FileInfo fi = new FileInfo(directExecpath);
                if(fi != null && fi.Directory != null)
                    folderPath = fi.Directory.FullName;
                else
                    folderPath = directExecpath;
            }
            catch
            {
                Log.Add(Log.LogSeverity.Error,"FsoBuid()","Unable to determine file folder for "+ directExecpath);
                folderPath = directExecpath;
            }
            stability = FsoStability.Custom;
            date = DateTime.Now.ToString();
            directExec = directExecpath;
        }
        /*
         Installed/nebula builds
         */
        public FsoBuild(Mod modJson)
        {
            if (modJson.fullPath == string.Empty)
            {
                //This is a nebula build
                isInstalled = false;
            }
            id = modJson.id;
            title = modJson.title;
            devMode = modJson.devMode;
            if(devMode)
            {
                modData = modJson;
            }
            description = modJson.description;
            version = modJson.version;
            folderPath = modJson.fullPath;
            stability = GetFsoStability(modJson.stability, modJson.id);
            date = modJson.lastUpdate;
            LoadExecutables(modJson);
        }

        public void UpdateBuildData(Mod modJson)
        {
            if (modJson.fullPath == string.Empty)
            {
                //This is a nebula build
                isInstalled = false;
            }
            id = modJson.id;
            title = modJson.title;
            devMode = modJson.devMode;
            if (devMode)
            {
                modData = modJson;
            }
            description = modJson.description;
            version = modJson.version;
            folderPath = modJson.fullPath;
            stability = GetFsoStability(modJson.stability, modJson.id);
            date = modJson.lastUpdate;
            executables.Clear();
            LoadExecutables(modJson);
        }

        public FlagsJsonV1? GetFlagsV1()
        {
            var fullpath = GetExec(FsoExecType.Release);
            if (fullpath == null)
            {
                fullpath = GetExec(FsoExecType.Debug);
            }
            if (fullpath == null)
            {
                Log.Add(Log.LogSeverity.Error, "FsoBuild.GetFlags()", "Unable to find a valid executable for this build: " + this.ToString());
                return null;
            }
            Log.Add(Log.LogSeverity.Information, "FsoBuild.GetFlags()", "Getting FSO Flags from file: " + fullpath);

            if(SysInfo.IsLinux || SysInfo.IsMacOS)
            {
                SysInfo.Chmod(fullpath,"+x");
            }

            string output = string.Empty;
            try
            {
                var cmd = new Process();
                cmd.StartInfo.FileName = fullpath;
                cmd.StartInfo.Arguments = "-get_flags json_v1";
                cmd.StartInfo.UseShellExecute = false;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.RedirectStandardInput = true;
                cmd.StartInfo.StandardOutputEncoding = new UTF8Encoding(false);
                cmd.StartInfo.WorkingDirectory = folderPath;
                cmd.Start();
                string result = cmd.StandardOutput.ReadToEnd();
                output = result;
                cmd.WaitForExit();
                cmd.Dispose();
                //avoiding the "fso is running in legacy config mode..."
                if(result.Contains("{"))
                {
                    result = result.Substring(result.IndexOf('{'));
                }
                return JsonSerializer.Deserialize<FlagsJsonV1>(result);
            }
            catch (JsonException exJson)
            {
                //json failed try to see if it exported a binary
                if(File.Exists(folderPath+Path.DirectorySeparatorChar+"flags.lch"))
                {
                    Log.Add(Log.LogSeverity.Error, "FsoBuild.GetFlagsV1()", "FSO build "+ this +" seems to be below the minimum supported version (3.8.1) and does not support exporting flags as Json.");
                    File.Delete(folderPath + Path.DirectorySeparatorChar + "flags.lch");
                }
                else
                {
                    Log.Add(Log.LogSeverity.Error, "FsoBuild.GetFlags()", exJson);
                    Log.Add(Log.LogSeverity.Error, "FSO EXE OUTPUT ", output);
                }
                return null;
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "FsoBuild.GetFlags()", ex);
                Log.Add(Log.LogSeverity.Error, "FSO EXE OUTPUT ", output);
                return null;
            }
        }

        /*
            Return the best executable fullpath that is valid for user system and requested type.
            Null if none.
         */
        public string? GetExec(FsoExecType type)
        {
            if (directExec != null)
            {
                return directExec;
            }

            var validExecs = executables.Where(b => b.isValid && b.type == type);

            if(validExecs.Any())
            {
                foreach(var exe in validExecs)
                {
                    if (Knossos.globalSettings.forceSSE2)
                    {
                        if (exe.arch == FsoExecArch.x64 || exe.arch == FsoExecArch.x86)
                        {
                            if (exe.score < 200)
                                exe.score += 200;
                        }
                    }
                    else
                    {
                        if (exe.arch == FsoExecArch.x64 || exe.arch == FsoExecArch.x86)
                        {
                            if (exe.score > 200)
                                exe.score -= 200;
                        }
                    }
                }
                validExecs = validExecs.OrderByDescending(b => b.score);
                return folderPath + validExecs.ToArray()[0].filename;
            }
            return null;
        }


        private void LoadExecutables(Mod modJson)
        {
            if (modJson.packages != null)
            {
                foreach (var package in modJson.packages)
                {
                    FsoExecEnvironment env = GetExecEnvironment(package.environment);
                    if (package.executables != null)
                    {
                        foreach (var exec in package.executables)
                        {
                            if (exec.file != null)
                            {
                                if(!isInstalled && package.environment != null)
                                {
                                    exec.properties = FillProperties(package.environment);
                                }
                                string filename = exec.file;
                                FsoExecType type = GetExecType(exec.label);
                                FsoExecArch arch = GetExecArch(exec.properties);
                                if(modJson.devMode)
                                    executables.Add(new FsoFile(package.folder+Path.DirectorySeparatorChar+exec.file.Replace(@"./",""), folderPath, type, arch, env));
                                else
                                    executables.Add(new FsoFile(exec.file, folderPath, type, arch, env));
                            }
                            else
                            {
                                Log.Add(Log.LogSeverity.Warning, "FsoBuild.LoadExecutables", "One filename in the packages list was null and ignored " + folderPath);
                            }
                        }
                    }
                }
            }
        }

        /*
         Complete mod executable properties if missing from the enviroment
         */
        public static ModProperties FillProperties(string environment)
        {
            var properties = new ModProperties();
            if (environment.ToLower().Contains("arm64"))
            {
                properties.arm64 = true;
            }
            if (environment.ToLower().Contains("arm32"))
            {
                properties.arm32 = true;
            }

            if (!environment.ToLower().Contains("arm32") && !environment.ToLower().Contains("arm64"))
            {
                if (!environment.ToLower().Contains("avx"))
                {
                    properties.sse2 = true;
                }
                if (environment.ToLower().Contains("x86_64") || environment.ToLower().Contains("x64"))
                {
                    properties.x64 = true;
                }
                if (environment.ToLower().Contains("avx2"))
                {
                    properties.avx2 = true;
                }
                else
                {
                    if (environment.ToLower().Contains("avx"))
                    {
                        properties.avx = true;
                    }
                }
            }
            return properties;
        }

        /// <summary>
        /// Determines if the current environment string is valid to download in the current system
        /// Used to determine witch packages to install for each build
        /// </summary>
        /// <param name="enviroment"></param>
        /// <returns>true if valid, false otherwise</returns>
        public static bool IsEnviromentStringValidInstall(string? enviroment)
        {
            if (enviroment == null || enviroment.Trim() == string.Empty)
            {
                return false;
            }

            if (enviroment.ToLower().Contains("windows") && !SysInfo.IsWindows || enviroment.ToLower().Contains("linux") && !SysInfo.IsLinux || enviroment.ToLower().Contains("macosx") && !SysInfo.IsMacOS)
            {
                return false;
            }

            if (enviroment.ToLower().Contains("avx2") && !SysInfo.CpuAVX2 || enviroment.ToLower().Contains("avx") && !SysInfo.CpuAVX)
            {
                return false;
            }

            if (enviroment.ToLower().Contains("x86_64") && SysInfo.CpuArch == "X64")
            {
                return true;
            }
            if (enviroment.ToLower().Contains("arm64") && SysInfo.CpuArch == "Arm64")
            {
                return true;
            }
            if (enviroment.ToLower().Contains("arm32") && (SysInfo.CpuArch == "Armv6" || SysInfo.CpuArch == "Arm"))
            {
                return true;
            }
            if (SysInfo.CpuArch == "X86" && !enviroment.ToLower().Contains("x86_64") && !enviroment.ToLower().Contains("arm64") && !enviroment.ToLower().Contains("arm32"))
            {
                return true;
            }
            if (SysInfo.CpuArch == "X64" && !enviroment.ToLower().Contains("x86_64") && !enviroment.ToLower().Contains("arm64") && !enviroment.ToLower().Contains("arm32"))
            {
                return true;
            }

            if (SysInfo.CpuArch == "Arm64" && (SysInfo.IsMacOS || SysInfo.IsWindows) && !enviroment.ToLower().Contains("arm32") && !enviroment.ToLower().Contains("avx"))
            {
                return true;
            }

            return false;
        }

        /*
            Determine the operating system this build file is compiled for 
        */
        public static FsoExecEnvironment GetExecEnvironment(string? enviroment)
        {
            if (enviroment == null)
            {
                Log.Add(Log.LogSeverity.Warning, "FsoBuild.GetExecEnvironment", "Unable to determine the proper build enviroment. Env: null");
                return FsoExecEnvironment.Unknown;
            }

            if (enviroment.ToLower().Contains("windows"))
                return FsoExecEnvironment.Windows;
            if (enviroment.ToLower().Contains("linux"))
                return FsoExecEnvironment.Linux;
            if (enviroment.ToLower().Contains("mac"))
                return FsoExecEnvironment.MacOSX;

            Log.Add(Log.LogSeverity.Information, "FsoBuild.GetExecEnvironment", "Unable to determine the proper build enviroment. Env: " + enviroment);
            return FsoExecEnvironment.Unknown;
        }

        /*
            Determine the CPU arch this build file is compiled for
        */
        public static FsoExecArch GetExecArch(ModProperties? properties)
        {
            if (properties == null || properties.other)
                return FsoExecArch.other;

            if (properties.arm32)
                return FsoExecArch.arm32;

            if (properties.arm64)
                return FsoExecArch.arm64;

            if (properties.x64)
            {
                if (properties.avx2)
                    return FsoExecArch.x64_avx2;
                else if (properties.avx)
                    return FsoExecArch.x64_avx;
                else
                    return FsoExecArch.x64;
            }
            else
            {
                if (properties.avx2)
                    return FsoExecArch.x86_avx2;
                else if(properties.avx)
                    return FsoExecArch.x86_avx;
                else
                    return FsoExecArch.x86;
            }
        }

        /*
          If label is null, then is Release.
        */
        public static FsoExecType GetExecType(string? label)
        {
            if (label == null)
                return FsoExecType.Release;

            switch (label.ToLower())
            {        
                case "release": return FsoExecType.Release;

                case "debug":
                case "fastdebug":
                case "rollback build": 
                case "fast debug": return FsoExecType.Debug;

                case "fred fastdebug":
                case "fred fast debug":
                case "fred debug":
                case "fred2debug": 
                case "fred2 debug": return FsoExecType.Fred2Debug;

                case "fred":
                case "fred2": return FsoExecType.Fred2;

                case "qtfred fastdebug":
                case "qtfreddebug":
                case "qtfred debug": return FsoExecType.QtFredDebug;

                case "qtfred": return FsoExecType.QtFred;

                default:
                    Log.Add(Log.LogSeverity.Warning, "FsoBuild.GetExecType", "Unable to determine FSO Exec Type. Label: " + label);
                    return FsoExecType.Unknown;
            }
        }

        public static string? GetLabelString(FsoExecType fsoExecType)
        {
            switch (fsoExecType)
            {
                case FsoExecType.Release: return null;
                case FsoExecType.Debug: return "debug";
                case FsoExecType.Fred2: return "fred2";
                case FsoExecType.Fred2Debug: return "fred2 debug";
                case FsoExecType.QtFred: return "qtfred";
                case FsoExecType.QtFredDebug: return "qtfred debug";
            }
            return null;
        }

        /* 
           Official FSO builds from nebula use "FSO" as id, only check stability on those.
           For all others the stability is "custom" since we cant really know.    
        */
        public static FsoStability GetFsoStability(string? stability, string? modId)
        {
            if (modId == null || stability == null)
            {
                Log.Add(Log.LogSeverity.Warning, "FsoBuild.GetFsoStability", "Unable to determine the proper build stability for " + modId);
                return FsoStability.Stable;
            }

            if (modId != "FSO")
                return FsoStability.Custom;

            switch (stability)
            {
                case "stable": return FsoStability.Stable;
                case "rc": return FsoStability.RC;
                case "nightly": return FsoStability.Nightly;

                default:
                    Log.Add(Log.LogSeverity.Warning, "FsoBuild.GetFsoStability", "Unable to determine the proper build stability: " + stability);
                    return FsoStability.Stable;
            }
        }

        public static string GetEnviromentString(FsoExecArch arch, FsoExecEnvironment so)
        {
            string env = so.ToString().ToLower();

            switch(arch)
            {
                case FsoExecArch.x86: env += " && x86"; break;
                case FsoExecArch.x86_avx: env += " && x86 && avx"; break;
                case FsoExecArch.x86_avx2: env += " && x86 && avx2"; break;
                case FsoExecArch.x64: env += " && x86_64"; break;
                case FsoExecArch.x64_avx: env += " && x86_64 && avx"; break;
                case FsoExecArch.x64_avx2: env += " && x86_64 && avx2"; break;
                case FsoExecArch.arm32: env += " && arm32"; break;
                case FsoExecArch.arm64: env += " && arm64"; break;
            }

            return env;
        }

        public override string ToString()
        {
            if(directExec != null)
                return directExec;
            return title + " " + version;
        }

        /* To use with the List .Sort()
           Results are inverted
        */
        public static int CompareDatesAsTimestamp(FsoBuild build1, FsoBuild build2)
        {
            if(build1.date == null && build2.date == null)
                return 0;
            if(build1.date == null)
                return -1;
            if(build2.date == null) 
                return 1;
            return string.Compare(build2.date.Replace("-", "").Trim(), build1.date.Replace("-", "").Trim());
        }

        /* 
         * To use with the List .Sort()
        */
        public static int CompareVersion(FsoBuild build1, FsoBuild build2)
        {
            //inverted
            return SemanticVersion.Compare(build2.version, build1.version);
        }
    }

    public class FsoFile
    {
        public string filename;
        public FsoExecType type;
        public FsoExecArch arch;
        public FsoExecEnvironment env;
        public bool isValid = false;
        internal int score = 0;

        public FsoFile(string filename, string modpath, FsoExecType type, FsoExecArch arch, FsoExecEnvironment env)
        {
            this.filename = filename;
            this.type = type;
            this.arch = arch;
            this.env = env;
            this.score = DetermineScore(modpath);
            if(score > 0)
                isValid = true;
        }

        /*
            Determine FSO File score based on OS and CPU Arch
         */
        private int DetermineScore(string modpath)
        {
            int score = 0;
            /* First the cases that are an instant 0 */
            if (arch == FsoExecArch.other || env == FsoExecEnvironment.Unknown || type == FsoExecType.Unknown)
            {
                if (modpath != string.Empty)
                    Log.Add(Log.LogSeverity.Warning, "FsoFile.DetermineScore", "File: " + modpath + filename + " has an unknown cpu arch, build or enviroment type in json.");
                return 0;
            }

            if (modpath != string.Empty && !File.Exists(modpath + filename))
            {
                if (modpath != string.Empty)
                    Log.Add(Log.LogSeverity.Warning, "FsoFile.DetermineScore", "File: " + modpath + filename + " does not exist!");
                return 0;
            }

            if (env == FsoExecEnvironment.Windows && !SysInfo.IsWindows || env == FsoExecEnvironment.Linux && !SysInfo.IsLinux || env == FsoExecEnvironment.MacOSX && !SysInfo.IsMacOS)
            {
                if (modpath != string.Empty)
                    Log.Add(Log.LogSeverity.Warning, "FsoFile.DetermineScore", "File: " + modpath + filename + " is not valid for this OS. Detected: " + env);
                return 0;
            }

            /* Calculate the score, keep in mind in Windows and MAC x86 can run on x64 and X86/X64 can run on ARM64 */
            /* No support for 32 bits ARM on Windows/Mac, also no support for x86/x64 AVX on Windows ARM */
            if(SysInfo.IsWindows || SysInfo.IsMacOS)
            {
                switch (arch)
                {
                    case FsoExecArch.x64_avx2:
                        switch(SysInfo.CpuArch)
                        {
                            case "X64":
                                score += SysInfo.CpuAVX2 ? 100 : 0;
                                break;
                            case "X86":
                                break;
                            case "Arm64":
                                //score += 50;
                                break;
                            case "Arm":
                            case "Armv6":
                                break;
                        }
                        break;
                    case FsoExecArch.x64_avx:
                        switch (SysInfo.CpuArch)
                        {
                            case "X64":
                                score += SysInfo.CpuAVX ? 90 : 0;
                                break;
                            case "X86":
                                break;
                            case "Arm64":
                                //score += 45;
                                break;
                            case "Arm":
                            case "Armv6":
                                break;
                        }
                        break;
                    case FsoExecArch.x64:
                        switch (SysInfo.CpuArch)
                        {
                            case "X64":
                                score += 80;
                                break;
                            case "X86":
                                break;
                            case "Arm64":
                                score += 60;
                                break;
                            case "Arm":
                            case "Armv6":
                                break;
                        }
                        break;
                    case FsoExecArch.x86_avx2:
                        switch (SysInfo.CpuArch)
                        {
                            case "X64":
                                score += SysInfo.CpuAVX2 ? 50 : 0;
                                break;
                            case "X86":
                                score += SysInfo.CpuAVX2 ? 100 : 0;
                                break;
                            case "Arm64":
                                //score += 25;
                                break;
                            case "Arm":
                            case "Armv6":
                                break;
                        }
                        break;
                    case FsoExecArch.x86_avx:
                        switch (SysInfo.CpuArch)
                        {
                            case "X64":
                                score += SysInfo.CpuAVX ? 45 : 0;
                                break;
                            case "X86":
                                score += SysInfo.CpuAVX ? 90 : 0;
                                break;
                            case "Arm64":
                                //score += 15;
                                break;
                            case "Arm":
                            case "Armv6":
                                break;
                        }
                        break;
                    case FsoExecArch.x86:
                        switch (SysInfo.CpuArch)
                        {
                            case "X64":
                                score += SysInfo.CpuAVX ? 40 : 0;
                                break;
                            case "X86":
                                score += SysInfo.CpuAVX ? 80 : 0;
                                break;
                            case "Arm64":
                                score += 30; 
                                break;
                            case "Arm":
                            case "Armv6":
                                break;
                        }
                        break;
                    case FsoExecArch.arm64:
                        switch (SysInfo.CpuArch)
                        {
                            case "X64":
                                break;
                            case "X86":
                                break;
                            case "Arm64":
                                score += 100;
                                break;
                            case "Arm":
                            case "Armv6":
                                break;
                        }
                        break;
                    case FsoExecArch.arm32:
                        switch (SysInfo.CpuArch)
                        {
                            case "X64":
                                break;
                            case "X86":
                                break;
                            case "Arm64":
                                break;
                            case "Arm":
                            case "Armv6":
                                score += 100;
                                break;
                        }
                        break;
                    default: 
                        Log.Add(Log.LogSeverity.Error, "FsoFile.DetermineScore", "FsoFile.DetermineScore() is missing the case for: " + arch);
                        break;
                }
            }
            else
            {
                //Linux
                switch (arch)
                {
                    case FsoExecArch.x64_avx2:
                        switch (SysInfo.CpuArch)
                        {
                            case "X64":
                                score += SysInfo.CpuAVX2 ? 100 : 0;
                                break;
                            case "X86":
                                break;
                            case "Arm64":
                                break;
                            case "Arm":
                            case "Armv6":
                                break;
                        }
                        break;
                    case FsoExecArch.x64_avx:
                        switch (SysInfo.CpuArch)
                        {
                            case "X64":
                                score += SysInfo.CpuAVX ? 90 : 0;
                                break;
                            case "X86":
                                break;
                            case "Arm64":
                                break;
                            case "Arm":
                            case "Armv6":
                                break;
                        }
                        break;
                    case FsoExecArch.x64:
                        switch (SysInfo.CpuArch)
                        {
                            case "X64":
                                score += 80;
                                break;
                            case "X86":
                                break;
                            case "Arm64":
                                break;
                            case "Arm":
                            case "Armv6":
                                break;
                        }
                        break;
                    case FsoExecArch.x86_avx2:
                        switch (SysInfo.CpuArch)
                        {
                            case "X64":
                                break;
                            case "X86":
                                score += SysInfo.CpuAVX2 ? 100 : 0;
                                break;
                            case "Arm64":
                                break;
                            case "Arm":
                            case "Armv6":
                                break;
                        }
                        break;
                    case FsoExecArch.x86_avx:
                        switch (SysInfo.CpuArch)
                        {
                            case "X64":
                                break;
                            case "X86":
                                score += SysInfo.CpuAVX ? 90 : 0;
                                break;
                            case "Arm64":
                                break;
                            case "Arm":
                            case "Armv6":
                                break;
                        }
                        break;
                    case FsoExecArch.x86:
                        switch (SysInfo.CpuArch)
                        {
                            case "X64":
                                break;
                            case "X86":
                                score += SysInfo.CpuAVX ? 80 : 0;
                                break;
                            case "Arm64":
                                break;
                            case "Arm":
                            case "Armv6":
                                break;
                        }
                        break;
                    case FsoExecArch.arm64:
                        switch (SysInfo.CpuArch)
                        {
                            case "X64":
                                break;
                            case "X86":
                                break;
                            case "Arm64":
                                score += 100;
                                break;
                            case "Arm":
                            case "Armv6":
                                break;
                        }
                        break;
                    case FsoExecArch.arm32:
                        switch (SysInfo.CpuArch)
                        {
                            case "X64":
                                break;
                            case "X86":
                                break;
                            case "Arm64":
                                break;
                            case "Arm":
                            case "Armv6":
                                score += 100;
                                break;
                        }
                        break;
                    default:
                        Log.Add(Log.LogSeverity.Error, "FsoFile.DetermineScore", "FsoFile.DetermineScore() is missing the case for: " + arch);
                        break;
                }
            }

            return score;
        }
    }
}
