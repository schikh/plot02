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
        private static readonly log4net.ILog _log =
                log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void Setup()
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository();

            var patternLayout = new PatternLayout();
            patternLayout.ConversionPattern = "%date %-5level - %message%newline";
            patternLayout.ActivateOptions();
            var roller = new RollingFileAppender();  //new FileAppender();
            roller.LockingModel = new FileAppender.InterProcessLock();
            roller.AppendToFile = true;
            var assemblyFolder = Helper.GetAssemblyFolder();
            roller.File = Path.Combine(assemblyFolder, "logs", "PlotPluggin.log");
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
    }
}