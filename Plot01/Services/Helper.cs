using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace BatchPlot.Services
{
    public static class Helper
    {
        public static double GetExecutionTime(Action action)
        {
            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            return Math.Round(stopwatch.Elapsed.TotalSeconds, 2);
        }

        public static string GetAssemblyFolder()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        public static string GetLogFolderPath()
        {
            var folder = Path.Combine(GetAssemblyFolder(), "logs");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return folder;
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