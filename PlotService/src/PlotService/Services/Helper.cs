using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Oracle.ManagedDataAccess.Client;

namespace PlotService.Services
{
	public static class Extensions
	{
		public static T GetSafe<T>(this IDataReader reader, int id)
		{
			var value = reader[id];
			return value == DBNull.Value ? default(T) : (T) value;
		}
	}

	public static class Helper
	{

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
			Trace(message, parameters);
		}

		public static void Log(Exception ex)
		{
			Trace("*** ERROR ************************************************************");
			Trace("COMMAND ERROR: {0}", ex.ToString().Replace("\r\n", ";"));
			Trace("**********************************************************************");
		}

		public static void Trace(string message, params object[] parameters)
		{
			Console.WriteLine(message, parameters);
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