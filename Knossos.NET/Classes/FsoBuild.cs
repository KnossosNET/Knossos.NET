using Knossos.NET.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

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
        arm32,
        arm64,
        other
    }

    public enum FsoExecEnvironment
    {
        Unknown,
        Windows,
        Linux,
        Mac
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
        /*
         Direct Exe
         */
        public FsoBuild(string directExecpath)
        {
            id = "DirectExec";
            title = "DirectExec";
            try
            {
                var parts = directExecpath.Split(@"\");
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
                        version = "99.99.99" + SysInfo.GetTimestamp(DateTime.Now);
                    }
                }
                else
                {
                    version = "99.99.99" + SysInfo.GetTimestamp(DateTime.Now);
                }
            }
            catch
            {
                version = "99.99.99" + SysInfo.GetTimestamp(DateTime.Now);
            }
            folderPath = directExecpath;
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
            description = modJson.description;
            version = modJson.version;
            folderPath = modJson.fullPath;
            stability = GetFsoStability(modJson.stability, modJson.id);
            date = modJson.lastUpdate;
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
            try
            {
                var cmd = new Process();
                cmd.StartInfo.FileName = fullpath;
                cmd.StartInfo.Arguments = "-get_flags json_v1";
                cmd.StartInfo.UseShellExecute = false;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                cmd.Start();
                cmd.WaitForInputIdle();
                string result = cmd.StandardOutput.ReadToEnd();
                cmd.Dispose();
                return JsonSerializer.Deserialize<FlagsJsonV1>(result);
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "FsoBuild.GetFlags()", ex);
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

            foreach (FsoFile file in validExecs)
            {
                switch(file.arch)
                {
                    case FsoExecArch.x64_avx:
                        if (SysInfo.CpuArch == "X64" && SysInfo.CpuAVX)
                        {
                            return folderPath + file.filename.Replace(@"/", @"\");
                        }
                        break;
                    case FsoExecArch.x64:
                        if(SysInfo.CpuArch == "X64")
                        {
                            return folderPath + file.filename.Replace(@"/", @"\");
                        }
                        break;

                    case FsoExecArch.x86:
                        if (SysInfo.CpuArch == "X86" && SysInfo.CpuAVX)
                        {
                            return folderPath + file.filename.Replace(@"/", @"\");
                        }
                        break;
                    case FsoExecArch.x86_avx:
                        if (SysInfo.CpuArch == "X86")
                        {
                            return folderPath + file.filename.Replace(@"/", @"\");
                        }
                        break;

                    case FsoExecArch.arm64:
                        if (SysInfo.CpuArch == "Arm64")
                        {
                            return folderPath + file.filename.Replace(@"/", @"\");
                        }
                        break;
                    case FsoExecArch.arm32:
                        if (SysInfo.CpuArch == "Arm" || SysInfo.CpuArch == "Armv6")
                        {
                            return folderPath + file.filename.Replace(@"/", @"\");
                        }
                        break;

                    case FsoExecArch.other:
                        //Unsupported
                        break;
                }
                
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
            if((environment.ToLower().Contains("x86") || environment.ToLower().Contains("x86_64") ) && !environment.ToLower().Contains("avx"))
            {
                properties.sse2 = true;
            }
            if ((environment.ToLower().Contains("x86") || environment.ToLower().Contains("x86_64")) && environment.ToLower().Contains("avx"))
            {
                properties.avx = true;
            }
            if (environment.ToLower().Contains("x86_64"))
            {
                properties.x64 = true;
            }
            if (environment.ToLower().Contains("arm64"))
            {
                properties.arm64 = true;
            }
            if (environment.ToLower().Contains("arm32"))
            {
                properties.arm32 = true;
            }
            return properties;
        }

        /*
            Determine the operating system this build file is compiled for 
        */
        private FsoExecEnvironment GetExecEnvironment(string? enviroment)
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
                return FsoExecEnvironment.Mac;

            Log.Add(Log.LogSeverity.Information, "FsoBuild.GetExecEnvironment", "Unable to determine the proper build enviroment. Env: " + enviroment);
            return FsoExecEnvironment.Unknown;
        }

        /*
            Determine the CPU arch this build file is compiled for
        */
        private FsoExecArch GetExecArch(ModProperties? properties)
        {
            if (properties == null || properties.other)
                return FsoExecArch.other;

            if (properties.arm32)
                return FsoExecArch.arm32;

            if (properties.arm64)
                return FsoExecArch.arm64;

            if (properties.x64)
            {
                if (properties.avx)
                    return FsoExecArch.x64_avx;
                else
                    return FsoExecArch.x64;
            }
            else
            {
                if (properties.avx)
                    return FsoExecArch.x86_avx;
                else
                    return FsoExecArch.x86;
            }
        }

        /*
          If label is null, then is Release.
        */
        private FsoExecType GetExecType(string? label)
        {
            if (label == null)
                return FsoExecType.Release;

            switch (label)
            {
                case "FastDebug":
                case "Rollback Build": 
                case "Fast Debug": return FsoExecType.Debug;

                case "Fred FastDebug":
                case "FRED Fast Debug":
                case "FRED Debug":
                case "FRED2 Debug": return FsoExecType.Fred2Debug;

                case "FRED":
                case "FRED2": return FsoExecType.Fred2;

                case "QTFred FastDebug":
                case "QtFRED Debug": return FsoExecType.QtFredDebug;

                case "QTFred":
                case "QtFRED": return FsoExecType.QtFred;


                default:
                    Log.Add(Log.LogSeverity.Warning, "FsoBuild.GetExecType", "Unable to determine FSO Exec Type. Label: " + label);
                    return FsoExecType.Unknown;
            }
        }

        /* 
           Official FSO builds from nebula use "FSO" as id, only check stability on those.
           For all others the stability is "custom" since we cant really know.    
        */
        private FsoStability GetFsoStability(string? stability, string? modId)
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
    }

    public class FsoFile
    {
        public string filename;
        public FsoExecType type;
        public FsoExecArch arch;
        public FsoExecEnvironment env;
        public bool isValid;

        public FsoFile(string filename, string modpath, FsoExecType type, FsoExecArch arch, FsoExecEnvironment env)
        {
            this.filename = filename;
            this.type = type;
            this.arch = arch;
            this.env = env;
            if(arch == FsoExecArch.other || env == FsoExecEnvironment.Unknown || type == FsoExecType.Unknown)
            {
                isValid = false;
            }
            else
            {
                isValid = IsValid(modpath);
            }
        }

        /*
            Checks if the file actually exist,
            if the OS system matchs and if the cpu the arch is compatible
        */
        private bool IsValid(string modpath)
        {
            if (modpath != string.Empty && !File.Exists(modpath + filename.Replace(@"/", @"\")))
            {
                Log.Add(Log.LogSeverity.Warning, "FsoFile.CheckValidity", "File: " + modpath + filename + " does not exist!");
                return false;
            }

            if(env == FsoExecEnvironment.Windows && !SysInfo.IsWindows || env == FsoExecEnvironment.Linux && !SysInfo.IsLinux || env == FsoExecEnvironment.Mac && !SysInfo.IsMacOS)
            {
                if(modpath != string.Empty)
                    Log.Add(Log.LogSeverity.Warning, "FsoFile.CheckValidity", "File: " + modpath + filename + " is not valid for this OS. Detected: "+env);
                return false;
            }

            if (arch == FsoExecArch.x86 && SysInfo.CpuArch != "X86" && SysInfo.CpuArch != "X64")
            {
                if (modpath != string.Empty)
                    Log.Add(Log.LogSeverity.Warning, "FsoFile.CheckValidity", "File: " + modpath + filename + " is not valid for this CPU. Detected: " + arch + " SysInfo: "+ SysInfo.CpuArch);
                return false;
            }

            if (arch == FsoExecArch.x64 && (SysInfo.CpuArch != "X64"))
            {
                if (modpath != string.Empty)
                    Log.Add(Log.LogSeverity.Warning, "FsoFile.CheckValidity", "File: " + modpath + filename + " is not valid for this CPU. Detected: " + arch + " SysInfo: " + SysInfo.CpuArch);
                return false;
            }

            if (arch == FsoExecArch.x86_avx && SysInfo.CpuArch != "X86" && SysInfo.CpuArch != "X64" && !SysInfo.CpuAVX)
            {
                if (modpath != string.Empty)
                    Log.Add(Log.LogSeverity.Warning, "FsoFile.CheckValidity", "File: " + modpath + filename + " is not valid for this CPU. Detected: " + arch + " SysInfo: " + SysInfo.CpuArch);
                return false;
            }

            if (arch == FsoExecArch.x64_avx && SysInfo.CpuArch != "X64" && !SysInfo.CpuAVX)
            {
                if (modpath != string.Empty)
                    Log.Add(Log.LogSeverity.Warning, "FsoFile.CheckValidity", "File: " + modpath + filename + " is not valid for this CPU. Detected: " + arch + " SysInfo: " + SysInfo.CpuArch);
                return false;
            }

            if (arch == FsoExecArch.arm32 && SysInfo.CpuArch != "Armv6" && SysInfo.CpuArch != "Arm")
            {
                if (modpath != string.Empty)
                    Log.Add(Log.LogSeverity.Warning, "FsoFile.CheckValidity", "File: " + modpath + filename + " is not valid for this CPU. Detected: " + arch + " SysInfo: " + SysInfo.CpuArch);
                return false;
            }

            if (arch == FsoExecArch.arm64 && SysInfo.CpuArch != "Armv64")
            {
                if (modpath != string.Empty)
                    Log.Add(Log.LogSeverity.Warning, "FsoFile.CheckValidity", "File: " + modpath + filename + " is not valid for this CPU. Detected: " + arch + " SysInfo: " + SysInfo.CpuArch);
                return false;
            }

            return true;
        }
    }

}
