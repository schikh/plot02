﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using PlotService.Services;

namespace PlotService
{
	public class Program
		{
			private static object sysLock = new object();

			public static void Main(string[] args)
		{
			var loggerFactory = new LoggerFactory();
			loggerFactory.AddProvider(new ConsoleLoggerProvider((text, logLevel) => logLevel >= LogLevel.Information, true));
			var logger = loggerFactory.CreateLogger<Program>();
			//logger.LogInformation("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");

			var tasks = new List<Task>();
			var blockingCollection = new BlockingCollection<PlotTask>();

			tasks.Add(Task.Factory.StartNew(() =>
			{
				while (true)
				{
					if (blockingCollection.Count() < Configuration.BatchSize / 2)
					{
						var plotManager = new PlotManager();
						var list = plotManager.GetTasks();
						list.ToList().ForEach(x => blockingCollection.Add(x));
					}
					Console.WriteLine("Tasks:" + blockingCollection.Count());
					Thread.Sleep(10000);
				}
			}));

			tasks.AddRange(Enumerable.Range(0, Configuration.NumberOfConsoles)
				.Select(x => Task.Factory.StartNew(() => {
					while (true)
					{
						var plotTask = blockingCollection.Take();

						try
						{
							lock (sysLock)
							{
								Console.ForegroundColor = ConsoleColor.Cyan;
								Console.WriteLine(">>> " + plotTask.CommandLineParameters());
								Console.ResetColor();
							}

							var result = false;

							if (plotTask.TypePlan == "T")
							{
								result = ProcessPlotTickects(plotTask);
							}
							else
							{
								if (File.Exists(plotTask.PathPlan))
								{
									var tempFolder = Helper.CreateTempFolder();
									var f = new FileHelper();
									var file = f.ImportServerFile(plotTask.PathPlan, tempFolder);
									var list = f.GetAttachedFilePaths(plotTask.PathPlan);
									f.ImportServerFiles(list, tempFolder);
									plotTask.PathPlan = file;
									result = ProcessPlotTickects(plotTask);
									Helper.DeleteTempFolder(tempFolder);
								}
								else
								{
								}
							}

							var plotManager = new PlotManager();
							plotManager.UpdateTaskStatus(plotTask.TaskId, result ? 5 : 3);
						}
						catch (System.Exception ex)
						{
							Helper.Log(ex.ToString());

							var plotManager = new PlotManager();
							plotManager.UpdateTaskStatus(plotTask.TaskId, 3);
						}
					}
				})));

			Task.WaitAll(tasks.ToArray());
		}

		private static bool ProcessPlotTickects(PlotTask plotTask)
		{
			//var fileName = @"C:\Program Files\Autodesk\Autodesk AutoCAD Map 3D 2014\accoreconsole.exe";
			//var xxx =  @"/i W:\RWA004\Cardex\Est\Edpl\Vvs\Projet\EDPL-PP-115871.dwg /s C:\Test\Plot\Plot01\Scripts\PlotDwg.scr /f c:\test\EnerGis\xxx.pdf ";

			using (var process = new Process())
			{
				process.StartInfo.FileName = Configuration.AcConsolePath;
				process.StartInfo.Arguments = plotTask.CommandLineParameters();
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.ErrorDialog = false;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardError = true;
				//process.StartInfo.RedirectStandardInput = true;
				process.StartInfo.RedirectStandardOutput = true;
				//process.EnableRaisingEvents = true;
				//process.OutputDataReceived += (sender, e) => {
				//	Console.ForegroundColor = ConsoleColor.Green;
				//	Console.WriteLine(e.Data);
				//	Console.ResetColor();
				//};
				//process.ErrorDataReceived += (sender, e) => {
				//	Console.ForegroundColor = ConsoleColor.Red;
				//	Console.WriteLine(e.Data);
				//	Console.ResetColor();
				//};
				//process.Exited += (sender, e) => {
				//	Console.ForegroundColor = ConsoleColor.Red;
				//	Console.WriteLine(e);
				//	Console.ResetColor();
				//};
				process.Start();
				//process.BeginErrorReadLine();
				//process.BeginOutputReadLine();

				//Thread.Sleep(1000);
				//process.StandardInput.WriteLine("Y");
				//process.StandardInput.WriteLine();
				//process.StandardInput.Flush();

				var standardOutput = process.StandardOutput.ReadToEnd();

				lock (sysLock)
				{
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine(standardOutput);
					Console.ResetColor();
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine(process.StandardError.ReadToEnd());
					Console.ResetColor();
				}

				process.WaitForExit(60000);

				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine("process.ExitCode:" + process.ExitCode);
				Console.ResetColor();

				if (!process.HasExited)
				{
					process.Kill();
				}

				return standardOutput.Contains("PLOT SUCCESSFUL");
			}
		}

		public class FileHelper
		{
			public IEnumerable<string> GetAttachedFilePaths(string dwgFile)
			{
				var directory = Path.GetDirectoryName(dwgFile);
				var xmlFile = Path.Combine(directory, Path.GetFileNameWithoutExtension(dwgFile) + ".xml");
				//var doc = XDocument.Load("yourfilepath");
				//var authors = doc.Descendants("file");
				if (File.Exists(xmlFile))
				{
					var text = File.ReadAllText(xmlFile);
					var pattern = @"<file .* filename=""(?<fileName>[^""]*)"" .*>";
					var expr = new Regex(pattern, RegexOptions.Multiline);
					foreach (Match match in expr.Matches(text))
					{
						yield return Path.Combine(directory, match.Groups["fileName"].Value);
					}
				}
			}

			public string ImportServerFile(string serverFilePath, string tempFolder)
			{
				var localFilePath = Helper.GetLocalFilePath(serverFilePath, tempFolder);
				File.Copy(serverFilePath, localFilePath, true);
				return localFilePath;
			}

			public void ImportServerFiles(IEnumerable<string> serverFilePaths, string tempFolder)
			{
				foreach (var serverFilePath in serverFilePaths)
				{
					if (File.Exists(serverFilePath))
					{
						ImportServerFile(serverFilePath, tempFolder);
					}
					else
					{
						Helper.Log("FILE NOT FOUND {0}", serverFilePath);
					}
				}
			}
		}

		public class PlotManager
		{
			public IEnumerable<PlotTask> GetTasks()
			{
				var connectionString = Configuration.Instance.GetConnectionString("WriteConnectionString");
				var da = new DataAccessService(connectionString);
				var query = string.Format(@"SELECT 
								a.PJOBID,
								a.S_PLOT_TICKET,
								a.S_PLTICKET_STATUS,
								a.O_DATE,
								a.N_TOT_PLAN,
								a.USERID,
								b.PTASKID,
								b.C_TYPE_PLAN,
								b.L_ID_STAMP,
								b.L_ID_PLANCHETTE,
								b.N_ORD_PLAN,
								b.C_TYPE_MAP,
								b.L_PATH_PLAN,
								b.LIST_ENERGY,
								b.L_PATH_RESULT_PDF,
								b.N_SCALE,
								b.N_ESSAY,
								b.S_PLTICKET_STATUS,
								b.C_SIDE
								FROM PJOB a
								INNER JOIN PTASK b
								ON a.PJOBID = b.PJOBID
								WHERE a.S_PLTICKET_STATUS = 1 AND ROWNUM <= {0} ", Configuration.BatchSize);
				var list = da.IterateOverReader(query, x => new PlotTask() {
					JobId = x.GetInt32(0),
					PlotTicket = x.GetInt32(1),
					JobStatus = x.GetInt32(2),
					Date = x.GetDateTime(3),

					TotalPlan = x.GetInt32(4),
					UserId = x.GetSafe<string>(5),
					TaskId = x.GetInt32(6),
					TypePlan = x.GetSafe<string>(7),
					IdStamp = x.GetSafe<string>(8),

					IdPlanchette = x.GetSafe<string>(9),
					OrdPlan = x.GetInt32(10),
					TypMap = x.GetSafe<string>(11),
					PathPlan = x.GetSafe<string>(12),
					ListEnergy = x.GetSafe<string>(13),
					PathResultPdf = Configuration.LocalRootPath + x.GetSafe<string>(14).Substring(Configuration.ProductionRootPath.Length), 
					Scale = x.GetSafe<string>(15),
					Essay = x.GetInt32(16),
					PlotTicketStatus = x.GetInt32(17),
					Side = x.GetSafe<string>(18)
				});
				return list;
			}

			public void UpdateTaskStatus(int taskId, int taskStatus)
			{
				var connectionString = Configuration.Instance.GetConnectionString("WriteConnectionString");
				var da = new DataAccessService(connectionString);
				var query = $"UPDATE PTASK SET S_PLTICKET_STATUS = {taskStatus} WHERE PTASKID = {taskId} ";
				var result = da.ExecuteCommand(query);
			}
		}

	}

	public class CommandLineParametersBuilder
	{
		private string _line;

		public CommandLineParametersBuilder Add(string name)
		{
			_line = _line + $"/{name} ";
			return this;
		}

		public CommandLineParametersBuilder Add(string name, string value)
		{
			_line = _line + $"/{name} \"{value}\" ";
			return this;
		}

		public string GetCommandLineParameters()
		{
			return _line;
		}
	}

	public class PlotTask
	{
		public int JobId { get; set; }
		public int PlotTicket { get; set; }
		public int JobStatus { get; set; }
		public DateTime Date { get; set; }
		public int TotalPlan { get; set; }
		public string UserId { get; set; }
		public int TaskId { get; set; }
		public string TypePlan { get; set; }
		public string IdStamp { get; set; }
		public string IdPlanchette { get; set; }
		public int OrdPlan { get; set; }
		public string TypMap { get; set; }
		public string PathPlan { get; set; }
		public string ListEnergy { get; set; }
		public string PathResultPdf { get; set; }
		public string Scale { get; set; }
		public int Essay { get; set; }
		public int PlotTicketStatus { get; set; }
		public string Side { get; set; }

		public string CommandLineParameters()
		{
			var builder = new CommandLineParametersBuilder();

			if (string.Equals(TypePlan, "T", StringComparison.InvariantCultureIgnoreCase))
			{
				builder.Add("i", Configuration.EmptyDwgPath) 
					.Add(	"s", Configuration.PlotPlanchetteScriptPath)
					.Add(	"id", IdPlanchette)
					.Add(	"r", Scale)
					.Add(	"z", Side)
					.Add(	"c", TypMap)
					.Add(	"e", ListEnergy.Replace(' ', ','))
					.Add(	"f", PathResultPdf)
					.Add(	"st", IdStamp)
					.Add(	"t", TotalPlan.ToString())
					.Add(	"n", OrdPlan.ToString())
					.Add(	"u", UserId)
					.Add("imp")
					.Add("d")
					.Add("isolate");
			}
			else
			{
				builder.Add("i", PathPlan)
					.Add("s", Configuration.PlotDwgScriptPath)
					.Add("f", PathResultPdf)
					.Add("d")
					.Add("isolate");
			}
			return builder.GetCommandLineParameters();
		}

		//################################################################################################################################
		//#
		//################################################################################################################################

		static void DeleteFile(BlockingCollection<PlotTask> blockingCollection)
		{
			var file = @"C:\bidon\Temp\xxx" + Guid.NewGuid();
			System.IO.File.WriteAllText(file + ".txt", "text");

			Process proc = new Process();
			// Run the command "del c:\temp" in a DOS window
			// This will show a confirmation prompt requiring 
			// us to send a "Y" to continue
			proc.StartInfo.FileName = "cmd.exe";
			proc.StartInfo.Arguments = @"/c del " + file + ".* /p";
			Console.WriteLine(">>> " + proc.StartInfo.Arguments);

			// Redirect standard input
			proc.StartInfo.RedirectStandardOutput = true;
			proc.StartInfo.RedirectStandardInput = true;

			// UseShellExecute must be false to redirect input
			proc.StartInfo.UseShellExecute = false;

			// Start the process
			proc.Start();

			// Wait a second
			//Thread.Sleep(1000);




			//proc.StandardInput.WriteLine("Y");


			var procOutput = new StringBuilder();
			while (!proc.StandardOutput.EndOfStream)
			{
				char[] buffer = new char[1024];
				int charsRead = proc.StandardOutput.Read(buffer, 0, buffer.Length);
				Console.WriteLine(">>> " + new string(buffer));

				procOutput.Append(buffer, 0, charsRead);

				if (procOutput.ToString().Contains("Delete (Y/N)?"))
				{
					proc.StandardInput.WriteLine("Y");
					proc.StandardInput.WriteLine();
					proc.StandardInput.Flush();
					break;
				}
			}

			// Now that we've sent the confirmation "Y" wait for the process to exit
			proc.WaitForExit();

			Console.WriteLine("The process finished with ExitCode: {0} {1}", proc.ExitCode, file);
			//Console.ReadKey();
		}

		private static void ProcessPlotTickects3(BlockingCollection<PlotTask> blockingCollection)
		{
			var fileName = @"C:\Program Files\Autodesk\Autodesk AutoCAD Map 3D 2014\accoreconsole.exe";

			var plotTask = blockingCollection.Take();

			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("Worker 1: " + fileName + " " + plotTask.CommandLineParameters());
			Console.ResetColor();

			var d = Path.GetDirectoryName(plotTask.PathResultPdf);
			if (!Directory.Exists(d))
			{
				Directory.CreateDirectory(d);
			}

			//var processStartInfo = new ProcessStartInfo {
			//	FileName = fileName,
			//	Arguments = plotTask.CommandLineParameters(),
			//	UseShellExecute = false,
			//	CreateNoWindow = true,
			//	//StandardOutputEncoding = Encoding.GetEncoding(Console.OutputEncoding.CodePage)
			//	RedirectStandardInput = true,
			//	RedirectStandardOutput = true,
			//	//StandardOutputEncoding = Encoding.ASCII,
			//	//StandardErrorEncoding  = Encoding.GetEncoding(65001),
			//	//StandardOutputEncoding = Encoding.GetEncoding(65001)
			//};

			//var process = System.Diagnostics.Process.Start(processStartInfo);


			Process process = new Process();
			// Run the command "del c:\temp" in a DOS window
			// This will show a confirmation prompt requiring 
			// us to send a "Y" to continue
			process.StartInfo.FileName = fileName;
			process.StartInfo.Arguments = plotTask.CommandLineParameters();
			Console.WriteLine(">>> " + process.StartInfo.Arguments);

			// Redirect standard input
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardError = true;

			process.StartInfo.StandardOutputEncoding = Encoding.ASCII;
			process.StartInfo.StandardErrorEncoding = Encoding.GetEncoding(65001);
			process.StartInfo.StandardOutputEncoding = Encoding.GetEncoding(65001);


			// UseShellExecute must be false to redirect input
			process.StartInfo.UseShellExecute = false;

			// Start the process
			process.Start();
			Thread.Sleep(1000);

			var procOutput = new StringBuilder();

			while (!process.StandardOutput.EndOfStream)
			{
				char[] buffer = new char[1024];
				int charsRead = process.StandardOutput.Read(buffer, 0, buffer.Length);
				Console.WriteLine(">>> " + new string(buffer));

				procOutput.Append(buffer, 0, charsRead);

				if (procOutput.ToString().Contains("Delete (Y/N)?"))
				{
					process.StandardInput.WriteLine("Y");
					process.StandardInput.WriteLine();
					process.StandardInput.Flush();
					break;
				}
			}


			//process.OutputDataReceived += (object sender, System.Diagnostics.DataReceivedEventArgs e) =>
			//Console.WriteLine(e.Data);
			//Console.WriteLine("output>>" + e.Data);
			//process.BeginOutputReadLine();


			//process.StandardInput.WriteLine("Y");
			//process.StandardInput.Close();

			//var output = process.StandardOutput.ReadToEnd();
			//var s = new StringBuilder();
			//output.Where((x, i) => i % 2 == 0).ToList().ForEach(x => s.Append(x));
			//output = s.ToString();




			process.WaitForExit();
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("process.ExitCode:" + process.ExitCode);
			Console.ResetColor();
			//Console.WriteLine(output);

			if (!process.HasExited)
			{
				process.Kill();
			}
		}

		private static void ProcessPlotTickects2()
		{
			var fileName = @"C:\Program Files\Autodesk\Autodesk AutoCAD Map 3D 2014\accoreconsole.exe";

			Process process = new Process();
			process.StartInfo.FileName = fileName;
			process.StartInfo.Arguments = @"/i W:\RWA004\Cardex\Est\Edpl\Vvs\Projet\EDPL-PP-115871.dwg /s C:\Test\Plot\Plot01\Scripts\PlotDwg.scr /f c:\test\EnerGis\HUB001A_PRD\BIZT_HUB_0347_04\East\201609\3_1_1643232_31043517\2_2_EDPL-PP-115871_197865.pdf /l en-US /isolate";
			//process.StartInfo.Arguments = @"/i C:\Test\Plot\Plot01\Scripts\Empty.dwg /s C:\Test\Plot\Plot01\Scripts\PlotPlanchette.scr /id 151127A /r 1000 /z O /c MAP /e BP /f c:\test\EnerGis\HUB001A_PRD\BIZT_HUB_0347_04\West\201609\1_2_1663348_31012075\1_1_151127A_BP.pdf /st 1663348-31012075 /t 5 /n 3 /u BZT /imp /d /l en-US /isolate";
			Console.WriteLine(">>> " + process.StartInfo.Arguments);

			//process.StartInfo.RedirectStandardOutput = true;
			//process.StartInfo.RedirectStandardInput = true;
			//process.StartInfo.RedirectStandardError = true;

			////process.StartInfo.StandardOutputEncoding = Encoding.ASCII;
			////process.StartInfo.StandardErrorEncoding = Encoding.GetEncoding(65001);
			////process.StartInfo.StandardOutputEncoding = Encoding.GetEncoding(65001);

			//process.StartInfo.UseShellExecute = false;
			//process.StartInfo.CreateNoWindow = false;

			//process.Start();
			//Thread.Sleep(1000);

			// some of the flags are not needed
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.ErrorDialog = false;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.EnableRaisingEvents = true;
			process.OutputDataReceived += (sender, e) =>
			Console.WriteLine(e.Data);
			process.ErrorDataReceived += (sender, e) =>
			Console.WriteLine(e.Data);
			process.Exited += (sender, e) =>
			Console.WriteLine(e);

			process.Start();
			process.BeginErrorReadLine();
			process.BeginOutputReadLine();

			//process.StandardInput.WriteLine("Y");
			//process.StandardInput.WriteLine();
			//process.StandardInput.Flush();

			//var procOutput = new StringBuilder();

			////var output = process.StandardOutput.ReadToEnd();
			////Console.WriteLine(">>> " + output);

			//while (!process.StandardOutput.EndOfStream)
			//{
			//	char[] buffer = new char[1024];
			//	int charsRead = process.StandardOutput.Read(buffer, 0, buffer.Length);
			//	Console.ForegroundColor = ConsoleColor.Cyan;
			//	Console.WriteLine(new string(buffer));
			//	Console.ResetColor();
			//	procOutput.Append(buffer, 0, charsRead);

			//	if (procOutput.ToString().Contains("Delete (Y/N)?"))
			//	{
			//		process.StandardInput.WriteLine("Y");
			//		process.StandardInput.WriteLine();
			//		process.StandardInput.Flush();
			//		break;
			//	}
			//}

			process.WaitForExit();
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("process.ExitCode:" + process.ExitCode);
			Console.ResetColor();

			if (!process.HasExited)
			{
				process.Kill();
			}
		}
	}
}
