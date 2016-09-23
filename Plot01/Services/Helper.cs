using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices.Core;

namespace BatchPlot.Services
{
    public static class Helper
    {
        private static readonly log4net.ILog _log =
            log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static double GetExecutionTime(Action action)
        {
            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            return Math.Round(stopwatch.Elapsed.TotalSeconds, 2);
        }

        public static void Log(string message, object parameter)
        {
            Log(message, new[] { parameter });
        }

        public static void Log(string message, params object[] parameters)
        {
            _log.DebugFormat(message, parameters);
            Trace(message, parameters);
        }

        public static void Log(Exception ex)
        {
            _log.Error("*** ERROR ************************************************************");
            _log.Error("Exception:", ex);
            _log.Error("**********************************************************************");
            Trace("*** ERROR ************************************************************");
            Trace("COMMAND ERROR: {0}", ex.ToString().Replace("\r\n", ";"));
            Trace("**********************************************************************");
        }

        //public static void Log(Exception ex, string message, params object[] parameters)
        //{
        //    _log.Error("*** ERROR ************************************************************");
        //    _log.ErrorFormat(message, parameters);
        //    _log.Error("Exception:", ex);
        //    _log.Error("**********************************************************************");
        //    Trace(message, parameters);
        //}

        public static void Trace(string message, params object[] parameters)
        {
            Console.WriteLine(message, parameters);
            //var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            //ed.WriteMessage(string.Format(message + "\n", parameters));
        }

        public static string GetAssemblyFolder()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        public static string CreateTempFolder()
        {
            var tempFolder = Path.Combine(GetAssemblyFolder(), "temp",
                Path.GetRandomFileName());
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }
            return tempFolder;
        }

        public static void DeleteTempFolder(string tempFolder)
        {
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
        }

        public static string GetLocalFilePath(string serverFilePath, string localFolder)
        {
            return Path.Combine(localFolder, Path.GetFileName(serverFilePath));
        }
    }
}