using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        private class LogEntry
        {
            string logString = string.Empty;
            bool writeToFile = false;

            public LogEntry(LogSeverity logSeverity, string from, string data)
            {
                logString = DateTime.Now.ToString() + " - *" + logSeverity.ToString() + "* : (" + from + ") " + data;
                if (Knossos.globalSettings.enableLogFile && (int)logSeverity >= Knossos.globalSettings.logLevel)
                {
                    writeToFile = true;
                }
                Task.Factory.StartNew(() => { 
                    ProcessLogEntry(); 
                });
            }

            private async void ProcessLogEntry()
            {
                while (!queuedLogs.Any() || queuedLogs.Peek() != this)
                    await Task.Delay(10);
                WriteToConsole();
                if (writeToFile)
                    WriteToFile();
                queuedLogs.Dequeue();
            }

            private void WriteToConsole()
            {
                Log.WriteToConsole(logString);
            }

            private void WriteToFile(int attempt = 1)
            {
                try
                {
                    using (var writer = new StreamWriter(LogFilePath, true))
                    {
                        writer.WriteLine(logString, Encoding.UTF8);
                    }
                }catch(Exception ex)
                {
                    if(attempt < maxFileWriteAttempts)
                    {
                        attempt++;
                        WriteToFile(attempt);
                    }
                    else
                    {
                        Log.WriteToConsole("Failed to write to the logfile, reason: " + ex.ToString() + " \nFilePath:"+ LogFilePath);
                    }
                }
            }
        }

        public static readonly string LogFilePath = KnUtils.GetKnossosDataFolderPath() + Path.DirectorySeparatorChar + "Knossos.log";
        private static readonly int maxFileWriteAttempts = 5;
        private static Queue<LogEntry> queuedLogs = new Queue<LogEntry>();

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
            queuedLogs.Enqueue(new LogEntry(logSeverity, from, data));
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
            if (exception.InnerException != null)
            {
                Add(logSeverity, from, exception.InnerException.Message);
            }
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
                    Debug.WriteLine(data);
                }
            }
            catch { }
        }
    }
}
