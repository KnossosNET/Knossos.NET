using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Knossos.NET
{
    /// <summary>
    /// Knet logging system class
    /// </summary>
    public static class Log
    {
        public enum LogSeverity
        {
            Information,
            Warning,
            Error
        }

        public static string LogFilePath = KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "Knossos.log";

        /// <summary>
        /// Write a log entry to console and file
        /// It is always written to console, and it is written to file ONLY if the logseverity
        /// is equal or higher to the one set in global settings
        /// </summary>
        /// <param name="logSeverity"></param>
        /// <param name="from"></param>
        /// <param name="data"></param>
        public static void Add(LogSeverity logSeverity, string from, string data)
        {
            var logString = DateTime.Now.ToString() + " - *" + logSeverity.ToString() + "* : (" + from + ") " + data;
            if (Knossos.globalSettings.enableLogFile && (int)logSeverity >= Knossos.globalSettings.logLevel )
            {
                Task.Run(async () => {
                    try
                    {
                        await WaitForFileAccess(LogFilePath);
                        using (var writer = new StreamWriter(LogFilePath, true))
                        {
                            writer.WriteLine(logString, Encoding.UTF8);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteToConsole(ex.Message);
                    }
                });
            }
            WriteToConsole(logString);
        }

        /// <summary>
        /// Write a log entry to console and file
        /// It is always written to console, and it is written to file ONLY if the logseverity
        /// is equal or higher to the one set in global settings
        /// </summary>
        /// <param name="logSeverity"></param>
        /// <param name="from"></param>
        /// <param name="exception"></param>
        public static void Add(LogSeverity logSeverity, string from, Exception exception)
        {
            Add(logSeverity,from, exception.Message);
        }

        /// <summary>
        /// Write a string to VS console and the UI console on the Debug tab
        /// </summary>
        /// <param name="data"></param>
        public async static void WriteToConsole(string data)
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() => Knossos.WriteToUIConsole(data), DispatcherPriority.Background);
                if (Debugger.IsAttached)
                {
                    System.Diagnostics.Debug.WriteLine(data);
                }
            }
            catch { }
        }

        /// <summary>
        /// Wait for the log file being available for write
        /// Returns when the file is ready
        /// </summary>
        /// <param name="filename"></param>
        private static async Task WaitForFileAccess(string filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read))
                    {
                        inputStream.Close();
                        return;
                    }
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
