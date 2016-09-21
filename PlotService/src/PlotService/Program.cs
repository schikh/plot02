using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PlotService.Services;

namespace PlotService
{
	public class Program
	{

		static void DeleteFile(BlockingCollection<PlotTask> blockingCollection)
		{
			Process proc = new Process();
			// Run the command "del c:\temp" in a DOS window
			// This will show a confirmation prompt requiring 
			// us to send a "Y" to continue
			proc.StartInfo.FileName = "cmd.exe";
			proc.StartInfo.Arguments = @"/c del C:\bidon\Temp\xxx.txt";

			// Redirect standard input
			proc.StartInfo.RedirectStandardInput = true;

			// UseShellExecute must be false to redirect input
			proc.StartInfo.UseShellExecute = false;

			// Start the process
			proc.Start();

			// Wait a second
			Thread.Sleep(1000);

			// Write a "Y" to the process's input
			proc.StandardInput.WriteLine("Y");

			// Now that we've sent the confirmation "Y" wait for the process to exit
			proc.WaitForExit();

			Console.WriteLine("The process finished with ExitCode: {0}", proc.ExitCode);
			Console.Write("Press any key to exit...");
			//Console.ReadKey();
		}
	

	public static void Main(string[] args)
		{
			Console.WriteLine("Hello world");

			var tasks = new List<Task>();
			var count = 0;
			var blockingCollection = new BlockingCollection<PlotTask>();

			tasks.Add(Task.Factory.StartNew(() =>
			{
				Console.WriteLine("Tasks: 1");
				while (true)
				{
					Console.WriteLine("Tasks: 2");
					if (blockingCollection.Count() < 50)
					{
						Console.WriteLine("Tasks: 7");
						var plotManager = new PlotManager();
						var list = plotManager.GetTasks();
						list.ToList().ForEach(x => blockingCollection.Add(x));
					}
					Console.WriteLine("Tasks:" + blockingCollection.Count());
					count++;
					Thread.Sleep(10000);
				}
			}));

			for (int i = 0; i < 3; i++)
			{
				Console.WriteLine("Tasks: 3");
				tasks.Add(Task.Factory.StartNew(() => {
					Console.WriteLine("Tasks: 4");
					XXXXXXXX(blockingCollection);
				}));
			}

			Console.WriteLine("Tasks: 5");
			Task.WaitAll(tasks.ToArray());
			Console.WriteLine("Tasks: 6");

		}

		private static void XXXXXXXX(BlockingCollection<PlotTask> blockingCollection)
		{
			var fileName = @"C:\Program Files\Autodesk\Autodesk AutoCAD Map 3D 2014\accoreconsole.exe";

			while (true)
			{
				var plotTask = blockingCollection.Take();

				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("Worker 1: " + fileName + " " + plotTask.CommandLineParameters());
				Console.ResetColor();

				var d = Path.GetDirectoryName(plotTask.PathResultPdf);
				if (!Directory.Exists(d))
				{
					Directory.CreateDirectory(d);
				}

				var processStartInfo = new ProcessStartInfo {
					FileName = fileName,
					Arguments = plotTask.CommandLineParameters(),
					UseShellExecute = false,
					CreateNoWindow = true,
					//StandardOutputEncoding = Encoding.GetEncoding(Console.OutputEncoding.CodePage)
					RedirectStandardInput = true,
					RedirectStandardOutput = true,
					//StandardOutputEncoding = Encoding.ASCII,
					//StandardErrorEncoding = Encoding.GetEncoding(65001),
					//StandardOutputEncoding = Encoding.GetEncoding(65001)
				};

				var process = System.Diagnostics.Process.Start(processStartInfo);

				while (!process.StandardOutput.EndOfStream)
				{
					// Read up to 1024 characters into the char[] array "buffer"
					char[] buffer = new char[1024];
					// Store the number of characters actually read
					// (since it will undoubtedly be less than 1024)
					int charsRead = process.StandardOutput.Read(buffer, 0, buffer.Length);
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
		}

		public class PlotManager
		{
			public PlotManager()
			{
				var builder = new ConfigurationBuilder()
					//.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
					.AddJsonFile("appsettings.development.json", optional: true);
				Configuration = builder.Build();
			}

			private IConfigurationRoot Configuration { get; }

			public IEnumerable<PlotTask> GetTasks()
			{
				Console.WriteLine("GetTasks: 1");
				Console.WriteLine("WriteConnectionString: " + Configuration.GetConnectionString("WriteConnectionString"));
				var connectionString = Configuration.GetConnectionString("WriteConnectionString");
				var da = new DataAccessService(connectionString);
				var query = @"SELECT 
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
								WHERE a.S_PLTICKET_STATUS = 1 AND ROWNUM <= 100 AND C_TYPE_PLAN = 'T' ";

				var productionOutputFolder = @"\\NL1ORE1.ORES.NET\";
				var localOutputFolder = @"c:\test\EnerGis\";
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
					PathResultPdf = localOutputFolder + x.GetSafe<string>(14).Substring(productionOutputFolder.Length), 
					Scale = x.GetSafe<string>(15),
					Essay = x.GetInt32(16),
					PlotTicketStatus = x.GetInt32(17),
					Side = x.GetSafe<string>(18)
				});
				Console.WriteLine("GetTasks: 2 count:" + list.Count());
				return list;
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
			return
				@"/i W:\RWA004\Cardex\Est\Edpl\Vvs\Projet\EDPL-PP-115871.dwg /s C:\Test\Plot\Plot01\Scripts\PlotDwg.scr /f c:\test\EnerGis\HUB001A_PRD\BIZT_HUB_0347_04\East\201609\3_1_1643232_31043517\2_2_EDPL-PP-115871_197865.pdf /isolate";

			var builder = new CommandLineParametersBuilder();

			if (string.Equals(TypePlan, "T", StringComparison.InvariantCultureIgnoreCase))
			{
				builder.Add("i", @"C:\Test\Plot\Plot01\Scripts\Empty.dwg") 
					.Add(	"s", @"C:\Test\Plot\Plot01\Scripts\PlotPlanchette.scr")
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
					.Add("s", @"C:\Test\Plot\Plot01\Scripts\PlotDwg.scr")
					.Add("f", PathResultPdf)
					.Add("d")
					.Add("isolate");
			}
			return builder.GetCommandLineParameters();
		}
	}
}
