using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using PlotService2.Configuration;
using PlotService2.Services;
using Topshelf;

namespace PlotService2
{
    // "C:\Test\plot\PlotService2\bin\Debug\PlotService2.exe"  install -instance:1 -username:ores\adn534 -password:C0mplexPwd02
    public class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x => 
            {
                x.Service<PlotTaskManager>(s =>
                {
                    s.ConstructUsing(name => new PlotTaskManager());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                //x.RunAsLocalSystem();
                //x.StartAutomatically();
                x.StartAutomaticallyDelayed();

                x.SetDescription("Energis plot service");
                x.SetDisplayName("Energis plot service");
                x.SetServiceName("EnergisPlotService");
            });
            //HostFactory.New(x =>
            //{
            //    x.EnableServiceRecovery(
            //        rc =>
            //        {
            //            rc.RestartService(1);
            //        });
            //});
        }
    }

    public class PlotTaskManager
    {
        private readonly object _sysLock = new object();
        private readonly PlotTaskRepository _plotTaskRepository = new PlotTaskRepository();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public void Start()
        {
            var tasks = new List<Task>();
            var plotTasks = new BlockingCollection<PlotTask>();
            var stopwatch = Stopwatch.StartNew();
            var cancellationToken = _cancellationTokenSource.Token;

            tasks.Add(
                Task.Factory.StartNew(
                    () => {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                _plotTaskRepository.UpdatePlotJobStatusesFromPlotTaskStatuses();
                                if (plotTasks.Count() < Settings.BatchSize / 2)
                                {
                                    RetrieveNewTasks(plotTasks);
                                    _plotTaskRepository.ImportNewPlotTasks();
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex);
                            }
                            Thread.Sleep(Settings.BatchLoadInterval * 1000);
                        }
                    },
                    cancellationToken));

            var count = 0;

            tasks.AddRange(
                Enumerable.Range(0, Settings.NumberOfConsoles)
                    .Select(
                        x => Task.Factory.StartNew(
                            () => {
                                while (!cancellationToken.IsCancellationRequested)
                                {
                                    try
                                    {
                                        if (ProcessPlotTasks(plotTasks))
                                        {
                                            Logger.Info("PlotTask {0} {1} sec", ++count, Math.Round(stopwatch.Elapsed.TotalSeconds, 0));
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error(ex);
                                    }
                                }
                            },
                            cancellationToken)));

            Task.WaitAll(tasks.ToArray());
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        private void RetrieveNewTasks(BlockingCollection<PlotTask> plotTasks)
        {
            if (plotTasks.Count() < Settings.QueueMinimumThreshold)
            {
                var list = _plotTaskRepository.GetPlotTasks();
                plotTasks.TakeWhile(qItem => true);
                list.ToList().ForEach(x => plotTasks.Add(x));
            }
            Console.WriteLine("Tasks queue length:" + plotTasks.Count());
        }

        private bool ProcessPlotTasks(BlockingCollection<PlotTask> plotTasks)
        {
            var plotTask = plotTasks.Take();
            var result = false;

            if (!_plotTaskRepository.LockPlotTask(plotTask.TaskId))
            {
                Logger.Info("Plot task: {0} already locked", plotTask.TaskId);
                return false;
            }

            try
            {
                if (plotTask.IsImpetrant)
                {
                    result = ProcessPlotTickects(plotTask);
                }
                else
                {
                    if (File.Exists(plotTask.PathPlan))
                    {
                        var tempFolder = Helper.CreateTempFolder();
                        var f = new FileHelper();
                        plotTask.LocalPathPlan = f.ImportServerFile(plotTask.PathPlan, tempFolder);
                        //var list = f.GetAttachedFilePaths(plotTask.PathPlan);
                        //f.ImportServerFiles(list, tempFolder);
                        result = ProcessPlotTickects(plotTask);
                        Helper.DeleteTempFolder(tempFolder);
                    }
                    else
                    {
                        Logger.Error("Plot task: {0} Input file '{1}' not found", plotTask.TaskId, plotTask.PathPlan);
                    }
                }

                _plotTaskRepository.UpdatePlotTaskStatus(plotTask.TaskId, result);
            }
            catch (Exception ex)
            {
                Logger.Error("Plot task: {0} ERROR", plotTask.TaskId);
                Logger.Error(ex);
                _plotTaskRepository.UpdatePlotTaskStatus(plotTask.TaskId, false);
            }

            return result;
        }

        private bool ProcessPlotTickects(PlotTask plotTask)
        {
            Logger.Info("Plot task: {0} {1}", plotTask.TaskId, plotTask.CommandLineParameters());

            using (var process = new Process())
            {
                process.StartInfo.FileName = Settings.AcConsolePath;
                process.StartInfo.Arguments = plotTask.CommandLineParameters();
                //process.StartInfo.Arguments = @"/i ""C:\Test\plot\Plot01\Scripts\edpl-1326-2.dwg"" /m ""W:\RWA004\Cardex\Est\Edpl\Vvs\Reperage\El\edpl-1326-2.dwg"" /s ""C:\Test\Plot\Plot01\Scripts\PlotDwg.scr"" /f ""C:\Test\Plot\Plot01\Scripts\dump2.pdf"" /isolate";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.ErrorDialog = false;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                //process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
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

                lock (_sysLock)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(standardOutput);
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(process.StandardError.ReadToEnd());
                    Console.ResetColor();
                }

                process.WaitForExit(Settings.MaximumConsoleExecutionTime * 1000);

                if (!process.HasExited)
                {
                    process.Kill();
                }

                return standardOutput.Contains("PLOT SUCCESSFUL");
            }


        }
    }

    public class FileHelper
    {
        //public IEnumerable<string> GetAttachedFilePaths(string dwgFile)
        //{
        //    var directory = Path.GetDirectoryName(dwgFile);
        //    var xmlFile = Path.Combine(directory, Path.GetFileNameWithoutExtension(dwgFile) + ".xml");
        //    if (File.Exists(xmlFile))
        //    {
        //        //var text = File.ReadAllText(xmlFile);
        //        //var pattern = @"<file .* filename=""(?<fileName>[^""]*)"" .*>";
        //        //var expr = new Regex(pattern, RegexOptions.Multiline);
        //        //foreach (var file in expr
        //        //    .Matches(text).Cast<Match>()
        //        //    .Select(x => x.Groups["fileName"].Value)
        //        //    .Where(x => !string.Equals(Path.GetExtension(x), ".dwf", StringComparison.InvariantCultureIgnoreCase )))
        //        //{
        //        //    yield return Path.Combine(directory, file);
        //        //}
        //        var doc = XDocument.Load(xmlFile);
        //        var files = doc.Descendants("file")
        //            .Attributes("fileName")
        //            .Concat(doc.Descendants("file")
        //            .Attributes("filename"))
        //            .Select(x => x.Value)
        //            .Where(x => !string.Equals(Path.GetExtension(x), ".dwf", StringComparison.InvariantCultureIgnoreCase));
        //        foreach (var file in files)
        //        {
        //            yield return Path.Combine(directory, file);
        //        }
        //    }
        //}

        public IEnumerable<string> GetAttachedFilePaths(string fileName)
        {
            //CardexEnerGISParameters
            //CardexEnerGISParameters
            //commandParameters.IdPlanchette = (string)parameters[0].Value;
            //commandParameters.MapType = ReadStringList(parameters, 1, out nextParamIndex);
            //commandParameters.EnergyList = ReadStringList(parameters, nextParamIndex, out nextParamIndex);
            //commandParameters.Scale = Convert.ToInt32(parameters[nextParamIndex].Value);
            //commandParameters.ResultFileName = (string)parameters[nextParamIndex].Value;
            //commandParameters.UserId = (string)parameters[nextParamIndex].Value;

            //CardexPlotSepParameters
            //commandParameters.Division = (string)parameters[0].Value;
            //commandParameters.AskNumber = (string)parameters[1].Value;
            //commandParameters.AskNumber = "";
            //commandParameters.UserName = identity;
            //commandParameters.TotPlan = Convert.ToInt32(parameters[3].Value);
            //commandParameters.PlotterName = (string)parameters[4].Value;

            //PlotCardexEnerGISCommand
            //commandParameters.SourceFileName = (string)parameters[0].Value;
            //commandParameters.IsEgis = Convert.ToBoolean(parameters[1].Value);
            //commandParameters.Point1 = (Point3d)parameters[2].Value;
            //commandParameters.Point2 = (Point3d)parameters[3].Value;
            //commandParameters.PlotterName = (string)parameters[4].Value;
            //commandParameters.Scale = Convert.ToInt32(parameters[6].Value);
            //commandParameters.NbrCopy = Convert.ToInt32(parameters[6].Value);
            //commandParameters.UserId = (string)parameters[9].Value;

            //PlotCardexPlotSjtCommand
            //no param


            var text = File.ReadAllText(fileName);
            var pattern = @"<file .* filename=""(?<fileName>[^""]*)"" .*>";
            var expr = new Regex(pattern, RegexOptions.Multiline);
            foreach (var file in expr
                .Matches(text).Cast<Match>()
                .Select(x => x.Groups["fileName"].Value)
                .Where(x => !string.Equals(Path.GetExtension(x), ".dwf", StringComparison.InvariantCultureIgnoreCase)))
            {
                yield return Path.Combine(directory, file);
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
                    Logger.Info("FILE NOT FOUND {0}", serverFilePath);
                }
            }
        }
    }

    public class PlotTaskRepository
    {
        readonly string _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["WriteConnectionString"].ConnectionString;

        public IEnumerable<PlotTask> GetPlotTasks()
        {
            var da = new DataAccessService(_connectionString);
            var template = QueryTemplates.Templates.GetTemplate("Retreive_PTASK_records_to_process").Template;
            var query = string.Format(template, Settings.BatchSize);
            var list = da.IterateOverReader(query, MapPlotTask);
            return list;
        }

        public PlotTask MapPlotTask(IDataReader reader)
        {
            var p = new PlotTask();
            p.JobId = Convert.ToInt32(reader.GetValue(0));
            p.PlotTicket = Convert.ToInt32(reader.GetValue(1));
            p.JobStatus = Convert.ToInt32(reader.GetValue(2));
            p.Date = reader.GetDateTime(3);
            p.TotalPlan = Convert.ToInt32(reader.GetValue(4));
            p.UserId = reader.GetSafe<string>(5);
            p.TaskId = Convert.ToInt32(reader.GetValue(6));
            p.TypePlan = reader.GetSafe<string>(7);
            p.IdStamp = reader.GetSafe<string>(8);
            p.IdPlanchette = reader.GetSafe<string>(9);
            p.OrdPlan = Convert.ToInt32(reader.GetValue(10));
            p.TypMap = reader.GetSafe<string>(11);
            p.PathPlan = reader.GetSafe<string>(12);
            p.ListEnergy = reader.GetSafe<string>(13);
            p.PathResultPdf = Settings.LocalRootPath
                + reader.GetSafe<string>(14).Substring(Settings.ProductionRootPath.Length);
            p.Scale = reader.GetSafe<string>(15);
            p.Essay = Convert.ToInt32(reader.GetValue(16));
            p.PlotTicketStatus = Convert.ToInt32(reader.GetValue(17));
            p.Side = reader.GetSafe<string>(18);
            if (!string.IsNullOrEmpty(p.PathPlan))
            {
                p.PathPlan = GetFileUncPath(p.PathPlan);
            }
            return p;
        }

        public bool LockPlotTask(int taskId)
        {
            var da = new DataAccessService(_connectionString);
            var template = QueryTemplates.Templates.GetTemplate("Lock_PTASK_record").Template;
            var query = string.Format(template, taskId);
            var result = da.ExecuteCommand(query);
            return result == 1;
        }

        public bool UpdatePlotTaskStatus(int taskId, bool successful)
        {
            var da = new DataAccessService(_connectionString);
            var templateName = successful ? "Update_PTASK_record_to_successful_status"
                : "Update_PTASK_record_to_failed_status";
            var template = QueryTemplates.Templates.GetTemplate(templateName).Template;
            var query = string.Format(template, taskId);
            var result = da.ExecuteCommand(query);
            return result == 1;
        }

        public void ImportNewPlotTasks()
        {
            var da = new DataAccessService(_connectionString);
            var query = QueryTemplates.Templates.GetTemplate("Import_PJOB_from_PLT_MNGR_PLOT_TICKET").Template;
            var result = da.ExecuteCommand(query);
            query = QueryTemplates.Templates.GetTemplate("Import_PTASK_from_PLT_MNGR_PLOT_TICKET").Template;
            result = da.ExecuteCommand(query);
        }

        public void UpdatePlotJobStatusesFromPlotTaskStatuses()
        {
            var da = new DataAccessService(_connectionString);
            var query = QueryTemplates.Templates.GetTemplate("Update_PJOB_status_from_PTASK_statuses").Template;
            var result = da.ExecuteCommand(query);
        }

        private string GetFileUncPath(string folder)
        {
            folder = folder.ToLower().Replace("/", @"\");
            if (folder.StartsWith(Settings.EstFileServerName.ToLower()))
            {
                return folder.Replace(Settings.EstFileServerName.ToLower(), Settings.EstFileServerUncName);
            }
            if (folder.StartsWith(Settings.WestFileServerName.ToLower()))
            {
                return folder.Replace(Settings.WestFileServerName.ToLower(), Settings.WestFileServerUncName);
            }
            throw new NotSupportedException(string.Format("Folder not supported: {0}", folder));
        }
    }

    public class CommandLineParametersBuilder
    {
        private string _line = "";

        public CommandLineParametersBuilder Add(string name)
        {
            _line = _line + string.Format("/{0} ", name);
            return this;
        }

        public CommandLineParametersBuilder Add(string name, string value)
        {
            _line = _line + string.Format("/{0} \"{1}\" ", name, value);
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
        public string LocalPathPlan { get; set; }

        public bool IsImpetrant {
            get { return string.Equals(TypePlan, "T", StringComparison.InvariantCultureIgnoreCase); }
        }

        public string CommandLineParameters()
        {
            var builder = new CommandLineParametersBuilder();
            if (IsImpetrant)
            {
                builder.Add("i", Settings.EmptyDwgPath)
                    .Add("s", Settings.PlotPlanchetteScriptPath)
                    .Add("id", IdPlanchette)
                    .Add("r", Scale)
                    .Add("z", Side)
                    .Add("c", TypMap)
                    .Add("e", ListEnergy.Replace(' ', ','))
                    .Add("imp");
            }
            else
            {
                builder.Add("i", LocalPathPlan)
                    .Add("s", Settings.PlotDwgScriptPath)
                    .Add("m", Path.GetDirectoryName(PathPlan));
            }
            builder
                .Add("f", PathResultPdf)
                .Add("st", IdStamp)
                .Add("t", TotalPlan.ToString())
                .Add("n", OrdPlan.ToString())
                .Add("u", UserId)
                .Add("d")
                .Add("isolate");
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



//(CARDEX_ENERGIS "076156G" (list "MAP") (list "NETGIS" "MT") 500 "W:\\RWA005\\CARDEX\\PSERV\\TICKET\\DWG_PLOT\\A969EF13-C8A0-CA59-9F7707C54A7D71F3-MT.dwg"" ""HAD115"")"
//(CARDEX_ENERGIS "076156G" (list "MAP") (list "NETGIS" "MP") 500 "W:\\RWA005\\CARDEX\\PSERV\\TICKET\\DWG_PLOT\\A969EF13-C8A0-CA59-9F7707C54A7D71F3-MP.dwg" "HAD115")
//(CARDEX_ENERGIS "076156G" (list "MAP") (list "NETGIS" "SI") 500 "W:\\RWA005\\CARDEX\\PSERV\\TICKET\\DWG_PLOT\\A969EF13-C8A0-CA59-9F7707C54A7D71F3-SI.dwg" "HAD115")
//(CARDEX_ENERGIS "076156F" (list "MAP") (list "NETGIS" "BT") 500 "W:\\RWA005\\CARDEX\\PSERV\\TICKET\\DWG_PLOT\\A969EF14-C541-2D20-F1AA57672AC39793-BT.dwg" "HAD115")
//(CARDEX_ENERGIS "076156F" (list "MAP") (list "NETGIS" "MT") 500 "W:\\RWA005\\CARDEX\\PSERV\\TICKET\\DWG_PLOT\\A969EF14-C541-2D20-F1AA57672AC39793-MT.dwg" "HAD115")
//(CARDEX_ENERGIS "076156F" (list "MAP") (list "NETGIS" "EP") 500 "W:\\RWA005\\CARDEX\\PSERV\\TICKET\\DWG_PLOT\\A969EF14-C541-2D20-F1AA57672AC39793-EP.dwg" "HAD115")
//(CARDEX_ENERGIS "076156F" (list "MAP") (list "NETGIS" "MP") 500 "W:\\RWA005\\CARDEX\\PSERV\\TICKET\\DWG_PLOT\\A969EF14-C541-2D20-F1AA57672AC39793-MP.dwg" "HAD115")
//(CARDEX_ENERGIS "076156F" (list "MAP") (list "NETGIS" "SI") 500 "W:\\RWA005\\CARDEX\\PSERV\\TICKET\\DWG_PLOT\\A969EF14-C541-2D20-F1AA57672AC39793-SI.dwg" "HAD115")

//(CARDEX_PLOT_SEP "LOCAL" nil "HAD115 - Sulmon Geoffrey" 8 "PlotWave 900 - EDH SYA - Bureau de dessin (PS)")

//(CARDEX_PLOT "W:\\RWA005\\CARDEX\\PSERV\\TICKET\\DWG_PLOT\\A969EF13-C8A0-CA59-9F7707C54A7D71F3-MT.dwg" T nil nil "PlotWave 900 - EDH SYA - Bureau de dessin (PS)" "" 0.00 0 nil "HAD115")
//(CARDEX_PLOT "W:\\RWA005\\CARDEX\\PSERV\\TICKET\\DWG_PLOT\\A969EF13-C8A0-CA59-9F7707C54A7D71F3-MP.dwg" T nil nil "PlotWave 900 - EDH SYA - Bureau de dessin (PS)" "" 0.00 0 nil "HAD115")
//(CARDEX_PLOT "W:\\RWA005\\CARDEX\\PSERV\\TICKET\\DWG_PLOT\\A969EF13-C8A0-CA59-9F7707C54A7D71F3-SI.dwg" T nil nil "PlotWave 900 - EDH SYA - Bureau de dessin (PS)" "" 0.00 0 nil "HAD115")
//(CARDEX_PLOT "W:\\RWA005\\CARDEX\\PSERV\\TICKET\\DWG_PLOT\\A969EF14-C541-2D20-F1AA57672AC39793-BT.dwg" T nil nil "PlotWave 900 - EDH SYA - Bureau de dessin (PS)" "" 0.00 0 nil "HAD115")
//(CARDEX_PLOT "W:\\RWA005\\CARDEX\\PSERV\\TICKET\\DWG_PLOT\\A969EF14-C541-2D20-F1AA57672AC39793-MT.dwg" T nil nil "PlotWave 900 - EDH SYA - Bureau de dessin (PS)" "" 0.00 0 nil "HAD115")
//(CARDEX_PLOT "W:\\RWA005\\CARDEX\\PSERV\\TICKET\\DWG_PLOT\\A969EF14-C541-2D20-F1AA57672AC39793-EP.dwg" T nil nil "PlotWave 900 - EDH SYA - Bureau de dessin (PS)" "" 0.00 0 nil "HAD115")
//(CARDEX_PLOT "W:\\RWA005\\CARDEX\\PSERV\\TICKET\\DWG_PLOT\\A969EF14-C541-2D20-F1AA57672AC39793-MP.dwg" T nil nil "PlotWave 900 - EDH SYA - Bureau de dessin (PS)" "" 0.00 0 nil "HAD115")
//(CARDEX_PLOT "W:\\RWA005\\CARDEX\\PSERV\\TICKET\\DWG_PLOT\\A969EF14-C541-2D20-F1AA57672AC39793-SI.dwg" T nil nil "PlotWave 900 - EDH SYA - Bureau de dessin (PS)" "" 0.00 0 nil "HAD115")

//(CARDEX_PLOT_SJT)

//WASS
