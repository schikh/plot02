using System;
using System.Reflection;
using log4net;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace PlotService2.Services
{
    public static class Logger
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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