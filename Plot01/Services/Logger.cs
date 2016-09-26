using System;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace BatchPlot.Services
{
    public class Logger
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Setup()
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository();

            var patternLayout = new PatternLayout();
            patternLayout.ConversionPattern = "%date %-5level - %message%newline";
            patternLayout.ActivateOptions();
            var roller = new RollingFileAppender();  //new FileAppender();
            roller.LockingModel = new FileAppender.InterProcessLock();
            roller.AppendToFile = true;
            var assemblyFolder = Helper.GetAssemblyFolder();
            var logsFolder = Path.Combine(assemblyFolder, "logs");
            if (!Directory.Exists(logsFolder))
            {
                Directory.CreateDirectory(logsFolder);
            }
            var file = Path.Combine(logsFolder, "PlotPluggin.log");
            roller.File = file;
            roller.Layout = patternLayout;
            roller.MaxSizeRollBackups = 15;
            //roller.MaximumFileSize = "10MB";
            roller.RollingStyle = RollingFileAppender.RollingMode.Date;
            roller.StaticLogFileName = true;
            roller.ActivateOptions();
            hierarchy.Root.AddAppender(roller);

            //MemoryAppender memory = new MemoryAppender();
            //memory.ActivateOptions();
            //hierarchy.Root.AddAppender(memory);

            hierarchy.Root.Level = Level.Debug;
            hierarchy.Configured = true;
        }

        //public static void Info(string message, object parameter)
        //{
        //    Info(message, new[] { parameter });
        //}

        public static void Info(string message, params object[] parameters)
        {
            _log.InfoFormat(message, parameters);
            Trace(message, parameters);
        }

        public static void Error(string message, params object[] parameters)
        {
            _log.ErrorFormat(message, parameters);
            Trace(message, parameters);
        }

        public static void Error(Exception ex)
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
    }
}