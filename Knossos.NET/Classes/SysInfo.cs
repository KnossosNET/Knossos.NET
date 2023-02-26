using Knossos.NET.Classes;
using Knossos.NET.Models;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;

namespace Knossos.NET
{
    public class SysInfo
    {
        private static readonly bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static readonly bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        private static readonly bool isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        private static readonly bool cpuAVX = Avx.IsSupported;
        private static readonly bool cpuAVX2 = Avx2.IsSupported;
        private static string fsoPrefPath = string.Empty;


        /*
            Possible Values:
            https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.architecture?view=net-7.0
        */
        private static readonly string cpuArch = RuntimeInformation.OSArchitecture.ToString();


        public static bool IsWindows => isWindows;
        public static bool IsLinux => isLinux;
        public static bool IsMacOS => isMacOS;
        public static string CpuArch => cpuArch;
        public static bool CpuAVX => cpuAVX;
        public static bool CpuAVX2 => cpuAVX2;

        public static string GetKnossosDataFolderPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)+@"\KnossosNET";
        }

        public static string GetFSODataFolderPath()
        {
            if(fsoPrefPath != string.Empty)
            {
                return fsoPrefPath;
            }
            else
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\HardLightProductions\FreeSpaceOpen";
            }
            
        }

        public static void SetFSODataFolderPath(string path)
        {
            fsoPrefPath = path;
        }

        public static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }

        public static String? GetBasePathFromKnossosLegacy()
        {
            try
            {
                string jsonPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\knossos\settings.json";
                if (File.Exists(jsonPath))
                {
                    string jsonString = File.ReadAllText(jsonPath);
                    var jsonObject = JsonSerializer.Deserialize<LegacySettings>(jsonString);
                    return jsonObject?.base_path;
                }
            } 
            catch(Exception ex) 
            {
                Log.Add(Log.LogSeverity.Error, "Sysinfo.GetBasePathFromKnossosLegacy", ex);
            }
            return null;
        }
    }
}
