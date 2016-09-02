using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
//using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;

namespace AutoCADTest.Service
{
    public static class Helper
    {
        private static readonly log4net.ILog _log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

        public static void Trace(string message, params object[] parameters)
        {
            Debug.WriteLine(message, parameters);
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage(string.Format(message + "\n", parameters));
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
            var tempFolder = Path.Combine(GetAssemblyFolder(), "temp");
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