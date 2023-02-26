using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

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

        public static string LogFilePath = SysInfo.GetKnossosDataFolderPath()+@"\Knossos_log.log";

        public static void Add(LogSeverity logSeverity, string from, string data)
        {
            var logString = DateTime.Now.ToString() + " - *" + GetSeverityString(logSeverity) + "* : (" + from + ") " + data;

            if (Knossos.globalSettings.enableLogFile && (int)logSeverity >= Knossos.globalSettings.logLevel )
            {
                StreamWriter writer = File.AppendText(LogFilePath);
                writer.WriteLine(logString,Encoding.UTF8);
                writer.Close();
            }

            WriteToConsole(logString);
        }

        public static void Add(LogSeverity logSeverity, string from, Exception exception)
        {
            var logString = DateTime.Now.ToString() + " - *" + GetSeverityString(logSeverity) + "* : (" + from + ") " + exception.Message;
            if (Knossos.globalSettings.enableLogFile && (int)logSeverity >= Knossos.globalSettings.logLevel)
            {
                try
                {
                    StreamWriter writer = File.AppendText(LogFilePath);
                    writer.WriteLine(logString, Encoding.UTF8);
                    writer.Close();

                }catch(Exception ex)
                {
                    WriteToConsole(ex.ToString());
                }
            }
            WriteToConsole(logString);
        }

        public static void WriteToConsole(string data)
        {
            Knossos.WriteToUIConsole(data);
            if (Debugger.IsAttached)
            {
                System.Diagnostics.Debug.WriteLine(data);
            }
        }
    }
}
