using RLM.Database.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Database
{
    public enum DbLogLevel
    {
        Production = 0, // logs Warning, Error & Critical msgs
        Test = 1, // logs Info + Production logs
        Debug = 2 // logs all
    }

    public class RlmDbLogger
    {
        private const string MSG_FORMAT = "[{0}]: {1}";

        public static void Critical(Exception ex)
        {
            throw new NotImplementedException();
        }

        public static void Debug(string msg, string dbName, string methodName)
        {
            LogLevel logLevel = ConfigFile.LogLevel;
            System.Diagnostics.Debug.WriteLine(msg);

            if (!string.IsNullOrEmpty(msg) && logLevel == LogLevel.Debug)
            {
                string message = string.Format(MSG_FORMAT, "DEBUG", msg);
                RlmDbLog newLog = new RlmDbLog(message, methodName, LogType.Debug);
                CreateErrLog(newLog, dbName);
            }
        }

        public static void Error(Exception ex, string dbName, string methodName)
        {
            LogLevel logLevel = ConfigFile.LogLevel;

            if (logLevel == LogLevel.Debug)
            {
                string message = string.Format(MSG_FORMAT, "DEBUG", ex.Message);
                RlmDbLog newLog = new RlmDbLog(message, methodName, LogType.Debug, ex);
                CreateErrLog(newLog, dbName);
            }
        }

        static Task ProcessWrite(string path, string text)
        {
            return WriteTextAsync(path, text);
        }

        static async Task WriteTextAsync(string filePath, string text)
        {
            byte[] encodedText = Encoding.Unicode.GetBytes(text);

            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Append, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            };
        }

        public static void Info(string msg, string dbName)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;

            //ProcessWrite(path + "\\" + dbName + "_info.txt", msg);

            //Task.Delay(150).Wait();
        }

        public static void Warning(string msg, string dbName, string methodName, Exception ex = null)
        {
            LogLevel logLevel = ConfigFile.LogLevel;

            if (logLevel == LogLevel.Debug)
            {
                string message = string.Format(MSG_FORMAT, "DEBUG", ex.Message);
                RlmDbLog newLog = new RlmDbLog(message, methodName, LogType.Debug, ex);
                CreateErrLog(newLog, dbName);
            }
        }

        private static void CreateErrLog(RlmDbLog log, string dbName)
        {
            if (!Directory.Exists(ConfigFile.RlmLogLocation))
            {
                Directory.CreateDirectory(ConfigFile.RlmLogLocation);
            }

            string logStr = "";

            string msg = log.Message;
            string src = log.Source;
            DateTime date = log.Date;
            string exception = log.Exception;

            logStr = string.Format("<html>" +
                                        "<table border='1px' width='100%' style='font-size:11px;border:1px solid black;'>" +
                                            "<thead>" +
                                                "<tr align='left'>" +
                                                    "<th>Message</th>" +
                                                    "<th>Source</th>" +
                                                    "<th>Date</th>" +
                                                    "<th>Exception</th>" +
                                                "</tr>" +
                                            "</thead>" +
                                            "<tbody>" +
                                                "<tr align='left'>" +
                                                    "<td>{0}</td>" +
                                                    "<td>{1}</td>" +
                                                    "<td>{2}</td>" +
                                                    "<td><pre><code>{3}</code></pre></td>" +
                                                "</tr>" +
                                            "</tbody>" +
                                        "</table>" +
                                        "<hr/><center>*** END ***</center><hr/>" +
                                "</html>", msg, src, date, exception);
            
            string path = Path.Combine(ConfigFile.RlmLogLocation, $"log_{Guid.NewGuid().ToString("N")}.html");
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                byte[] info = new UTF8Encoding(true).GetBytes(logStr);
                fs.Write(info, 0, info.Length);
            }
        }
    }
}
