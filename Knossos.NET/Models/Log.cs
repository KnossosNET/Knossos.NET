using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Knossos.NET
{
    /*
        Handles everything related to logging.
    */
    public static class Log
    {
        public enum LogSeverity
        {
            Information,
            Warning,
            Error
        }

        private static string GetSeverityString(LogSeverity severity)
        {
            switch (severity)
            {
                case LogSeverity.Error: return "Error";
                case LogSeverity.Warning: return "Warning";
                case LogSeverity.Information: return "Info";
                default: return "Undefined";
            }
        }

        public static string LogFilePath = SysInfo.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "Knossos.log";

        public static void Add(LogSeverity logSeverity, string from, string data)
        {
            var logString = DateTime.Now.ToString() + " - *" + GetSeverityString(logSeverity) + "* : (" + from + ") " + data;

            if (Knossos.globalSettings.enableLogFile && (int)logSeverity >= Knossos.globalSettings.logLevel )
            {
                try
                {
                    Task.Run(async () => {
                        await WaitForFileAccess(LogFilePath);
                        StreamWriter writer = File.AppendText(LogFilePath);
                        writer.WriteLine(logString, Encoding.UTF8);
                        writer.Close();
                    });
                }
                catch (Exception ex)
                {
                    WriteToConsole(ex.ToString());
                }
            }
            WriteToConsole(logString);
        }

        public static void Add(LogSeverity logSeverity, string from, Exception exception)
        {
            Add(logSeverity,from, exception.Message);
        }

        public async static void WriteToConsole(string data)
        {
            await Dispatcher.UIThread.InvokeAsync(() => Knossos.WriteToUIConsole(data), DispatcherPriority.Background);
            if (Debugger.IsAttached)
            {
                System.Diagnostics.Debug.WriteLine(data);
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
                await Task.Delay(500);
                await WaitForFileAccess(filename);
            }
        }
    }
}
