using System.IO;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace BatchPlot.Services
{
    public class Logger
    {
        public static void Setup()
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository();

            var patternLayout = new PatternLayout();
            patternLayout.ConversionPattern = "%date [%thread] %-5level %logger - %message%newline";
            patternLayout.ActivateOptions();

            var roller = new FileAppender();
            roller.AppendToFile = true;
            var assemblyFolder = Helper.GetAssemblyFolder();
            roller.File = Path.Combine(assemblyFolder, "#Test.log");
            roller.Layout = patternLayout;
            //roller.MaxSizeRollBackups = 5;
            //roller.MaximumFileSize = "10MB";
            //roller.RollingStyle = RollingFileAppender.RollingMode.Size;
            //roller.StaticLogFileName = true;
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