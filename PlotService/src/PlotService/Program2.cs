//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Security.Cryptography.X509Certificates;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace PlotService
//{
//	//public class Program
//	//{
//	//  public static void Main(string[] args)
//	//  {
//	//	Task[] tasks = new Task[3];
//	//	var count = 0;
//	//	var blockingCollection = new BlockingCollection<string>();

//	//	tasks[0] = Task.Factory.StartNew(() =>
//	//	{
//	//		while (true)
//	//		{
//	//			blockingCollection.Add("value" + count);
//	//			Debug.WriteLine("ADD" + count);
//	//			count++;
//	//			Thread.Sleep(1000);
//	//		}
//	//	});

//	//	tasks[1] = Task.Factory.StartNew(() =>
//	//	{
//	//		while (true)
//	//		{
//	//			Console.WriteLine("Worker 1: " + blockingCollection.Take());
//	//		}
//	//	});

//	//	tasks[2] = Task.Factory.StartNew(() =>
//	//	{
//	//		while (true)
//	//		{
//	//			Console.WriteLine("Worker 2: " + blockingCollection.Take());
//	//		}
//	//	});

//	//	Task.WaitAll(tasks);
//	//}

//	public class Program
//	{
//		public static void Main(string[] args)
//		{
//			Task[] tasks = new Task[4];
//			var count = 0;
//			var blockingCollection = new BlockingCollection<string>();

//			tasks[0] = Task.Factory.StartNew(() =>
//			{
//				while (true)
//				{
//					blockingCollection.Add("value" + count);
//					Debug.WriteLine("ADD" + count);
//					count++;
//					Thread.Sleep(1000);
//				}
//			});

//			//tasks[1] = Task.Factory.StartNew(() =>
//			//{
//			//	while (true)
//			//	{
//			//		Console.WriteLine("Worker 1: " + blockingCollection.Take());
//			//		//var process = new Process();
//			//		//process.StartInfo.UseShellExecute = false;
//			//		//process.StartInfo.RedirectStandardOutput = true;
//			//		//process.OutputDataReceived += (sender, a) => Console.WriteLine("received output: {0}", a.Data);
//			//		//process.Start();
//			//		//process.BeginOutputReadLine();

//			//	//	var processStartInfo = new ProcessStartInfo
//			//	//	{
//			//	//		FileName = @"C:\Program Files\Autodesk\Autodesk AutoCAD Map 3D 2014\accoreconsole.exe",
//			//	//		Arguments = "/i \"C:\\Test\\Plot\\Plot01\\Scripts\\Empty.dwg\" /s \"C:\\Test\\Plot\\Plot01\\Scripts\\test.scr\" /id 079145E /r 500 /z West /c MAP /e \"BT\" /f \"C:\\Test\\Plot\\Plot01\\Scripts\\dump1.pdf\" /imp /isolate",
//			//	//		RedirectStandardOutput = true,
//			//	//		UseShellExecute = false,
//			//	//		CreateNoWindow = true,
//			//	//		StandardOutputEncoding = Encoding.Unicode
//			//	//};

//			//	//	var process = Process.Start(processStartInfo);
//			//	//   process.OutputDataReceived += (sender, a) => Console.WriteLine("received output: {0}", a.Data);
//			//	//	var output = process.StandardOutput.ReadToEnd();
//			//	//	process.WaitForExit();
//			//	//	Console.WriteLine("process.ExitCode:" + process.ExitCode);
//			//	//	Console.WriteLine(output);
//			//	}
//			//});

//			tasks[1] = Task.Factory.StartNew(() =>
//			{
//				while (true)
//				{
//					Console.ForegroundColor = ConsoleColor.Green;
//					Console.WriteLine("Worker 1: " + blockingCollection.Take());
//					Console.ResetColor();

//					var processStartInfo = new ProcessStartInfo
//					{
//						FileName = @"C:\Program Files\Autodesk\Autodesk AutoCAD Map 3D 2014\accoreconsole.exe",
//						Arguments = "/i \"C:\\Test\\Plot\\Plot01\\Scripts\\Empty.dwg\" /s \"C:\\Test\\Plot\\Plot01\\Scripts\\test.scr\" /id 079145E /r 500 /z West /c MAP /e \"BT\" /f \"C:\\Test\\Plot\\Plot01\\Scripts\\dump2.pdf\" /p \"Canon C5235 - MERCK NAM IT - BSM Reseaux.pc3\" /imp /d /isolate",
//						UseShellExecute = false,
//						CreateNoWindow = true,
//						//StandardOutputEncoding = Encoding.GetEncoding(Console.OutputEncoding.CodePage)
//						RedirectStandardInput = true,
//						RedirectStandardOutput = true,
//						StandardOutputEncoding = Encoding.ASCII,
//						//StandardErrorEncoding = Encoding.GetEncoding(65001),
//						//StandardOutputEncoding = Encoding.GetEncoding(65001)
//				};

//					var process = Process.Start(processStartInfo);

//					var output = process.StandardOutput.ReadToEnd();
//					var s = new StringBuilder();
//					output.Where((x,i) => i % 2 == 0).ToList().ForEach(x => s.Append(x));
//					output = s.ToString();
//					process.WaitForExit();
//					Console.ForegroundColor = ConsoleColor.Green;
//					Console.WriteLine("process.ExitCode:" + process.ExitCode);
//					Console.ResetColor();
//					Console.WriteLine(output);
//				}
//			});

//			Task.WaitAll(tasks);

//		}
//	}
//}
