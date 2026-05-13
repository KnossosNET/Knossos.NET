using Avalonia.Threading;
using System;
using System.Collections.Concurrent;
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

        private class LogEntry
        {
            public string LogString { get; }
            public bool WriteToFile { get; }

            public LogEntry(LogSeverity logSeverity, string from, string data)
            {
                LogString = $"{DateTime.Now} - *{logSeverity}* : ({from}) {data}";

                WriteToFile = Knossos.globalSettings.enableLogFile && (int)logSeverity >= Knossos.globalSettings.logLevel;
            }
        }

        public static readonly string LogFilePath = Path.Combine(KnUtils.GetKnossosDataFolderPath(), "Knossos.log");
        private static readonly ConcurrentQueue<LogEntry> queuedLogs = new();
        private static readonly Task _consumerTask;
        private static readonly int maxFileWriteAttempts = 5;

        static Log()
        {
            _consumerTask = Task.Run(ConsumeQueueAsync);
            if(Knossos.globalSettings.enableLogFile)
            {
                _ = WriteToFileAsync($"{DateTime.Now.ToString()} Init logger...");
            }
        }

        private static async Task ConsumeQueueAsync()
        {
            while (true)
            {
                if (queuedLogs.TryDequeue(out var entry))
                {
                    await ProcessEntryAsync(entry);
                }
                else
                {
                    await Task.Delay(10);
                }
            }
        }

        private static async Task ProcessEntryAsync(LogEntry entry)
        {
            // Siempre se escribe en consola (UI thread)
            await WriteToConsole(entry.LogString);

            // Si corresponde, escribir en archivo
            if (entry.WriteToFile)
            {
                await WriteToFileAsync(entry.LogString);
            }
        }

        private static async Task WriteToFileAsync(string data, int attempt = 1)
        {
            try
            {
                await using var writer = new StreamWriter(LogFilePath, true, Encoding.UTF8);
                await writer.WriteLineAsync(data);
            }
            catch (Exception ex)
            {
                if (attempt < maxFileWriteAttempts)
                {
                    await Task.Delay(50 * attempt);
                    await WriteToFileAsync(data, attempt + 1);
                }
                else
                {
                    await WriteToConsole($"Failed to write to logfile: {ex.Message}\nFilePath: {LogFilePath}");
                }
            }
        }

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
        public static async Task WriteToConsole(string data)
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
