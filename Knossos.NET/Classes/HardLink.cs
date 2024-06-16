using System;
using System.IO;
using System.Runtime.InteropServices;


namespace Knossos.NET.Classes
{
    /// <summary>
    /// Helper class to create file Hardlinks in Windows, Linux and MacOS
    /// </summary>
    public static class HardLink
    {
        /// <summary>
        /// Creates a file HardLink
        /// Cross-Platform
        /// Note: It only works with files, not folders
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <returns>true if successfull, false otherwise</returns>
        public static bool CreateFileLink(string origin, string destination)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return CreateWindowsLink(origin, destination);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return CreateUnixLink(origin, destination);
            }
            return false;
        }

        private static bool CreateWindowsLink(string origin, string destination)
        {
            try
            {
                if (CreateHardLinkWindows(destination, origin))
                    return true;

                //Fail, throw exception
                int hR = Marshal.GetHRForLastWin32Error();
                var inner = Marshal.GetExceptionForHR(hR);
                throw new IOException("Error while creating hardlink: " + inner);
            }
            catch (Exception ex)
            {
                //Log exception
                Log.Add(Log.LogSeverity.Error, "Hardlink.CreateWindowsLink()", ex);
            }
            return false;
        }

        private static bool CreateUnixLink(string origin, string destination)
        {
            try
            {
                int ret = CreateHardLinkUnix(origin, destination);
                if (ret == 0)
                    return true;

                //Fail, throw exception
                var errorCode = Marshal.GetLastWin32Error();
                var errorMessage = "Error while creating hardlink: ";
                //Reference: https://github.com/dotnet/runtime/blob/main/src/libraries/Common/src/Interop/Unix/Interop.Errors.cs
                switch (errorCode)
                {
                    case 0x10002: errorMessage += "Permission denied"; break;
                    case 0x10008: errorMessage += "Bad file descriptor"; break;
                    case 0x1001C: errorMessage += "Invalid argument"; break;
                    case 0x1001D: errorMessage += "I/O Error"; break;
                    case 0x10020: errorMessage += "Too many levels of symbolic links"; break;
                    case 0x10025: errorMessage += "Filename too long"; break;
                    case 0x1002D: errorMessage += "No such file or directory"; break;
                    case 0x10039: errorMessage += "Not a directory or a symbolic link to a directory."; break;
                    case 0x10042: errorMessage += "Operation not permitted"; break;
                    default: errorMessage += "Unknown Error " + errorCode.ToString(); break;
                }
                throw new IOException(errorMessage);
            }
            catch (Exception ex)
            {
                //Log exception
                Log.Add(Log.LogSeverity.Error, "Hardlink.CreateUnixLink()", ex);
            }
            return false;
        }

        [DllImport("kernel32.dll", EntryPoint = "CreateHardLink", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateHardLinkWindows(string dest, string org, int flags = 0);

        [DllImport("libc", EntryPoint = "link", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern int CreateHardLinkUnix(string org, string dest);
    }
}
