using Microsoft.Extensions.Configuration;

namespace PlotService.Services
{
	public class Configuration
	{
		public static IConfigurationRoot Instance { get; }
		private static IConfigurationSection AppSettings => Instance.GetSection("AppSettings");
		public static string EmptyDwgPath => AppSettings["EmptyDwgPath"];
		public static string PlotPlanchetteScriptPath => AppSettings["PlotPlanchetteScriptPath"];
		public static string PlotDwgScriptPath => AppSettings["PlotDwgScriptPath"];
		public static string AcConsolePath => AppSettings["AcConsolePath"];
		public static string LocalRootPath => AppSettings["LocalRootPath"];
		public static string ProductionRootPath => AppSettings["ProductionRootPath"];
		public static int BatchSize => int.Parse(AppSettings["BatchSize"]);
		public static int NumberOfConsoles => int.Parse(AppSettings["NumberOfConsoles"]);

		static Configuration()
		{
			var builder = new ConfigurationBuilder()
				//.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile("appsettings.development.json", optional: true);
			Instance = builder.Build();
		}
	}
}