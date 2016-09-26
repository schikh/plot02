using System.Configuration;

namespace PlotService2.Configuration
{
    public static class Settings
    {
        public static readonly string EmptyDwgPath= ConfigurationSettings.AppSettings["EmptyDwgPath"];
        public static readonly string PlotPlanchetteScriptPath = ConfigurationSettings.AppSettings["PlotPlanchetteScriptPath"];
        public static readonly string PlotDwgScriptPath = ConfigurationSettings.AppSettings["PlotDwgScriptPath"];
        public static readonly string AcConsolePath = ConfigurationSettings.AppSettings["AcConsolePath"];
        public static readonly string LocalRootPath = ConfigurationSettings.AppSettings["LocalRootPath"];
        public static readonly string ProductionRootPath = ConfigurationSettings.AppSettings["ProductionRootPath"];
        public static readonly int BatchSize = int.Parse(ConfigurationSettings.AppSettings["BatchSize"]);
        public static readonly int QueueMinimumThreshold = int.Parse(ConfigurationSettings.AppSettings["QueueMinimumThreshold"]);
        public static readonly int NumberOfConsoles = int.Parse(ConfigurationSettings.AppSettings["NumberOfConsoles"]);
        public static readonly int BatchLoadInterval = int.Parse(ConfigurationSettings.AppSettings["BatchLoadInterval"]);
        public static readonly int MaximumConsoleExecutionTime = int.Parse(ConfigurationSettings.AppSettings["MaximumConsoleExecutionTime"]);        
    }
}